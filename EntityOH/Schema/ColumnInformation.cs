using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EntityOH.Schema
{
    public class ColumnInformation
    {

        readonly Dictionary<string, string> Values;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="values">key,value string</param>
        public ColumnInformation(Dictionary<string, string> values)
        {
            Values = values;
        }


        public string Name
        {
            get
            {
                return Values["COLUMN_NAME"];
            }
        }

        public string Type
        {
            get
            {
                return Values["DATA_TYPE"];
            }
        }

        public override string ToString()
        {
            return Name + ": " + Type;
        }
    }
}

