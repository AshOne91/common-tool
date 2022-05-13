using System;
using System.Collections.Generic;
using System.Text;

namespace common_tool
{
    public class Parameter
    {
        public readonly Dictionary<string, string> _dicActionParam = new Dictionary<string, string>();

        public Parameter(string[] args)
        {
            for (var i = 0; i < args.Length; i++)
            {
                string key = args[i];
                if (i >= args.Length - 1)
                {
                    throw new System.Exception($"invalid args length - {string.Join(", ", args)}");
                }

                string value = args[++i];
                if (value.StartsWith("--") == true)
                {
                    throw new System.Exception($"invalid args value - {value}");
                }

                _dicActionParam.Add(key, value);
            }
        }
    }
}
