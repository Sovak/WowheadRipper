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

namespace WowheadRipper
{
    partial class Program
    {
        static public Defines Def = new Defines();
        static public Int32 datad = 0;
        static public Dictionary<KeyValuePair<uint, uint>, int> commandList = new Dictionary<KeyValuePair<uint, uint>, int>();
        static StreamWriter outPut = new StreamWriter("data.sql", true);
        static Mutex mut = new Mutex();
        static public bool usePreCached = Config.Default.UsePreCachedWowhead;

        public static void WriteSQL(string str)
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
            }

            Console.WriteLine("Got {0} records to parse", commandList.Count);
            Console.WriteLine("Starting Parsing");

            foreach (KeyValuePair<uint, uint> key in commandList.Keys)
            {
                new Thread(new ThreadStart(delegate { ParseData(key.Key, commandList[key], key.Value); })).Start();
                if (usePreCached)
                    Thread.Sleep(10);
                else
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
            List<string> content;
            List<int> ids = Def.ExtractFlags(typeId, subTypeIdFlags);

            try
            {
                content = (usePreCached ? ReadFile(Def.GenerateWowheadFileName(typeId, entry)) : ReadPage(Def.GenerateWowheadUrl(typeId, entry)));
            }
            catch (Exception e)
            {
                datad += Def.GetValidFlags(typeId, subTypeIdFlags);
                Console.WriteLine("{0}% - Error while parsing Entry {1} ({2})", Math.Round(datad / (float)commandList.Count * 100, 2), entry, e.Message);
                return;
            }

            foreach (uint subTypeId in ids)
                Ripp(Def.GetParserType(typeId, subTypeId), entry, typeId, subTypeId, content);
            return;
        }

        static List<string> ReadPage(string url)
        {
            HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(url);
            myRequest.Method = "GET";
            myRequest.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:12.0) Gecko/20100101 Firefox/12.0"; // Only browsers can read all data, wowhead filters out bots
            myRequest.Timeout = 120000;
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

        static List<string> ReadFile(string fileName)
        {
            StreamReader sr = new StreamReader(string.Format("./wowhead/{0}", fileName), System.Text.Encoding.ASCII);
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
