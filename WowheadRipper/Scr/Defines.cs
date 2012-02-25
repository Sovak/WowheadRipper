using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace WowheadRipper
{
    public class Defines
    {
        public static string fileName = "";
        public static int programExit = 0;
        public static Queue<string> stream = new Queue<string>();
        private static readonly string[] wowhead_raw_name = new string[] {"zone"};
        public static readonly string[] id_name = new string[] {"fishing"};
        public static readonly string[] db_name = new string[] {"fishing_loot_temlate"};
        public static string GenerateWowheadUrl(UInt32 type, UInt32 entry) { return string.Format("http://www.wowhead.com/{0}={1}", wowhead_raw_name[type], entry); }
        public static Regex GetDataRegex(UInt32 type)
        {
            switch (type)
            {
                case 0:
                    return new Regex(@"new Listview\(\{template: 'item', id: 'fishing'.*data: (\[.+\])\}\);");
                default:
                    return new Regex(@"");
            }
        }
        public static Regex GetTotalCountRegex(UInt32 type)
        {
            switch (type)
            {
                case 0:
                    return new Regex(@"new Listview\(\{template: 'item', id: 'fishing'.*_totalCount:");
                default:
                    return new Regex(@"");
            }
        }
    }
}