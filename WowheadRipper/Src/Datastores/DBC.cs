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
    class DBC
    {
        public static DBCStorage<CurrencyTypesEntry> sCurrencyStore = new DBCStorage<CurrencyTypesEntry>();

        public static void LoadDBCs()
        {
            LoadDBC<DBCStorage<CurrencyTypesEntry>>("CurrencyTypes.dbc", sCurrencyStore);
        }

        private static void LoadDBC<T> (string fileName, T storage)
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
