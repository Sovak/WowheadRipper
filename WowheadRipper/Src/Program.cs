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
        static public Int32 count = 0;
        static public Int32 dataDone = 0;
        static public Dictionary<KeyValuePair<uint, uint>, int> commandList = new Dictionary<KeyValuePair<uint, uint>, int>();
        static StreamWriter singleFileStream = null;
        static Mutex mutex = new Mutex();
        static public bool usePreCached = Properties.Settings.Default.usePreCached;
        static public String configFileName = Properties.Settings.Default.fileName;
        static public string dbcFolder = "./dbc/";

        static void Main(String[] args)
        {
            Console.Clear();
            Console.Title = "Wowhead Ripper";
            DBC.LoadDBCs();
            DB2.LoadDB2s();
            List<String> files = new List<String>();


            foreach (String fileName in args)
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

            foreach (String fileName in files)
            {
                Console.WriteLine("Loading file {0}...", fileName);
                var file = File.ReadAllText(fileName);

                Regex regex = new Regex(@"([0-9]+) +([0-9]+) +([0-9]+) *[,\r\n]+");

                foreach (Match result in regex.Matches(file))
                {
                    UInt32 typeId = uint.Parse(result.Groups[1].ToString());
                    Int32 subTypeIdFlag = int.Parse(result.Groups[2].ToString());
                    UInt32 entry = uint.Parse(result.Groups[3].ToString());

                    if (typeId >= Defines.GetMaxTypeId())
                    {

                        Console.WriteLine("Incorrect typeId for command \"{0} {1} {2}\"", typeId, subTypeIdFlag, entry);
                        continue;
                    }

                    if (subTypeIdFlag >= (1 << (int)Defines.GetMaxTypeId()))
                    {

                        Console.WriteLine("Incorrect flags for command \"{0} {1} {2}\"", typeId, subTypeIdFlag, entry);
                        continue;
                    }

                    if (!commandList.Keys.Contains(new KeyValuePair<uint, uint>(typeId, entry)))
                            commandList.Add(new KeyValuePair<uint, uint>(typeId, entry), subTypeIdFlag);
                    else
                        commandList[new KeyValuePair<uint, uint>(typeId, entry)] |= subTypeIdFlag;

                }
            }

            Console.WriteLine("Got {0} records to parse.", commandList.Count);
            Console.WriteLine("Starting Parsing, please Stand by.");

            if (configFileName != "")
            {
                singleFileStream = new StreamWriter(configFileName, true);
                count = commandList.Count;
                singleFileStream.AutoFlush = true;
            }

            foreach (KeyValuePair<uint, uint> key in commandList.Keys)
            {
                new Thread(new ThreadStart(delegate { ParseData(key.Key, commandList[key], key.Value); })).Start();
                if (usePreCached)
                    Thread.Sleep(20);
                else
                    Thread.Sleep(700); // Needs to be done because Wowhead will think that you are a bot
            }

            while (count != dataDone)
            {
                // Wait till all threads close
            }

            if (singleFileStream != null)
            {
                singleFileStream.Flush();
                singleFileStream.Close();
            }

            Console.Beep();
            Console.WriteLine("Parsing done!");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        public static void ParseData(UInt32 typeId, Int32 subTypeIdFlags, UInt32 entry)
        {
            List<String> content;
            List<int> ids = Defines.ExtractFlags(typeId, subTypeIdFlags);

            try
            {
                content = (usePreCached ? ReadFile(Defines.GenerateWowheadFileName(typeId, entry)) : ReadPage(Defines.GenerateWowheadUrl(typeId, entry)));
            }
            catch (Exception e)
            {
                dataDone += Defines.GetValidFlags(typeId, subTypeIdFlags);
                Console.WriteLine("{0}% - Error while parsing Entry {1} ({2})", Math.Round(dataDone / (float)commandList.Count * 100, 2), entry, e.Message);
                return;
            }

            foreach (uint subTypeId in ids)
                Ripp(Defines.GetParserType(typeId, subTypeId), entry, typeId, subTypeId, content);

            return;
        }

        static List<String> ReadPage(String url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:12.0) Gecko/20100101 Firefox/12.0"; // Only browsers can read all data, wowhead filters out bots
            request.Timeout = 120000;
            request.Proxy = null;
            WebResponse myResponse = request.GetResponse();
            StreamReader streamReader = new StreamReader(myResponse.GetResponseStream(), System.Text.Encoding.ASCII);

            // We need to skip null lines

            String line = "";
            List<String> content = new List<String>();
            while (line != null)
            {
                line = streamReader.ReadLine();

                if (line != null)
                    content.Add(line);
            }

            return content;

        }

        static List<String> ReadFile(String fileName)
        {
            List<String> content = new List<String>();
            return File.ReadAllLines(String.Format("./wowhead/{0}", fileName)).ToList();
        }

        public static void WriteSQL(UInt32 type, UInt32 entry, string str)
        {
            if (singleFileStream != null)
            {
                mutex.WaitOne();
                singleFileStream.WriteLine(str);
                mutex.ReleaseMutex();
            }
            else
            {
                String fileName = String.Format("{0}_{1}.sql", Defines.GetRawName(type), entry);
                using (StreamWriter streamWriter = new StreamWriter(fileName, true))
                {
                    streamWriter.WriteLine(str);
                }
            }
        }
    }
}
