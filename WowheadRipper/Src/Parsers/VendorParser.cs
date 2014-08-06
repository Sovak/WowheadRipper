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
            StringBuilder strBuilder = new StringBuilder();
            UInt32 slot = 0;
            UInt16 type = 0;
            strBuilder.AppendLine(String.Format("-- Parsing {0} vendor data for entry {1}", Defines.GetStreamName(typeId, subTypeId), entry));
            strBuilder.AppendLine(String.Format("DELETE FROM `{0}` WHERE entry = {1};", Defines.GetDBName(typeId, subTypeId), entry));
            strBuilder.AppendLine(String.Format("INSERT INTO `npc_vendor` (`entry`, `slot`, `item`, `maxcount`, `incrtime`, `ExtendedCost`, `type`) VALUES "));

            WowheadSerializer serializer = new WowheadSerializer(content, typeId, subTypeId);

            foreach (Dictionary<String, Object> objectInto in serializer.Objects)
            {
                try
                {
                    WowheadObject wowheadObject = new WowheadObject(objectInto);

                    UInt32 id = wowheadObject.GetId();
                    type = 1;
                    String name = wowheadObject.GetFixedName();
                    UInt64 cost = wowheadObject.GetMoneyCost();
                    if (cost == 0)
                    {
                        // type = 2;
                        cost = ExtendedCosts.GetExtendedCost(wowheadObject.GetCurrencyCost(), wowheadObject.GetItemCost(), 0);
                        if (cost == 0)
                            cost = 99999;
                    }

                    // (`entry`, `slot`, `item`, `maxcount`, `incrtime`, `ExtendedCost`, `type`)
                    String str = String.Format("('{0}', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}'); -- {7}", entry, slot, id, 0, 0, cost, type, name);
                    strBuilder.AppendLine(str);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                slot++;
            }

            strBuilder.AppendLine(String.Format("-- Parsed {0} data for entry {1}", Defines.GetStreamName(typeId, subTypeId), entry));
            strBuilder.AppendLine("");
            WriteSQL(typeId, entry, strBuilder.ToString());

            Console.WriteLine("{0}% - Parsed {1} data for entry {2}", Math.Round(++dataDone / (float)commandList.Count * 100, 2), Defines.GetStreamName(typeId, subTypeId), entry);
        }
    }
}
