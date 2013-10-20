using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EntityOH.Schema
{
    public class TableInformation
    {
        readonly Dictionary<string, string> Values;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="values">key,value string</param>
        public TableInformation(Dictionary<string, string> values)
        {
            Values = values;
        }

        public string Name
        {
            get
            {
                return Values["TABLE_NAME"];
            }
        }

        public TableType Type
        {
            get
            {
                if (Values["TABLE_TYPE"].Contains("TABLE")) return TableType.Table;
                if (Values["TABLE_TYPE"].Contains("View")) return TableType.View;
                return TableType.Unknown;
            }
        }

        public string this[string key]
        {
            get
            {
                return Values[key];
            }
        }


        public ICollection<ColumnInformation> Columns
        {
            get;
            internal set;
        }


        public override string ToString()
        {
            return Name;
        }
    }
}
