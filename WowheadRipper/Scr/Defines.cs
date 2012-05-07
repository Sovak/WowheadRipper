using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace WowheadRipper
{
    public class Defines
    {
        private const int maxTypeId = 4;
        private const int maxSubTypeId = 4;
        private uint[] maxSubClassTypeId = new uint[maxTypeId];
        private string[] wowhead_raw_name = new string[maxTypeId];
        private string[,] id_name = new string[maxTypeId, maxSubTypeId];
        private string[,] id_db_name = new string[maxTypeId, maxSubTypeId];
        private Regex[,] id_regex = new Regex[maxTypeId, maxSubTypeId];
        private Regex[,] id_count_regex = new Regex[maxTypeId, maxSubTypeId];
        public Queue<string> stream = new Queue<string>();
        public string fileName = "";
        public int programExit = 0;

        public uint GetMaxTypeId() { return maxTypeId; }
        public uint GetMaxSubTypeId(int i) { return maxSubClassTypeId[i]; }
        public int GetValidFlags(UInt32 TypeId, UInt32 Flags)
        {
            int f = 0;
            for (Int32 i = 0; i < 32; i++)
                if ((Flags & (1 << i)) == 1)
                    if (i <= maxSubClassTypeId[i])
                        f++;
            return f;
        }
        public string GetOutputName(UInt32 TypeId, UInt32 subTypeId) { return id_name[TypeId, subTypeId]; }
        public string GetDBName(UInt32 TypeId, UInt32 subTypeId) { return id_db_name[TypeId, subTypeId]; }
        public string GenerateWowheadUrl(UInt32 typeId, UInt32 entry) { return string.Format("http://www.wowhead.com/{0}={1}", wowhead_raw_name[typeId], entry); }
        public Regex GetDataRegex(UInt32 TypeId, UInt32 subTypeId) { return id_regex[TypeId, subTypeId]; }
        public Regex GetTotalCountRegex(UInt32 TypeId, UInt32 subTypeId) { return id_count_regex[TypeId, subTypeId]; }

        public Defines()
        {
            // Zone Parser
            wowhead_raw_name[0] = "zone";
            maxSubClassTypeId[0] = 1;
              // Fishing
              id_name[0, 0] = "fishing";
                id_db_name[0, 0] = "fishing_loot_temlate";
                id_regex[0, 0] = new Regex(@"new Listview\(\{template: 'item', id: 'fishing'.*data: (\[.+\])\}\);");
                id_count_regex[0, 0] = new Regex(@"new Listview\(\{template: 'item', id: 'fishing'.*_totalCount:");

            // Gameobject Parser
            wowhead_raw_name[1] = "object";
            maxSubClassTypeId[1] = 3;
              // Contains
              id_name[1, 0] = "contains";
                id_db_name[1, 0] = "gameobject_loot_template";
                id_regex[1, 0] = new Regex(@"new Listview\(\{template: 'item', id: 'contains'.*data: (\[.+\])\}\);");
                id_count_regex[1, 0] = new Regex(@"new Listview\(\{template: 'item', id: 'contains'.*computeDataFunc:");
              // Mining
              id_name[1, 1] = "mining";
                id_db_name[1, 1] = "gameobject_loot_template";
                id_regex[1, 1] = new Regex(@"new Listview\(\{template: 'item', id: 'mining'.*data: (\[.+\])\}\);");
                id_count_regex[1, 1] = new Regex(@"new Listview\(\{template: 'item', id: 'mining'.*_totalCount:");
              // Herbalism
              id_name[1, 2] = "herbalism";
                id_db_name[1, 2] = "gameobject_loot_template";
                id_regex[1, 2] = new Regex(@"new Listview\(\{template: 'item', id: 'herbalism'.*data: (\[.+\])\}\);");
                id_count_regex[1, 2] = new Regex(@"new Listview\(\{template: 'item', id: 'herbalism'.*_totalCount:");

            // Item Parser
            wowhead_raw_name[2] = "item";
            maxSubClassTypeId[2] = 3; 
              // Contains
              id_name[2, 0] = "contains";
                id_db_name[2, 0] = "item_loot_template";
                id_regex[2, 0] = new Regex(@"new Listview\(\{template: 'item', id: 'contains'.*data: (\[.+\])\}\);");
                id_count_regex[2, 0] = new Regex(@"new Listview\(\{template: 'item', id: 'contains'.*_totalCount:");
              // Milling
              id_name[2, 1] = "milling";
                id_db_name[2, 1] = "milling_loot_template";
                id_regex[2, 1] = new Regex(@"new Listview\(\{template: 'item', id: 'milling'.*data: (\[.+\])\}\);");
                id_count_regex[2, 1] = new Regex(@"new Listview\(\{template: 'item', id: 'milling'.*_totalCount:");
              // Prospecting
              id_name[2, 2] = "prospecting";
                id_db_name[2, 2] = "prospecting_loot_template";
                id_regex[2, 2] = new Regex(@"new Listview\(\{template: 'item', id: 'prospecting'.*data: (\[.+\])\}\);");
                id_count_regex[2, 2] = new Regex(@"new Listview\(\{template: 'item', id: 'prospecting'.*_totalCount:");

            // Creature Paser
            wowhead_raw_name[3] = "npc";
            maxSubClassTypeId[3] = 4;
              // Drop
              id_name[3, 0] = "drop";
                id_db_name[3, 0] = "creature_loot_template";
                id_regex[3, 0] = new Regex(@"new Listview\(\{template: 'item', id: 'drops'.*data: (\[.+\])\}\);");
                id_count_regex[3, 0] = new Regex(@"new Listview\(\{template: 'item', id: 'drops'.*_totalCount:");
              // Skinning
              id_name[3, 1] = "skinning";
                id_db_name[3, 1] = "skinning_loot_tem";
                id_regex[3, 1] = new Regex(@"new Listview\(\{template: 'item', id: 'skinning'.*data: (\[.+\])\}\);");
                id_count_regex[3, 1] = new Regex(@"new Listview\(\{template: 'item', id: 'skinning'.*_totalCount:");
              // Pickpocketing
              id_name[3, 2] = "pickpocketing";
                id_db_name[3, 2] = "pickpocketing_loot_template";
                id_regex[3, 2] = new Regex(@"new Listview\(\{template: 'item', id: 'pickpocketing'.*data: (\[.+\])\}\);");
                id_count_regex[3, 2] = new Regex(@"new Listview\(\{template: 'item', id: 'pickpocketing'.*_totalCount:");
              // Engineering
              id_name[3, 3] = "engineering";
                id_db_name[3, 3] = "engineering_loot_template";
                id_regex[3, 3] = new Regex(@"new Listview\(\{template: 'item', id: 'engineering'.*data: (\[.+\])\}\);");
                id_count_regex[3, 3] = new Regex(@"new Listview\(\{template: 'item', id: 'engineering'.*_totalCount:");
        }
    }
}