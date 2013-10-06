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
        public static void ParseVendor(uint entry, uint typeId, uint subTypeId, List<String> content)
        {
            uint count = 0;
            foreach (String line in content)
            {
                Match match = Def.GetDataRegex(typeId, subTypeId).Match(line);
                if (match.Success)
                {
                    var json = new JavaScriptSerializer() { MaxJsonLength = int.MaxValue };
                    String data = match.Groups[1].Captures[0].Value;
                    data = data.Replace("[,", "[0,");    // Otherwise deserializer will fail
                    data = data.Replace(",]", "]");
                    data = data.Replace("cost", "\"cost\"");
                    object[] objectArray = (object[])json.DeserializeObject(data);

                    WriteSQL(typeId, entry, String.Format("-- Parsing {0} vendor data for entry {1}", Def.GetStreamName(typeId, subTypeId), entry));
                    WriteSQL(typeId, entry, String.Format("DELETE FROM `{0}` WHERE entry = {1};", Def.GetDBName(typeId, subTypeId), entry));

                    foreach (System.Collections.Generic.Dictionary<String, object> objectInto in objectArray)
                    {
                        try
                        {
                            String name = "";
                            UInt32 cost = 0;
                            count++;

                            int itemId = (int)objectInto["id"];
                            if (objectInto.ContainsKey("name"))
                                name = ((String)objectInto["name"]).Remove(0, 1); // Remove the first char, some weird number from Wowhead

                            if (objectInto.ContainsKey("cost"))
                            {
                                Dictionary<UInt32, UInt32> itemCost  = new Dictionary<uint,uint>();
                                Dictionary<UInt32, UInt32> currencyCost  = new Dictionary<uint,uint>();

                                if (objectInto["cost"] is Object[])
                                {
                                    // Convert object[] array of cost to object array
                                    int costArraySize = ((Object[])objectInto["cost"]).Length;
                                    Object[] costArray = new Object[costArraySize];
                                    Array.Copy((Object[])objectInto["cost"], costArray, costArraySize);

                                    int arrayIndex = 0;
                                    foreach (object subCostArray in costArray)
                                    {
                                        // 1st thing is always gold
                                        if (++arrayIndex == 1)
                                            continue;

                                        // Convert object to Array class
                                        Array subCostArray1 = (Array)subCostArray;

                                        Object[] objectSubCostArray = new Object[subCostArray1.Length];
                                        Array.Copy(subCostArray1, objectSubCostArray, objectSubCostArray.Length);

                                        // Might not contain extended cost of currency or item
                                        if (subCostArray1.Length == 0)
                                            continue;

                                        foreach (Object finalArray in subCostArray1)
                                        {
                                            Array finalArray1 = (Array)finalArray;
                                            int[] intFinalArray = new int[finalArray1.Length];
                                            Array.Copy(finalArray1, intFinalArray, intFinalArray.Length);

                                            if (intFinalArray.Length < 1)
                                                continue;

                                            // 3rd array is item, format [itemId, count] or [itemId] for 1 item
                                            if (arrayIndex == 3)
                                            {
                                                {
                                                    UInt32 itemCount = 1;
                                                    UInt32 item = (UInt32)intFinalArray[0];
                                                    if (intFinalArray.Length != 1)
                                                        itemCount = (UInt32)intFinalArray[1];
                                                    itemCost.Add(item, itemCount);
                                                }
                                            }

                                            // 2rd array is currency, format [currencyId, count]
                                            else if (arrayIndex == 2)
                                            {
                                                {
                                                    UInt32 currency = (UInt32)intFinalArray[0];
                                                    UInt32 currenyCount = (UInt32)intFinalArray[1];

                                                    if (ExtendedCosts.HasToBePrecised(currency))
                                                        currenyCount *= ExtendedCosts.CURRENCY_PRECISION;

                                                    currencyCost.Add(currency, currenyCount);
                                                }
                                            }
                                        }
                                    }
                                }

                                cost = ExtendedCosts.GetExtendedCost(currencyCost, itemCost, 0);
                            }

                            // Needed
                            if (cost == 2)
                                cost = 0;

                            String str = String.Format("INSERT INTO `{0}` VALUES ( '{1}', '{2}', '{3}', '{4}', '{5}', '{6}', '{7}'); -- {8}",
                            Def.GetDBName(typeId, subTypeId), entry, 0, itemId, 0, 0, cost, 1, name);
                            WriteSQL(typeId, entry, str);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                        }
                    }
                }
            }
            // If SQL aint empty
            if (count != 0)
            {
                WriteSQL(typeId, entry, String.Format("-- Parsed {0} data for entry {1}", Def.GetStreamName(typeId, subTypeId), entry));
                WriteSQL(typeId, entry, "");
            }
            datad++;
            Console.WriteLine("{0}% - Parsed {1} data for entry {2}", Math.Round(datad / (float)commandList.Count * 100, 2), Def.GetStreamName(typeId, subTypeId), entry);
        }
    }
}
