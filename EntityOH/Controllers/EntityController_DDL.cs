using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;

using EntityOH.Attributes;
using EntityOH.Controllers.Connections;

using System.Data.Common;
using EntityOH.Schema;


namespace EntityOH.Controllers
{
    public partial class EntityController<Entity> : IDisposable
    {

        public ICollection<TableInformation> GetTablesInformation()
        {
            DataTable dt;

            string gg = DatabaseCommands.TablesSchemaSelectStatement;

            if (string.IsNullOrEmpty(gg))
            {
                dt = _Connection.GetSchemaTables();
            }
            else
            {
                dt = _Connection.ExecuteDataTable(gg);
            }

            List<TableInformation> Tables = new List<TableInformation>();

            foreach (DataRow row in dt.Rows)
            {
                Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    values.Add(dt.Columns[i].ColumnName, row[i].ToString());
                }

                Tables.Add(new TableInformation(values));
            }

            foreach (var tbl in Tables)
            {
                DataTable dcs;
                if (string.IsNullOrEmpty(gg))
                {
                    dcs = _Connection.GetSchemaTableColumns(tbl.Name);
                }
                else
                {
                    dcs = _Connection.ExecuteDataTable("SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '" + tbl.Name + "'");
                }

                List<ColumnInformation> columns = new List<ColumnInformation>();
                foreach (DataRow row in dcs.Rows)
                {
                    Dictionary<string, string> c_values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                    for (int i = 0; i < dcs.Columns.Count; i++)
                    {
                        c_values.Add(dcs.Columns[i].ColumnName, row[i].ToString());
                    }

                    columns.Add(new ColumnInformation(c_values));
                }
                tbl.Columns = columns;
            }
            return Tables;
        }


        /// <summary>
        /// Create the table in the database based on the entity class anatomy ;)
        /// </summary>
        public void CreateTable()
        {
            ExecutePreOperations();

            var cmd = DatabaseCommands.GetCreateTableCommand();

            _LastSqlStatement = cmd.CommandText;
            _LastReturnValue = _Connection.ExecuteNonQuery(cmd);
        }
    }
}