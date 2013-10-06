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
        [Ripper(Defines.ParserType.PARSER_TYPE_DROPPEDBY)]
        public static void DroppedByParser(uint entry, uint typeId, uint subTypeId, List<String> content)
        {
            Int32 objectCount = 0;
            Int32 index = 0;

            foreach (String line in content)
            {
                String newLine = line;
                index++;
                int subIndex = 0;

                if (line.Length > 1)
                {
                    while (newLine[newLine.Length - 1] == ',')
                    {
                        newLine = String.Format("{0}{1}", newLine, content[index + ++subIndex]);
                    }
                }

                Match match = Def.GetDataRegex(typeId, subTypeId).Match(newLine);

                if (match.Success)
                {
                    JavaScriptSerializer json = new JavaScriptSerializer() { MaxJsonLength = int.MaxValue };
                    String data = match.Groups[1].Captures[0].Value;
                    data = data.Replace("[,", "[0,");  // Otherwise deserializer will fail
                    data = data.Replace("undefined", "0");  // Faction undefined to 0

                    object[] objectArray = (object[])json.DeserializeObject(data);

                    WriteSQL(typeId, entry, String.Format("-- Parsing {0} loot for entry {1}", Def.GetStreamName(typeId, subTypeId), entry));
                    WriteSQL(typeId, entry, String.Format("DELETE FROM `{0}` WHERE item = {1};", Def.GetDBName(typeId, subTypeId), entry));
                    objectCount = objectArray.Length;

                    foreach (System.Collections.Generic.Dictionary<String, object> objectInto in objectArray)
                    {
                        try
                        {
                            UInt32 lootId = (UInt32)(int)objectInto["id"];
                            Int32 maxcount = 1;
                            Int32 mincount = 1;
                            Double pct = 0.0f;
                            String name = "";
                            Int32 lootmode = 0;

                            if (typeId == 0 && subTypeId == 0)  // Only one fish in fishing
                                lootmode = 1;

                            if (objectInto.ContainsKey("name"))
                                name = ((String)objectInto["name"]);

                            int count = (int)objectInto["count"];
                            int totalCount = (int)objectInto["outof"];

                            pct = (float)count / totalCount * 100.0f;

                            maxcount = 1;
                            mincount = 1;

                            pct = Math.Round(pct, 3);
                            String strpct = pct.ToString();
                            strpct = strpct.Replace(",", "."); // needs to be changed otherwise SQL errors

                            String str = String.Format("INSERT INTO `{0}` VALUES ( '{1}', '{2}', '{3}', '{4}', '{5}', '{6}' , '{7}'); -- {8}",
                            Def.GetDBName(typeId, subTypeId), entry, lootId, strpct, 1, lootmode, mincount, maxcount, name);
                            WriteSQL(typeId, entry, str);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                        }
                    }
                    // If SQL aint empty
                    if (objectCount != 0)
                    {
                        WriteSQL(typeId, entry, String.Format("-- Parsed {0} loot for entry {1}", Def.GetStreamName(typeId, subTypeId), entry));
                        WriteSQL(typeId, entry, "");
                    }
                    datad++;
                    Console.WriteLine("{0}% - Parsed {1} data for entry {2}", Math.Round(datad / (float)commandList.Count * 100, 2), Def.GetStreamName(typeId, subTypeId), entry);
                }
            }
        }
    }
}
