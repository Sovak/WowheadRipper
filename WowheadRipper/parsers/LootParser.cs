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
        public static void ParserLoot(uint entry, uint typeId, uint subTypeId, List<string> content)
        {
            uint count = 0;
            foreach (string line in content)
            {
                Match match = Def.GetDataRegex(typeId, subTypeId).Match(line);
                if (match.Success)
                {
                    uint totalCount = uint.Parse(Def.GetStringBetweenTwoOthers(line, "_totalCount: ", ","));
                    var json = new JavaScriptSerializer() { MaxJsonLength = int.MaxValue };
                    string data = match.Groups[1].Captures[0].Value;
                    data = data.Replace("[,", "[0,");   // otherwise deserializer complains
                    object[] m_object = (object[])json.DeserializeObject(data);

                    WriteSQL(string.Format("-- Parsing {0} loot for entry {1}", Def.GetOutputName(typeId, subTypeId), entry));
                    WriteSQL(string.Format("DELETE FROM `{0}` WHERE entry = {1};", Def.GetDBName(typeId, subTypeId), entry));

                    foreach (System.Collections.Generic.Dictionary<string, object> objectInto in m_object)
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

                            if (typeId == 0 && subTypeId == 0)  // Only one fish in fishing
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
                            WriteSQL(str);
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
                WriteSQL(string.Format("UPDATE `gameobject_template` SET data1 = {0} WHERE entry = {0};", entry));
            // Item Contains
            // Disenchanging
            foreach (string s in content)
                if (Def.StringContains(s, "[tooltip=tooltip_reqenchanting]").Success)
                    if (typeId == 2 && subTypeId == 3)
                        WriteSQL(string.Format("UPDATE `item_template` SET DisenchantID = '{0}', RequiredDisenchantSkill = '{1}' WHERE entry = '{0}';", entry, /*Def.StringContains(line, "[tooltip=tooltip_reqenchanting]").Success ? */uint.Parse(Def.GetStringBetweenTwoOthers(s, "[tooltip=tooltip_reqenchanting]", "[/tooltip]"))/* : 1)*/));

            // If SQL aint empty
            if (count != 0)
            {
                WriteSQL(string.Format("-- Parsed {0} loot for entry {1}", Def.GetOutputName(typeId, subTypeId), entry));
                WriteSQL("");
            }
            datad++;
            Console.WriteLine("{0}% - Parsed {1} loot for entry {2}", Math.Round(datad / (float)commandList.Count * 100, 2), Def.GetOutputName(typeId, subTypeId), entry);
        }
    }
}
