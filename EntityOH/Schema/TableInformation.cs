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


        /// <summary>
        /// Table/View Type coming from the database
        /// </summary>
        public string DBType
        {
            get
            {
                return Values["TABLE_TYPE"];
            }
        }

        /// <summary>
        /// Mapped table type from the library
        /// </summary>
        public TableType Type
        {
            get
            {
                if (DBType.ToUpperInvariant().Contains("TABLE")) return TableType.Table;
                if (DBType.ToUpperInvariant().Contains("VIEW")) return TableType.View;
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
