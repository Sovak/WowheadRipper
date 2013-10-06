using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MySql.Data.MySqlClient;

namespace WowheadRipper
{
    public static class ExtendedCosts
    {
        public static UInt32 CURRENCY_PRECISION = 100;

        public static bool HasToBePrecised(UInt32 currency)
        {
            if (DBC.sCurrencyStore.ContainsKey(currency))
                if ((DBC.sCurrencyStore[currency].Flags & CURRENCY_PRECISION) != 0)
                    return true;
            return false;
        }

        public static UInt32 GetExtendedCost(Dictionary<UInt32, UInt32> currencyCost, Dictionary<UInt32, UInt32> itemCost, UInt32 arenaRating)
        {
            foreach (var itemExtendedCost in DB2.sItemExtendedCostStore)
            {
                int currencyMatchCount = 0;
                int currencyDefaultCount = 0;
                int itemMatchCount = 0;
                int itemDefaultCount = 0;

                for (int i = 0; i < 5; i++)
                {
                    if (itemExtendedCost.RequiredCurrency[i] != 0)
                        currencyDefaultCount++;

                    if (itemExtendedCost.RequiredItem[i] != 0)
                        itemDefaultCount++;

                    if (i <= currencyCost.Count)
                        if (currencyCost.ContainsKey(itemExtendedCost.RequiredCurrency[i]))
                            if (itemExtendedCost.RequiredCurrencyCount[i] == currencyCost[itemExtendedCost.RequiredCurrency[i]])
                                currencyMatchCount++;

                    if (i <= itemCost.Count)
                        if (itemCost.ContainsKey(itemExtendedCost.RequiredItem[i]))
                            if (itemExtendedCost.RequiredItemCount[i] == itemCost[itemExtendedCost.RequiredItem[i]])
                                itemMatchCount++;
                }

                if (currencyMatchCount == currencyCost.Count && currencyCost.Count == currencyDefaultCount && itemMatchCount == itemCost.Count && itemCost.Count == itemDefaultCount)
                    return itemExtendedCost.ID;
            }
            return 0;
        }
    }
}
