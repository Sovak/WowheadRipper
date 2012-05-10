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
        static private Defines Def = new Defines();
        static private UInt32 datad;
        static private UInt32 datat;

        static void AddToStream(string str)
        {
            Def.stream.Enqueue(str);
        }

        static void PressExit()
        {
            Console.WriteLine("Press any key to continue...");
        }

        static List<int> ExtractFlags(UInt32 typeId, Int32 num)
        {
            List<int> f = new List<int>();
            for (Int32 i = 0; i < 32; i++)
                if ((num & (1 << i)) == 1)
                    if (i <= Def.GetMaxSubTypeId(i))
                        f.Add(i);
            return f;
        }

        static void Main(string[] args)
        {
            datad = 0;
            Console.Clear();
            Console.Title = "Wowhead Ripper";

            if (args.Length != 2 || !args[0].Contains("-file"))
            {
                Console.WriteLine("Usage WowheadRipper -file <filename>");
                PressExit();
                Console.ReadKey();
                return;
            }

            Def.fileName = args[1];

            if (!File.Exists(Def.fileName))
            {
                Console.WriteLine("File {0} doesnt exist", Def.fileName);
                PressExit();
                Console.ReadKey();
                return;
            }

            //Lets take this to other thread, this one will be waiting for program exit
            Thread mainThread = new Thread(new ThreadStart(StartMainThread));
            mainThread.Start();

            while ((Console.ReadLine() != string.Format("exit") && Def.programExit == 0) && Def.programExit == 0)
                continue;

            return;
        }

        static void StartMainThread()
        {
            var lines = File.ReadAllLines(Def.fileName);
            Int32 count = lines.Length;

            if (count != 0)
                Console.WriteLine("Sucesfully loaded file {0}, {1} records found, please wait till parser starts parsing", Def.fileName, count);
            else
            {
                Console.WriteLine("File {0} is empty", Def.fileName);
                PressExit();
                Def.programExit = 1;
            }

            Console.WriteLine("");

            StreamReader tester = new StreamReader(Def.fileName);
            while (tester.Peek() >= 0)
            {
                string str = tester.ReadLine();
                string[] numbers = Regex.Split(str, @"\D");

                if (numbers.Length != 3)
                    continue;

                UInt32 typeId = UInt32.Parse(numbers[0]);
                UInt32 subTypeIdFlag = UInt32.Parse(numbers[1]);
    
                if (typeId >= Def.GetMaxTypeId())
                    continue;

                if (subTypeIdFlag >= (1 << (int)Def.GetMaxTypeId()))
                    continue;

                datat += (UInt32)Def.GetValidFlags(typeId, subTypeIdFlag);
            }

            StreamReader reader = new StreamReader(Def.fileName);
            Thread writerThread = new Thread(new ThreadStart(WriterThread));
            writerThread.Start();

            while (reader.Peek() >= 0)
            {
                string str = reader.ReadLine();
                string[] numbers = Regex.Split(str, @"\D");

                if (numbers.Length != 3)
                {
                    Console.WriteLine("Incorrect format for {0}, skipping", str);
                    continue;
                }

                UInt32 typeId = UInt32.Parse(numbers[0]);
                UInt32 subTypeIdFlag = UInt32.Parse(numbers[1]);
                UInt32 entry = UInt32.Parse(numbers[2]);

                if (typeId >= Def.GetMaxTypeId())
                {
                    Console.WriteLine("Incorrect TypeId {0} for {1}, skipping", typeId, entry);
                    continue;
                }

                if (subTypeIdFlag >= (1 << (int)Def.GetMaxTypeId()))
                {
                    Console.WriteLine("Incorrect SubTypeId Flag {0} for {1}, skipping", subTypeIdFlag, entry);
                    continue;
                }

                ThreadStart starter = delegate { ParseData(typeId, subTypeIdFlag, entry); };
                Thread thread = new Thread(starter);
                thread.Start();
                Thread.Sleep(700); // Else connections will time out or your net will go down
            }
            return;
        }

        public static void ParseData(UInt32 typeId, UInt32 subTypeIdFlag, UInt32 entry)
        {
            UInt32 totalCount = 0;
            UInt32 count = 0;
            List<string> content;
            List<int> ids = ExtractFlags(typeId, (Int32)subTypeIdFlag);

            try
            {
                content = ReadPage(Def.GenerateWowheadUrl(typeId, entry));
            }
            catch (Exception e)
            {
                datad++;
                Console.WriteLine("{0}% - Id {1} Doesn't exist ({2})", Math.Round(datad / (float)datat * 100, 2), entry, e.Message);
                return;
            }

            foreach (int tId in ids)
            {
                UInt32 subTypeId = (UInt32)tId;
                foreach (string line in content)
                {
                    Regex dataRegex = Def.GetDataRegex(typeId, subTypeId);
                    Regex totalCountRegex = Def.GetTotalCountRegex(typeId, subTypeId);

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

                    AddToStream(string.Format("-- Parsing {0} loot for entry {1}", Def.GetOutputName(typeId, subTypeId), entry));
                    AddToStream(string.Format("DELETE FROM `{0}` WHERE entry = {1};", Def.GetDBName(typeId, subTypeId), entry));
                    AddToStream("");

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

                            if (typeId == 0 && subTypeId == 0)  // Only one loot in fishing
                                lootmode = 1;

                            if (objectInto.ContainsKey("name"))
                                name = (string)objectInto["name"];
                            int m_count = (int)objectInto["count"];
                            int ArraySize = ((Array)objectInto["stack"]).GetLength(0);
                            int[] stack = new int[ArraySize];
                            Array.Copy((Array)objectInto["stack"], stack, ArraySize);
                            pct = (float)m_count / totalCount * 100.0f;
                            maxcount = stack[1];
                            mincount = stack[0];
                            pct = (float)Math.Round(pct, 3);
                            string strpct = pct.ToString();
                            strpct = strpct.Replace(",", "."); // needs to be changed otherwise SQL errors
                            string str = string.Format("INSERT INTO `{0}` VALUES ( '{1}', '{2}', '{3}', '{4}', '{5}', '{6}' , '{7}'); -- {8}",
                            Def.GetDBName(typeId, subTypeId), entry, id, strpct, 1, lootmode, mincount, maxcount, name);
                            AddToStream(str);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                        }
                    }

                    if (count != 0)
                    {
                        AddToStream(string.Format("-- Parsed {0} loot for entry {1}", Def.GetOutputName(typeId, subTypeId), entry));
                        AddToStream("");
                    }
                }
                datad++;
                Console.WriteLine("{0}% - Parsed {1} loot for entry {2}", Math.Round(datad / (float)datat * 100, 2), Def.GetOutputName(typeId, subTypeId), entry);
            }
            return;
        }

        public static void WriterThread()
        {
            StreamWriter file = new StreamWriter("parsed_data.sql", true);
            file.AutoFlush = true;

            while (true && Def.programExit == 0)
            {
                UInt32 dataWritten = 0;
                Queue<string> copy = Def.stream; // prevent crash
                foreach (string str in copy)
                {
                    dataWritten = 1;
                    file.WriteLine(str);
                }

                if (datad == datat && Def.stream.Count == 0)
                    Def.programExit = 1;

                Thread.Sleep(100);

                if (dataWritten == 1)
                    Def.stream.Clear();
            }

            file.Flush();
            file.Close();
            Console.WriteLine("");
            Console.WriteLine("Parsing done");
            PressExit();
            Console.Beep();

            return;
        }

        static List<string> ReadPage(string url)
        {
            HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(url);
            myRequest.Method = "GET";
            myRequest.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:12.0) Gecko/20100101 Firefox/12.0"; // Only this one can parse items
            WebResponse myResponse = myRequest.GetResponse();
            StreamReader sr = new StreamReader(myResponse.GetResponseStream(), System.Text.Encoding.UTF8);

            string sLine = "";
            int i = 0;
            List<string> content = new List<string>();
            while (sLine != null)
            {
                i++;
                sLine = sr.ReadLine();
                if (sLine != null)
                    content.Add(sLine);
            }
            return content;

        }
    }
}
