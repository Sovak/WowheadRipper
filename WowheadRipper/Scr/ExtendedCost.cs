using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MySql.Data.MySqlClient;

namespace WowheadRipper
{
    public static class ExtendedCosts
    {
        private struct ExtendedCostStructure
        {
            public UInt32[] itemId;
            public UInt32[] itemCount;
            public UInt32[] currencyId;
            public UInt32[] currencyCount;
            public UInt32 arenaRating;
        }

        private static List<UInt32> precisedCurrency = new List<UInt32>();
        private static Dictionary<UInt32, ExtendedCostStructure> extendedCostStore = new Dictionary<uint,ExtendedCostStructure>();
        public static UInt32 CURRENCY_PRECISION = 100;

        public static void Initialize()
        {
            extendedCostStore.Clear();
            precisedCurrency.Clear();

            try
            {
                MySqlCommand mySqlCommand;
                string connectionString = String.Format("server=localhost;port = 3306; user id=trinity; password=trinity; database=wowheadripper; pooling=false;");

                MySqlConnection mySqlConnection = new MySqlConnection(connectionString);
                mySqlConnection.Open();

                string query = string.Format("SELECT * FROM extendedcosts WHERE 1");
                mySqlCommand = new MySqlCommand(query, mySqlConnection);

                using (MySqlDataReader reader = mySqlCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int i = 0;
                        ExtendedCostStructure costStruct = new ExtendedCostStructure();
                        UInt32 id = uint.Parse(reader[i++].ToString());

                        costStruct.itemId = new UInt32[5];
                        costStruct.itemCount = new UInt32[5];
                        costStruct.currencyId = new UInt32[5];
                        costStruct.currencyCount = new UInt32[5];

                        for (int j = 0; j < 5; j++)
                            costStruct.itemId[j] = uint.Parse(reader[i++].ToString());
                        for (int j = 0; j < 5; j++)
                            costStruct.itemCount[j] = uint.Parse(reader[i++].ToString());
                        costStruct.arenaRating = uint.Parse(reader[i++].ToString());
                        for (int j = 0; j < 5; j++)
                            costStruct.currencyId[j] = uint.Parse(reader[i++].ToString());
                        for (int j = 0; j < 5; j++)
                            costStruct.currencyCount[j] = uint.Parse(reader[i++].ToString());
                        extendedCostStore.Add(id, costStruct);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            precisedCurrency.Add(390); // Conquest
            precisedCurrency.Add(392); // Honor
            precisedCurrency.Add(395); // Justice
            precisedCurrency.Add(396); // Valor
            precisedCurrency.Add(483); // Conquest Arena Meta
            precisedCurrency.Add(484); // Conquest BG Meta
        }

        public static bool HasToBePrecised(UInt32 currency)
        {
            return precisedCurrency.Contains(currency);
        }

        public static UInt32 GetExtendedCost(Dictionary<UInt32, UInt32> currencyCost, Dictionary<UInt32, UInt32> itemCost, UInt32 arenaRating)
        {
            foreach (KeyValuePair<UInt32, ExtendedCostStructure> data in extendedCostStore)
            {
                int currencyMatchCount = 0;
                int currencyDefaultCount = 0;
                int itemMatchCount = 0;
                int itemDefaultCount = 0;

                for (int i = 0; i < 5; i++)
                {
                    if (data.Value.currencyId[i] != 0)
                        currencyDefaultCount++;

                    if (data.Value.itemId[i] != 0)
                        itemDefaultCount++;

                    if (i > currencyCost.Count)
                        continue;

                    if (currencyCost.ContainsKey(data.Value.currencyId[i]))
                        if (data.Value.currencyCount[i] == currencyCost[data.Value.currencyId[i]])
                            currencyMatchCount++;
                    if (itemCost.ContainsKey(data.Value.itemId[i]))
                        if (data.Value.itemCount[i] == itemCost[data.Value.itemId[i]])
                            itemMatchCount++;
                }

                if (currencyMatchCount == currencyCost.Count && currencyCost.Count == currencyDefaultCount && itemMatchCount == itemCost.Count && itemCost.Count == itemDefaultCount)
                    return data.Key;
            }
            return 0;
        }
    }
}
