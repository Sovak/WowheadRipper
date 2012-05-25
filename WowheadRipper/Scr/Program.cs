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
        static private Int32 datad = 0;
        static private Dictionary<KeyValuePair<uint, uint>, int> commandList = new Dictionary<KeyValuePair<uint, uint>, int>();
        static StreamWriter outPut = new StreamWriter("data.sql", true);
        static Mutex mut = new Mutex();

        static void WriteLog(string str)
        {
            mut.WaitOne();
            outPut.WriteLine(str);
            mut.ReleaseMutex();
        }

        static void Main(string[] args)
        {
            outPut.AutoFlush = true;
            Console.Clear();
            Console.Title = "Wowhead Ripper";
            List<string> files = new List<string>();

            foreach (string fileName in args)
            {
                if (!File.Exists(fileName))
                {
                    Console.WriteLine("File {0} doesnt exist, skipping", fileName);
                    continue;
                }
                if (File.ReadAllLines(fileName).Length == 0)
                {
                    Console.WriteLine("File {0} is empty, skipping", fileName);
                    continue;
                }
                files.Add(fileName);
            }

            foreach (string fileName in files)
            {
                Console.WriteLine("Loading file {0} ...", fileName);
                StreamReader file = new StreamReader(fileName);

                while (file.Peek() >= 0)
                {
                    string Line = file.ReadLine();
                    List<uint> lineData = Def.GetAllNumbersOfString(Line);

                    if (lineData.Count != 3)
                    {
                        Console.WriteLine("Incorrect format for line \"{0}\"", Line);
                        continue;
                    }

                    UInt32 typeId = lineData.ToArray()[0];
                    Int32 subTypeIdFlag = (int)lineData.ToArray()[1];
                    UInt32 entry = lineData.ToArray()[2];

                    if (typeId >= Def.GetMaxTypeId())
                    {

                        Console.WriteLine("Incorrect typeId for line \"{0}\"", Line);
                        continue;
                    }

                    if (subTypeIdFlag >= (1 << (int)Def.GetMaxTypeId()))
                    {

                        Console.WriteLine("Incorrect flags for line \"{0}\"", Line);
                        continue;
                    }

                    if (!commandList.Keys.Contains(new KeyValuePair<uint, uint>(typeId, entry)))
                         commandList.Add(new KeyValuePair<uint, uint>(typeId, entry), subTypeIdFlag);
                    else
                        commandList[new KeyValuePair<uint, uint>(typeId, entry)] |= subTypeIdFlag;
                }
                Console.WriteLine("File {0} loaded...", fileName);
            }

            Console.WriteLine("Got {0} records to parse", commandList.Count);
            Console.WriteLine("Starting Parsing");

            foreach (KeyValuePair<uint, uint> key in commandList.Keys)
            {
                new Thread(new ThreadStart(delegate { ParseData(key.Key, commandList[key], key.Value); })).Start();
                Thread.Sleep(700); // Needs to be done because Wowhead will think that you are a bot
            }

            Console.Beep();
            Console.WriteLine("Parsing done!");
            Console.WriteLine("Press any key to continue...");
            outPut.Flush();
            outPut.Close();
            Console.ReadKey();
            Environment.Exit(1);
        }

        public static void ParseData(UInt32 typeId, Int32 subTypeIdFlags, UInt32 entry)
        {
            UInt32 totalCount = 0;
            UInt32 count = 0;
            List<string> content;
            List<int> ids = Def.ExtractFlags(typeId, subTypeIdFlags);

            try
            {
                content = ReadPage(Def.GenerateWowheadUrl(typeId, entry));
            }
            catch (Exception e)
            {
                datad += Def.GetValidFlags(typeId, subTypeIdFlags);
                Console.WriteLine("{0}% - Error while parsing Entry {1} ({2})", Math.Round(datad / (float)commandList.Count * 100, 2), entry, e.Message);
                return;
            }

            foreach (uint subTypeId in ids)
            {
                foreach (string line in content)
                {
                    Match m = Def.GetDataRegex(typeId, subTypeId).Match(line);

                    if (!m.Success)
                        continue;

                    totalCount = uint.Parse(Def.GetStringBetweenTwoOthers(line, "_totalCount: ", ","));

                    var json = new JavaScriptSerializer() { MaxJsonLength = int.MaxValue };
                    string data = m.Groups[1].Captures[0].Value;
                    data = data.Replace("[,", "[0,");   // otherwise deserializer complains
                    object[] m_object = (object[])json.DeserializeObject(data);

                    WriteLog(string.Format("-- Parsing {0} loot for entry {1}", Def.GetOutputName(typeId, subTypeId), entry));
                    WriteLog(string.Format("DELETE FROM `{0}` WHERE entry = {1};", Def.GetDBName(typeId, subTypeId), entry));

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
                            WriteLog(str);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                        }
                    }

                    if (count != 0)
                    {
                        WriteLog(string.Format("-- Parsed {0} loot for entry {1}", Def.GetOutputName(typeId, subTypeId), entry));
                        WriteLog("");
                    }
                }
                datad++;
                Console.WriteLine("{0}% - Parsed {1} loot for entry {2}", Math.Round(datad / (float)commandList.Count * 100, 2), Def.GetOutputName(typeId, subTypeId), entry);
            }
            return;
        }

        static List<string> ReadPage(string url)
        {
            HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(url);
            myRequest.Method = "GET";
            myRequest.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:12.0) Gecko/20100101 Firefox/12.0"; // Only browsers can read all data, wowhead filters out bots
            WebResponse myResponse = myRequest.GetResponse();
            StreamReader sr = new StreamReader(myResponse.GetResponseStream(), System.Text.Encoding.ASCII);

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
