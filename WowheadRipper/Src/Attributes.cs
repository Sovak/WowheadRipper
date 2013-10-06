using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WowheadRipper
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class RipperAttribute : Attribute
    {
        public RipperAttribute(Defines.ParserType type)
        {
            Type = type;
        }

        public Defines.ParserType Type { get; private set; }
    }
}
