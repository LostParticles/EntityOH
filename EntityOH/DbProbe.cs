using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using EntityOH.Controllers.Connections;
using System.Text.RegularExpressions;
using EntityOH.Controllers;

namespace EntityOH
{
    /// <summary>
    /// Database Probe for fast access to the database data.
    /// for any complex data access please <see cref="EntityController"/> Instead.
    /// </summary>
    public class DbProbe
    {

        private readonly SmartConnection _UnderlyingConnection;
        private DbProbe(SmartConnection sm)
        {
            _UnderlyingConnection = sm;
        }


        public static DbProbe Db()
        {
            DbProbe dbp = new DbProbe(SmartConnection.GetSmartConnection());
            return dbp;

        }

        /// <summary>
        /// Get Database Probe by connection key.
        /// </summary>
        /// <param name="connectionKey"></param>
        /// <returns></returns>
        public static DbProbe Db(string connectionKey)
        {
            DbProbe dbp = new DbProbe(SmartConnection.GetSmartConnection(connectionKey));
            return dbp;
            
        }

        

        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name">Name of table/view    i.e.  Person  or  Person{ID=3}</param>
        /// <returns></returns>
        public DataTable this[string name]
        {
            get
            {
                var match = Regex.Match(name, @"(.+)(\{(.*)\})");

                string phName = string.Empty;
                string where = string.Empty;

                if (match.Success)
                {
                    phName = match.Groups[1].Value.Trim();
                    where = match.Groups[3].Value.Trim();
                }
                else
                {
                    phName = name;
                }

                string sql = "SELECT * FROM " + phName;

                if (!string.IsNullOrEmpty(where))
                    sql += " WHERE " + where;


                _UnderlyingConnection.OpenConnection();


                DataTable data = new DataTable();

                try
                {
                    using (var reader = _UnderlyingConnection.ExecuteReader(sql))
                    {
                        // add fields names
                        for (int ix = 0; ix < reader.FieldCount; ix++)
                        {
                            data.Columns.Add(reader.GetName(ix), reader.GetFieldType(ix));                            
                        }

                        while (reader.Read())
                        {

                            var row = data.NewRow();
                            for (int ix = 0; ix < reader.FieldCount; ix++)
                            {
                                row[ix] = reader.GetValue(ix);
                            }

                            data.Rows.Add(row);
                        }
                        reader.Close();
                    }
                }
                finally
                {
                    _UnderlyingConnection.CloseConnection();
                }
                
                return data;
            }
        }



        /// <summary>
        /// Select entities from the database directly.
        /// </summary>
        /// <typeparam name="Entity"></typeparam>
        /// <returns></returns>
        public ICollection<Entity> Select<Entity>()
        {
            ICollection<Entity> all;

            using (var ee = new EntityController<Entity>(_UnderlyingConnection))
            {
                all = ee.Select();
            }

            return all;

        }


        public ICollection<Entity> Select<Entity>(string where)
        {
            ICollection<Entity> all;

            using (var ee = new EntityController<Entity>(_UnderlyingConnection))
            {
                all = ee.Select(where);
            }

            return all;

        }

    }


}
