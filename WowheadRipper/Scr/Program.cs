using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Net;
using System.Web.Script.Serialization;
using System.Threading;
using System.Collections;
using System.Timers;

using dict = System.Collections.Generic.Dictionary<string, object>;

namespace WowheadRipper
{
    class Program
    {
        static public UInt32 datad;
        static public UInt32 datat;

        static void Main(string[] args)
        {
            datad = 0;
            Console.Title = "Wowhead Ripper";

            if (args.Length != 2 || !args[0].Contains("-file"))
            {
                Console.WriteLine("Usage WowheadRipper -file <filename>");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                return;
            }

            Defines.fileName = args[1];

            if (!File.Exists(Defines.fileName))
            {
                Console.WriteLine("File {0} doesnt exist", Defines.fileName);
                Console.WriteLine("Press any key to continue...");
                return;
            }

            //Lets take this to other thread
            Thread mainThread = new Thread(new ThreadStart(StartMainThread));
            mainThread.Start();

            while ((Console.ReadLine() != string.Format("exit") && Defines.programExit == 0) && Defines.programExit == 0)
                continue;

            return;
        }

        static void StartMainThread()
        {
            var lines = File.ReadAllLines(Defines.fileName);
            Int32 count = lines.Length;

            StreamReader reader = new StreamReader(Defines.fileName);

            if (count != 0)
                Console.WriteLine("Sucesfully loaded file {0}, {1} records found", Defines.fileName, count);

            else
            {
                Console.WriteLine("File {0} is empty", Defines.fileName);
                Console.WriteLine("Press any key to continue...");
                Defines.programExit = 1;
            }

            datat = (UInt32)count;
            Console.WriteLine("");

            Thread writerThread = new Thread(new ThreadStart(WriterThread));
            writerThread.Start();

            while (reader.Peek() >= 0)
            {
                string str = reader.ReadLine();
                string[] numbers = Regex.Split(str, @"\D+");

                if (numbers.Length != 2)
                {
                    Console.WriteLine("Incorrect format for {0}, skipping", str);
                    continue;
                }

                UInt32 type =  UInt32.Parse(numbers[1]);
                UInt32 entry =  UInt32.Parse(numbers[0]);
                ThreadStart starter = delegate { ParseData(entry, type); };
                Thread thread = new Thread(starter);
                thread.Start();

                Thread.Sleep(700); // Else connections will time out or your net will go down
            }
            return;
        }

        public static void ParseData(UInt32 type, UInt32 entry)
        {
            UInt32 totalCount = 0;
            UInt32 count = 0;
            List<string> content;

            try
            {
                content = ReadPage(Defines.GenerateWowheadUrl(type, entry));
            }
            catch (Exception e)
            {
                Console.WriteLine("{0} Id {1} Doesn't exist ({2})", Defines.id_name[type], entry, e.Message);
                datad++;
                return;
            }

            foreach (string line in content)
            {
                Regex dataRegex = Defines.GetDataRegex(type);
                Regex totalCountRegex = Defines.GetTotalCountRegex(type);

                Match m = dataRegex.Match(line);
                Match m2 = totalCountRegex.Match(line);

                if (m2.Success)
                {
                    string str = m2.Groups[0].Captures[0].Value;
                    string[] numbers = Regex.Split(str, @"\D+");
                        totalCount = uint.Parse(numbers[2]);
                }

                if (!m.Success)
                    continue;

                var json = new JavaScriptSerializer() { MaxJsonLength = int.MaxValue };
                string data = m.Groups[1].Captures[0].Value;
                data = data.Replace("[,", "[0,");   // otherwise deserializer complains
                object[] m_object = (object[])json.DeserializeObject(data);
                Defines.stream.Enqueue(string.Format("-- Parsing {0} loot for entry {1}", Defines.id_name[0], entry));

                foreach (dict objectInto in m_object)
                {
                    try
                    {
                        count++;
                        int id = (int)objectInto["id"];
                        int maxcount = 1;
                        int mincount = 1;
                        float pct = 0.0f;
                        string name = "";
                        int lootmode = 0;

                        if (type == 0)
                            lootmode = 1;

                        if (objectInto.ContainsKey("name"))
                            name = (string)objectInto["name"];
                        int m_count = (int)objectInto["count"];
                        int ArraySize = ((Array)objectInto["stack"]).GetLength(0);
                        int[] stack = new int[ArraySize];
                        Array.Copy((Array)objectInto["stack"], stack, ArraySize);
                        pct = (float)count / totalCount * 100.0f;
                        maxcount = stack[1];
                        mincount = stack[0];
                        pct = (float)Math.Round(pct, 3);
                        string strpct = pct.ToString();
                        strpct = strpct.Replace(",", "."); // needs to be changed otherwise SQL errors
                        string str = string.Format("INSERT INTO `{0}` VALUES ( '{1}', '{2}', '{3}', '{4}', '{5}', '{6}' , '{7}'); -- {8}",
                        Defines.db_name[type], entry, id, strpct, 1, lootmode, mincount, maxcount, name);
                        Defines.stream.Enqueue(str);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }

                if (count != 0)
                {
                    Defines.stream.Enqueue(string.Format("-- Parsed {0} loot for entry {1}", Defines.id_name[0], entry));
                    Defines.stream.Enqueue("");
                }

                Console.WriteLine("Parsed {0} loot for entry {1}", Defines.id_name[0], entry);
                datad++;
                return;
            }
            datad++;
            return;
        }

        public static void WriterThread()
        {
            StreamWriter file = new StreamWriter("parsed_data.sql", true);
            file.AutoFlush = true;
            while (true && Defines.programExit == 0)
            {
                Console.WriteLine("I lold");
                UInt32 dataWritten = 0;
                Queue<string> copy = Defines.stream; // prevent crash
                foreach (string str in copy)
                {
                    dataWritten = 1;
                    file.WriteLine(str);
                }

                if (datad == datat && Defines.stream.Count == 0)
                    Defines.programExit = 1;

                Thread.Sleep(100);

                if (dataWritten == 1)
                    Defines.stream.Clear();
            }
            file.Flush();
            file.Close();

            Console.WriteLine("");
            Console.WriteLine("Parsing done");
            Console.WriteLine("Press any key to continue...");

            return;
        }
        static List<string> ReadPage(string url)
        {
            WebRequest wrGETURL = WebRequest.Create(url);
            Stream objStream = wrGETURL.GetResponse().GetResponseStream();
            StreamReader objReader = new StreamReader(objStream);

            string sLine = "";
            int i = 0;
            List<string> content = new List<string>();
            while (sLine != null)
            {
                i++;
                sLine = objReader.ReadLine();
                if (sLine != null)
                    content.Add(sLine);
            }
            return content;

        }
    }
}
