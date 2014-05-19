using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace WowheadRipper
{
    public static class Defines
    {
        public enum ParserType
        {
            PARSER_TYPE_LOOT,
            PARSER_TYPE_OUTDATED_QUEST,
            PARSER_TYPE_VENDOR,
            PARSER_TYPE_DROPPEDBY
        };

        private const int _maxTypeId = 5;
        private const int _maxSubTypeId = 5;
        private static uint[] _maxSubClassTypeId = new uint[_maxTypeId];
        private static string[] _wowhead_raw_name = new string[_maxTypeId];
        private static string[,] _id_name = new string[_maxTypeId, _maxSubTypeId];
        private static string[,] _id_db_name = new string[_maxTypeId, _maxSubTypeId];
        private static Regex[,] _id_regex = new Regex[_maxTypeId, _maxSubTypeId];
        private static ParserType[,] _parser_type = new ParserType[_maxTypeId, _maxSubTypeId];

        public static uint GetMaxTypeId() { return _maxTypeId; }
        public static uint GetMaxSubTypeId(int i) { return _maxSubClassTypeId[i]; }
        public static int GetValidFlags(UInt32 TypeId, Int32 Flags)
        {
            int f = 0;
            for (Int32 i = 0; i < 32; i++)
                if ((Flags & (1 << i)) != 0)
                    if (i < _maxSubClassTypeId[TypeId])
                        f++;
            return f;
        }

        public static List<int> ExtractFlags(UInt32 typeId, Int32 num)
        {
            List<int> f = new List<int>();
            for (Int32 i = 0; i < 32; i++)
                if ((num & (1 << i)) != 0)
                    if (i < GetMaxSubTypeId((int)typeId))
                        f.Add(i);
            return f;
        }

        public static string GetStreamName(UInt32 TypeId, UInt32 subTypeId) { return _id_name[TypeId, subTypeId]; }
        public static string GetRawName(UInt32 TypeId) { return _wowhead_raw_name[TypeId]; }
        public static string GetDBName(UInt32 TypeId, UInt32 subTypeId) { return _id_db_name[TypeId, subTypeId]; }
        public static string GenerateWowheadUrl(UInt32 typeId, UInt32 entry) { return string.Format("http://www.wowhead.com/{0}={1}", _wowhead_raw_name[typeId], entry); }
        public static string GenerateWowheadFileName(UInt32 typeId, UInt32 entry) { return string.Format("{0}={1}", _wowhead_raw_name[typeId], entry); }
        public static Regex GetDataRegex(UInt32 TypeId, UInt32 subTypeId) { return _id_regex[TypeId, subTypeId]; }
        public static ParserType GetParserType(UInt32 TypeId, UInt32 subTypeId) { return _parser_type[TypeId, subTypeId]; }
        public static string GetStringBetweenTwoOthers(string baseString, string begin, string end)
        {
            begin = StringToRegex(begin);
            end = StringToRegex(end);

            return Regex.Split(baseString, string.Format("(?<={0})(.*?)(?={1})", begin, end))[1];
        }

        public static Match StringContains(string str, string contains)
        {
            return Regex.Match(str, StringToRegex(contains));
        }

        public static string StringToRegex(string str)
        {
            str = str.Replace("=", "\\=");
            str = str.Replace("[", "\\[");
            str = str.Replace("]", "\\]");
            str = str.Replace("/", "\\/");
            str = str.Replace(".", "\\.");
            return str;
        }

        static Defines()
        {
            // Zone Parser
            _wowhead_raw_name[0] = "zone";
            _maxSubClassTypeId[0] = 1;
              // Fishing
              _id_name[0, 0] = "fishing";
                _id_db_name[0, 0] = "fishing_loot_temlate";
                _id_regex[0, 0] = new Regex(@"new Listview\(\{template: 'item', id: 'fishing'.*data: (\[.+\])\}\);");
                _parser_type[0, 0] = ParserType.PARSER_TYPE_LOOT;

            // Gameobject Parser
            _wowhead_raw_name[1] = "object";
            _maxSubClassTypeId[1] = 3;
              // Contains
            _id_name[1, 0] = "contains";
                _id_db_name[1, 0] = "gameobject_loot_template";
                _id_regex[1, 0] = new Regex(@"new Listview\(\{template: 'item', id: 'contains'.*data: (\[.+\])\}\);");
                _parser_type[1, 0] = ParserType.PARSER_TYPE_LOOT;
              // Mining
              _id_name[1, 1] = "mining";
                _id_db_name[1, 1] = "gameobject_loot_template";
                _id_regex[1, 1] = new Regex(@"new Listview\(\{template: 'item', id: 'mining'.*data: (\[.+\])\}\);");
                _parser_type[1, 1] = ParserType.PARSER_TYPE_LOOT;
              // Herbalism
              _id_name[1, 2] = "herbalism";
                _id_db_name[1, 2] = "gameobject_loot_template";
                _id_regex[1, 2] = new Regex(@"new Listview\(\{template: 'item', id: 'herbalism'.*data: (\[.+\])\}\);");
                _parser_type[1, 2] = ParserType.PARSER_TYPE_LOOT;

            // Item Parser
            _wowhead_raw_name[2] = "item";
            _maxSubClassTypeId[2] = 5;
              // Contains
              _id_name[2, 0] = "contains";
                _id_db_name[2, 0] = "item_loot_template";
                _id_regex[2, 0] = new Regex(@"new Listview\(\{template: 'item', id: 'contains'.*data: (\[.+\])\}\);");
                _parser_type[2, 0] = ParserType.PARSER_TYPE_LOOT;
              // Milling
              _id_name[2, 1] = "milling";
                _id_db_name[2, 1] = "milling_loot_template";
                _id_regex[2, 1] = new Regex(@"new Listview\(\{template: 'item', id: 'milling'.*data: (\[.+\])\}\);");
                _parser_type[2, 1] = ParserType.PARSER_TYPE_LOOT;
              // Prospecting
              _id_name[2, 2] = "prospecting";
                _id_db_name[2, 2] = "prospecting_loot_template";
                _id_regex[2, 2] = new Regex(@"new Listview\(\{template: 'item', id: 'prospecting'.*data: (\[.+\])\}\);");
                _parser_type[2, 2] = ParserType.PARSER_TYPE_LOOT;
              // Disenchanting
              _id_name[2, 3] = "disenchanting";
                _id_db_name[2, 3] = "disenchant_loot_template";
                _id_regex[2, 3] = new Regex(@"new Listview\(\{template: 'item', id: 'disenchanting'.*data: (\[.+\])\}\);");
                _parser_type[2, 3] = ParserType.PARSER_TYPE_LOOT;
                // Dropped By
              _id_name[2, 4] = "dropped-by";
                _id_db_name[2, 4] = "creature_loot_template";
                _id_regex[2, 4] = new Regex(@"new Listview\(\{template: 'npc', id: 'dropped-by'.*data: (\[.+\])\}\);");
                _parser_type[2, 4] = ParserType.PARSER_TYPE_DROPPEDBY;

            // Creature Paser
            _wowhead_raw_name[3] = "npc";
            _maxSubClassTypeId[3] = 5;
              // Drop
              _id_name[3, 0] = "drop";
                _id_db_name[3, 0] = "creature_loot_template";
                _id_regex[3, 0] = new Regex(@"new Listview\(\{template: 'item', id: 'drops'.*data: (\[.+\])\}\);");
                _parser_type[3, 0] = ParserType.PARSER_TYPE_LOOT;
              // Skinning
              _id_name[3, 1] = "skinning";
                _id_db_name[3, 1] = "skinning_loot_tem";
                _id_regex[3, 1] = new Regex(@"new Listview\(\{template: 'item', id: 'skinning'.*data: (\[.+\])\}\);");
                _parser_type[3, 1] = ParserType.PARSER_TYPE_LOOT;
              // Pickpocketing
              _id_name[3, 2] = "pickpocketing";
                _id_db_name[3, 2] = "pickpocketing_loot_template";
                _id_regex[3, 2] = new Regex(@"new Listview\(\{template: 'item', id: 'pickpocketing'.*data: (\[.+\])\}\);");
                _parser_type[3, 2] = ParserType.PARSER_TYPE_LOOT;
              // Engineering
              _id_name[3, 3] = "engineering";
                _id_db_name[3, 3] = "engineering_loot_template";
                _id_regex[3, 3] = new Regex(@"new Listview\(\{template: 'item', id: 'engineering'.*data: (\[.+\])\}\);");
                _parser_type[3, 3] = ParserType.PARSER_TYPE_LOOT;
              // Vendor
              _id_name[3, 4] = "vendor";
                _id_db_name[3, 4] = "npc_vendor";
                _id_regex[3, 4] = new Regex(@"new Listview\(\{template: 'item', id: 'sells'.*data: (\[.+\])\}\);");
                _parser_type[3, 4] = ParserType.PARSER_TYPE_VENDOR;

            // Quest Paser
            _wowhead_raw_name[4] = "quest";
            _maxSubClassTypeId[4] = 1;
              // Outdated
              _id_name[4, 0] = "outdated quest";
                _id_db_name[4, 0] = "quest_template";
                _id_regex[4, 0] = new Regex(@"This quest is no longer available within the game");
                _parser_type[4, 0] = ParserType.PARSER_TYPE_OUTDATED_QUEST;
        }
    }
}