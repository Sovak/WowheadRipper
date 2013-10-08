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
        public static void LootParser(uint entry, uint typeId, uint subTypeId, List<String> content)
        {
            WriteSQL(typeId, entry, String.Format("-- Parsing {0} loot for entry {1}", Def.GetStreamName(typeId, subTypeId), entry));
            WriteSQL(typeId, entry, String.Format("DELETE FROM `{0}` WHERE entry = {1};", Def.GetDBName(typeId, subTypeId), entry));

            WowheadSerializer serializer = new WowheadSerializer(content, typeId, subTypeId);

            foreach (Dictionary<String, Object> objectInto in serializer.Objects)
            {
                try
                {
                    WowheadObject npcObject = new WowheadObject(objectInto);

                    UInt32 lootId = npcObject.GetId();
                    String name = npcObject.GetFixedName();
                    UInt32 mincount = npcObject.GetStackArray()[0];
                    UInt32 maxcount = npcObject.GetStackArray()[1];
                    Double pct = (npcObject.GetCount() / (Double)serializer.TotalCount) * 100.0f;
                    pct = Math.Round(pct, 3);
                    String stringPct = pct.ToString().Replace(",", "."); // needs to be changed otherwise SQL errors

                    Int32 lootmode = 0;
                    // Fishing lootmode - always 1
                    if (typeId == 0 && subTypeId == 0)
                        lootmode = 1;

                    // - infront of quest items
                    Int32 questId = npcObject.GetQuestId();

                    String str = String.Format("INSERT INTO `{0}` VALUES ( '{1}', '{2}', '{3}', '{4}', '{5}', '{6}' , '{7}'); -- {8}",
                    Def.GetDBName(typeId, subTypeId), entry, questId, stringPct, 1, lootmode, mincount, maxcount, name);
                    WriteSQL(typeId, entry, str);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
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

            WriteSQL(typeId, entry, String.Format("-- Parsed {0} loot for entry {1}", Def.GetStreamName(typeId, subTypeId), entry));
            WriteSQL(typeId, entry, "");
            Console.WriteLine("{0}% - Parsed {1} loot for entry {2}", Math.Round(++datad / (float)commandList.Count * 100, 2), Def.GetStreamName(typeId, subTypeId), entry);
        }
    }
}
