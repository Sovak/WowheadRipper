using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Threading;
using System.IO;

namespace WowheadRipper
{
    partial class Program
    {
        static bool isInitialized = false;
        private static readonly Dictionary<int, Action<uint, uint, uint, List<string>>> Rippers =
            new Dictionary<int, Action<uint, uint, uint, List<string>>>();

        public static void Initialize()
        {
            var asm = Assembly.GetExecutingAssembly();
            var types = asm.GetTypes();
            foreach (var type in types)
            {
                if (!type.IsAbstract)
                    continue;

                if (!type.IsPublic)
                    continue;

                var methods = type.GetMethods();

                foreach (var method in methods)
                {
                    if (!method.IsPublic)
                        continue;
                    DoAttributes(method, typeof(RipperAttribute), Rippers);
                }
            }
        }

        private static bool DoAttributes(MethodInfo method, Type type, Dictionary<int, Action<uint, uint, uint, List<string>>> Rippers)
        {
            var tmp = method.GetCustomAttributes(type, false);

            object[] attrs;
            if (type == typeof(RipperAttribute))
                attrs = (RipperAttribute[])tmp;
            else if (type == typeof(RipperAttribute))
                attrs = (RipperAttribute[])tmp;
            else
                return false;

            if (attrs.Length <= 0)
                return false;

            var parms = method.GetParameters();

            /*if (parms.Length != 4)
                return false;

            if (parms[0].ParameterType != typeof(uint))
                return false;

            if (parms[1].ParameterType != typeof(uint))
                return false;

            if (parms[2].ParameterType != typeof(uint))
                return false;
            
            if (parms[3].ParameterType != typeof(List<string>))
                return false;*/

            foreach (var attr in attrs)
            {
                int rType = (int)((RipperAttribute)attr).Type;

                var del = (Action<uint, uint, uint, List<string>>)Delegate.CreateDelegate(typeof(Action<uint, uint, uint, List<string>>), method);

                Rippers[rType] = del;
            }
            return true;
        }

        public static void Ripp(Defines.ParserType type, uint entry, uint typeId, uint subTypeId, List<string> content)
        {
            if (!isInitialized)
            {
                Initialize();
                isInitialized = true;
            }

            if (Rippers.ContainsKey((int)type))
                Rippers[(int)type](entry, typeId, subTypeId, content);
        }
    }
}
