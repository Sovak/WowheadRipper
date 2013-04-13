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
        [Ripper(Defines.ParserType.PARSER_TYPE_LOOT)]
        public static void ParseLoot(uint entry, uint typeId, uint subTypeId, List<String> content)
        {
            Int32 objectCount = 0;
            foreach (String line in content)
            {
                Match match = Def.GetDataRegex(typeId, subTypeId).Match(line);
                if (match.Success)
                {
                    UInt32 totalCount = UInt32.Parse(Def.GetStringBetweenTwoOthers(line, "_totalCount: ", ","));
                    JavaScriptSerializer json = new JavaScriptSerializer() { MaxJsonLength = int.MaxValue };
                    String data = match.Groups[1].Captures[0].Value;
                    data = data.Replace("[,", "[0,");  // Otherwise deserializer will fail

                    object[] objectArray = (object[])json.DeserializeObject(data);

                    WriteSQL(typeId, entry, String.Format("-- Parsing {0} loot for entry {1}", Def.GetStreamName(typeId, subTypeId), entry));
                    WriteSQL(typeId, entry, String.Format("DELETE FROM `{0}` WHERE entry = {1};", Def.GetDBName(typeId, subTypeId), entry));
                    objectCount = objectArray.Length;

                    foreach (System.Collections.Generic.Dictionary<String, object> objectInto in objectArray)
                    {
                        try
                        {
                            UInt32 lootId = (UInt32)objectInto["id"];
                            Int32 maxcount = 1;
                            Int32 mincount = 1;
                            Double pct = 0.0f;
                            String name = "";
                            Int32 lootmode = 0;

                            if (typeId == 0 && subTypeId == 0)  // Only one fish in fishing
                                lootmode = 1;

                            if (objectInto.ContainsKey("name"))
                                name = ((String)objectInto["name"]).Remove(0, 1); // Remove the first char, some weird number from Wowhead

                            int count = (int)objectInto["count"];

                            int arraySize = ((Array)objectInto["stack"]).GetLength(0);
                            int[] stackArray = new int[arraySize];
                            Array.Copy((Array)objectInto["stack"], stackArray, arraySize);

                            pct = (float)count / totalCount * 100.0f;

                            maxcount = stackArray[1];
                            mincount = stackArray[0];

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
                }
            }
            // We need to parse aditional data
            // Gameobjects 
            if (typeId == 1)
                WriteSQL(typeId, entry, String.Format("UPDATE `gameobject_template` SET data1 = {0} WHERE entry = {0};", entry));
            // Item Contains
            // Disenchanging
            foreach (String s in content)
                if (Def.StringContains(s, "[tooltip=tooltip_reqenchanting]").Success)
                    if (typeId == 2 && subTypeId == 3)
                        WriteSQL(typeId, entry, String.Format("UPDATE `item_template` SET DisenchantID = '{0}', RequiredDisenchantSkill = '{1}' WHERE entry = '{0}';", entry, /*Def.StringContains(line, "[tooltip=tooltip_reqenchanting]").Success ? */uint.Parse(Def.GetStringBetweenTwoOthers(s, "[tooltip=tooltip_reqenchanting]", "[/tooltip]"))/* : 1)*/));

            // If SQL aint empty
            if (objectCount != 0)
            {
                WriteSQL(typeId, entry, String.Format("-- Parsed {0} loot for entry {1}", Def.GetStreamName(typeId, subTypeId), entry));
                WriteSQL(typeId, entry, "");
            }
            datad++;
            Console.WriteLine("{0}% - Parsed {1} loot for entry {2}", Math.Round(datad / (float)commandList.Count * 100, 2), Def.GetStreamName(typeId, subTypeId), entry);
        }
    }
}
