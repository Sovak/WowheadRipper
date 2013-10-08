using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace WowheadRipper
{
    class WowheadSerializer
    {
        private List<String> _content;
        private UInt32 _typeId;
        private UInt32 _subTypeId;

        public Object[] Objects;
        public UInt32 TotalCount;

        public WowheadSerializer(List<String> content, UInt32 typeId, UInt32 subTypeId)
        {
            _content = content;
            _typeId = typeId;
            _subTypeId = subTypeId;

            SerializeData();

        }
 
        String GetFixedLine(Int32 lineNumber)
        {
            String regexData = _content[lineNumber];
            Int32 subIndex = 0;

            // Will throw out an exception, we surelly wont need this anyway
            if (regexData.Length < 2)
                return regexData;

            // Wowhead is diving the lines for some funky reason - if , is the last character of listview, read new line
            while (regexData[regexData.Length - 1] == ',')
            {
                regexData = String.Format("{0}{1}", regexData, _content[lineNumber + ++subIndex]);
            }

            regexData = regexData.Replace("[,", "[0,");
            regexData = regexData.Replace(",]", ",0]");
            // Some fields needs to be converted to strings
            regexData = regexData.Replace("undefined", "\"undefined\"");
            regexData = regexData.Replace("cost", "\"cost\"");

            return regexData;
        }

        void SerializeData()
        {
            for (Int32 i = 0; i < _content.Count; i++)
            {
                String line = GetFixedLine(i);
                Match match = Program.Def.GetDataRegex(_typeId, _subTypeId).Match(line);

                if (match.Success)
                {
                    JavaScriptSerializer json = new JavaScriptSerializer()
                    {
                        MaxJsonLength = int.MaxValue
                    };

                    if (line.Contains("_totalCount"))
                        TotalCount = UInt32.Parse(Program.Def.GetStringBetweenTwoOthers(line, "_totalCount: ", ","));
                    else
                        TotalCount = 0;

                    Objects = (Object[])json.DeserializeObject(match.Groups[1].Captures[0].Value);
                }
            }
        }
    }
}
