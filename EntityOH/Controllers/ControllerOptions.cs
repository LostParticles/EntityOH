using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EntityOH.Controllers
{
    public struct COptions
    {
        public string ConnectionKey { get; set; }
        public string TableName { get; set; }


        public static COptions Table(string tableName)
        {
            COptions cp = new COptions();
            cp.TableName = tableName;
            return cp;
        }
    }
}
