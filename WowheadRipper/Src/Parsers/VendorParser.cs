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
        [Ripper(Defines.ParserType.PARSER_TYPE_VENDOR)]
        public static void ParseVendor(UInt32 entry, UInt32 typeId, UInt32 subTypeId, List<String> content)
        {
            WriteSQL(typeId, entry, String.Format("-- Parsing {0} vendor data for entry {1}", Def.GetStreamName(typeId, subTypeId), entry));
            WriteSQL(typeId, entry, String.Format("DELETE FROM `{0}` WHERE entry = {1};", Def.GetDBName(typeId, subTypeId), entry));

            WowheadSerializer serializer = new WowheadSerializer(content, typeId, subTypeId);

            foreach (Dictionary<String, Object> objectInto in serializer.Objects)
            {
                try
                {
                    WowheadObject npcObject = new WowheadObject(objectInto);

                    UInt32 id = npcObject.GetId();
                    String name = npcObject.GetFixedName();
                    UInt32 extendedCost = ExtendedCosts.GetExtendedCost(npcObject.GetCurrencyCost(), npcObject.GetItemCost(), 0);

                    // Needed (NULL cost ?)
                    if (extendedCost == 2)
                        extendedCost = 0;

                    String str = String.Format("INSERT INTO `{0}` VALUES ( '{1}', '{2}', '{3}', '{4}', '{5}', '{6}', '{7}'); -- {8}",
                    Def.GetDBName(typeId, subTypeId), entry, 0, id, 0, 0, extendedCost, 1, name);
                    WriteSQL(typeId, entry, str);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }

            WriteSQL(typeId, entry, String.Format("-- Parsed {0} data for entry {1}", Def.GetStreamName(typeId, subTypeId), entry));
            WriteSQL(typeId, entry, "");
            Console.WriteLine("{0}% - Parsed {1} data for entry {2}", Math.Round(++datad / (float)commandList.Count * 100, 2), Def.GetStreamName(typeId, subTypeId), entry);
        }
    }
}
