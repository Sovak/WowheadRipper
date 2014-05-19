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
    public static partial class Program
    {
        [Ripper(Defines.ParserType.PARSER_TYPE_OUTDATED_QUEST)]
        public static void ParseOutdatedQuests(uint entry, uint typeId, uint subTypeId, List<string> content)
        {
            List<String> strList = new List<String>();
            foreach (string line in content)
            {
                Match match = Defines.GetDataRegex(typeId, subTypeId).Match(line);
                if (match.Success)
                {
                    strList.Add(string.Format("UPDATE `{0}` SET minLvl = -1, maxLvl = -1 WHERE entry = {1};", Defines.GetDBName(typeId, subTypeId), entry));
                }
            }
            WriteSQL(typeId, entry, strList);
            Console.WriteLine("{0}% - Parsed {1} data for entry {2}", Math.Round(++dataDone / (float)commandList.Count * 100, 2), Defines.GetStreamName(typeId, subTypeId), entry);
        }
    }
}
