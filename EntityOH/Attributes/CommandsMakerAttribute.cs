using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EntityOH.Attributes
{
    public class CommandsMakerAttribute : Attribute
    {
        public string Key { get; set; }

        public CommandsMakerAttribute(string key)
        {
            Key = key;
        }
    }
}
