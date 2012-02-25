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

        static void Main(string[] args)
        {
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

            while (Console.ReadLine() != string.Format("exit") || Defines.programExit == 1)
                Defines.programExit = 0;
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

            while (reader.Peek() >= 0)
            {
                string str = reader.ReadLine();
                string[] numbers = Regex.Split(str, @"\D+");

                if (numbers.Length != 2)
                {
                    Console.WriteLine("Incorrect format for {0}, skipping", str);
                    continue;
                }

                UInt32 type =  UInt32.Parse(numbers[0]);
                UInt32 entry =  UInt32.Parse(numbers[1]);
                ThreadStart starter = delegate { ParseData(entry, type); };
                Thread thread = new Thread(starter);
                thread.Start();

                Thread.Sleep(700); // Else connections will time out or your net will go down
            }

        }

        public static void ParseData(UInt32 type, UInt32 entry)
        {

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
