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
        public static void ParserOutdatedQuests(uint entry, uint typeId, uint subTypeId, List<string> content)
        {
            foreach (string line in content)
            {
                Match match = Def.GetDataRegex(typeId, subTypeId).Match(line);
                if (match.Success)
                {
                    WriteSQL(string.Format("UPDATE `{0}` SET minLvl = -1, maxLvl = -1 WHERE entry = {1};", Def.GetDBName(typeId, subTypeId), entry));
                }
            }
            datad++;
            Console.WriteLine("{0}% - Parsed {1} data for entry {2}", Math.Round(datad / (float)commandList.Count * 100, 2), Def.GetOutputName(typeId, subTypeId), entry);
        }
    }
}
