using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace WowheadRipper
{

    class WowheadObject
    {
        Dictionary<String, Object> _objectInfo;

        public WowheadObject(Dictionary<String, Object> objectInfo)
        {
            _objectInfo = objectInfo;
        }

        public UInt32 GetId()
        {
            return (UInt32)(int)_objectInfo["id"];
        }

        public String GetName()
        {
            return (String)_objectInfo["name"];
        }

        public String GetFixedName()
        {
            // Remove the first char, some weird number from Wowhead - only sometimes
            return GetName().Remove(0, 1);
        }

        public UInt32 GetCount()
        {
            return (UInt32)(int)_objectInfo["count"];
        }

        public UInt32 GetClass()
        {
            return (UInt32)(int)_objectInfo["classs"];
        }

        public UInt32 GetOutOf()
        {
            return (UInt32)(int)_objectInfo["outof"];
        }

        public Int32 GetQuestId()
        {
            return GetClass() == 12 ? -(Int32)GetId() : (Int32)GetId();
        }

        public Dictionary<UInt32, UInt32> GetCurrencyCost()
        {
            Dictionary<UInt32, UInt32> cost = new Dictionary<uint, uint>();

            // [money(int), [[currencyId, currencyCount], [currencyId, curencyCount]], [[itemId, itemCount], [itemId, itemCount]]]

            if (Contains("cost"))
            {
                Object array = _objectInfo["cost"];

                Object[] costTypes = ObjectToObjectArray(array);
                Object[] currencyCost = ObjectToObjectArray(costTypes[1]);

                foreach (Object currencyArray in currencyCost)
                {
                    UInt32[] finalCost = ObjectToUInt32Array(currencyArray);
                    cost.Add(finalCost[0], finalCost[1]);
                }
            }

            return cost;
        }

        public Dictionary<UInt32, UInt32> GetItemCost()
        {
            Dictionary<UInt32, UInt32> cost = new Dictionary<uint, uint>();

            // [money(int), [[currencyId, currencyCount], [currencyId, curencyCount]], [[itemId, itemCount], [itemId, itemCount]]]

            if (Contains("cost"))
            {
                Object array = _objectInfo["cost"];

                Object[] costTypes = ObjectToObjectArray(array);
                Object[] ItemCost = ObjectToObjectArray(costTypes[2]);

                foreach (Object itemArray in ItemCost)
                {
                    UInt32[] finalCost = ObjectToUInt32Array(itemArray);
                    cost.Add(finalCost[0], finalCost[1]);
                }
            }

            return cost;
        }

        public UInt32 GetMoneyCost()
        {
            // [money(int), [[currencyId, currencyCount], [currencyId, curencyCount]], [[itemId, itemCount], [itemId, itemCount]]]

            if (Contains("cost"))
            {
                Object array = _objectInfo["cost"];

                Object[] costTypes = ObjectToObjectArray(array);
                return (UInt32)(int)costTypes[0];
            }

            return 0;
        }

        public UInt32[] GetStackArray()
        {
            UInt32[] array = {1, 1};

            if (Contains("stack"))
            {
                array = ObjectToUInt32Array(_objectInfo["stack"]);
            }

            return array;
        }

        private Object[] ObjectToObjectArray(Object objectArray)
        {
            Array array = (Array)objectArray;

            Object[] realObjectArray = new Object[array.Length];
            Array.Copy(array, realObjectArray, realObjectArray.Length);

            return realObjectArray;
        }

        private UInt32[] ObjectToUInt32Array(Object objectArray)
        {
            Object[] array = ObjectToObjectArray((Array)objectArray);
            UInt32[] uintArray = new UInt32[array.Length];

            for (int i = 0; i < array.Length; i++)
                uintArray[i] = (UInt32)(int)array[i];

            return uintArray;
        }

        private bool Contains(String key)
        {
            return _objectInfo.ContainsKey(key);
        }

    }
}
