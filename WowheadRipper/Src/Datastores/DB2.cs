using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using DBFilesClient.NET;

namespace WowheadRipper
{
    class DB2
    {
        public static DB2Storage<ItemExtendedCostEntry> sItemExtendedCostStore = new DB2Storage<ItemExtendedCostEntry>();

        public static void LoadDB2s()
        {
            LoadDB2<DB2Storage<ItemExtendedCostEntry>>("ItemExtendedCost.db2", sItemExtendedCostStore);
        }

        private static void LoadDB2<T>(string fileName, T storage)
        {
            try
            {
                FileStream stream = new FileStream(string.Format("{0}{1}", Program.dbcFolder, fileName), FileMode.Open);
                storage.GetType().GetMethod("Load", new Type[] { typeof(FileStream), typeof(LoadFlags) }).Invoke(storage, new object[] { stream, LoadFlags.None });

            }
            catch (Exception e)
            {
                Console.WriteLine(string.Format("Couldnt load dbc {0}, exception: {1}", fileName, e.Message));
            }
        }
    }
}
