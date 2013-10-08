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

            WriteSQL(typeId, entry, String.Format("-- Parsing {0} loot for entry {1}", Def.GetStreamName(typeId, subTypeId), entry));
            WriteSQL(typeId, entry, String.Format("DELETE FROM `{0}` WHERE item = {1};", Def.GetDBName(typeId, subTypeId), entry));

            WowheadSerializer serializer = new WowheadSerializer(content, typeId, subTypeId);

            foreach (Dictionary<String, Object> objectInto in serializer.Objects)
            {
                try
                {
                    WowheadObject npcObject = new WowheadObject(objectInto);

                    UInt32 lootId = npcObject.GetId();
                    String name = npcObject.GetName();
                    Int32 maxcount = 1; // NYI
                    Int32 mincount = 1; // NYI
                    Double pct = (npcObject.GetCount() / (Double)npcObject.GetOutOf()) * 100.0f;
                    pct = Math.Round(pct, 3);
                    String stringPct = pct.ToString().Replace(",", "."); // needs to be changed otherwise SQL errors

                    String str = String.Format("INSERT INTO `{0}` VALUES ( '{1}', '{2}', '{3}', '{4}', '{5}', '{6}' , '{7}'); -- {8}",
                    Def.GetDBName(typeId, subTypeId), lootId, entry, stringPct, 1, 0, mincount, maxcount, name);
                    WriteSQL(typeId, entry, str);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }

            WriteSQL(typeId, entry, String.Format("-- Parsed {0} loot for entry {1}", Def.GetStreamName(typeId, subTypeId), entry));
            WriteSQL(typeId, entry, "");
            Console.WriteLine("{0}% - Parsed {1} data for entry {2}", Math.Round(++datad / (Double)commandList.Count * 100, 2), Def.GetStreamName(typeId, subTypeId), entry);
        }
    }
}
