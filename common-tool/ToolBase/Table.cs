using System;
using System.Collections.Generic;
using System.Text;

namespace common_tool
{
    class Column
    {
        private string _name;
        private string _type;
        public string Name { get => _name; set => _name = value; }
        public string Type { get => _type; set => _type = value; }
    }
}
