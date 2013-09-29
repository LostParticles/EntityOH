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


        /// <summary>
        /// Shows the last sql statement done on the database.
        /// </summary>
        public string LastSqlStatement { get; private set; }

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
        /// Returns data table based on the table/view you enter in the indexer and a condition that 
        /// can be added between brackets Person{ID = 4}
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



                LastSqlStatement = sql;

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
        public ICollection<Entity> Select<Entity>(string where="")
        {
            ICollection<Entity> all = null;

            using (var ee = new EntityController<Entity>(_UnderlyingConnection))
            {
                try
                {
                    if (string.IsNullOrEmpty(where))
                        all = ee.Select();
                    else
                        all = ee.Select(where);
                }
                finally
                {
                    LastSqlStatement = ee.LastSqlStatement;
                }
            }

            return all;
        }


        public ICollection<Entity> SelectWithOrder<Entity>(string where = "", string orderby="")
        {
            ICollection<Entity> all = null;

            using (var ee = new EntityController<Entity>(_UnderlyingConnection))
            {
                try
                {
                    all = ee.SelectWithOrder(where, orderby);
                }
                finally
                {
                    LastSqlStatement = ee.LastSqlStatement;
                }
            }

            return all;
        }



        /// <summary>
        /// Select a page from the database of the required entity.
        /// </summary>
        /// <typeparam name="Entity">Class that resemble the table in the database.</typeparam>
        /// <param name="pageIndex">required page index</param>
        /// <param name="pageItemsCount">how many items in the page.</param>
        /// <param name="totalDiscoveredCount">The total discovered entities from this selection</param>
        /// <param name="where"></param>
        /// <param name="orderby"></param>
        /// <returns></returns>
        public ICollection<Entity> SelectPage<Entity>(int pageIndex, int pageItemsCount, out int totalDiscoveredCount, string where = "", string orderby = "")
        {
            ICollection<Entity> all = null;

            using (var ee = new EntityController<Entity>(_UnderlyingConnection))
            {
                try
                {
                    all = ee.SelectPagedWithOrder(where, orderby, pageIndex, pageItemsCount, out totalDiscoveredCount);
                }
                finally
                {
                    LastSqlStatement = ee.LastSqlStatement;
                }
            }

            return all;
        }
        

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="Entity"></typeparam>
        /// <param name="where"></param>
        /// <returns></returns>
        public ICollection<Entity> SelectDistinct<Entity>(string where = "")
        {
            ICollection<Entity> all = null;

            using (var ee = new EntityController<Entity>(_UnderlyingConnection))
            {
                try
                {
                    if (string.IsNullOrEmpty(where))
                        all = ee.SelectDistinct();
                    else
                        all = ee.SelectDistinct(where);
                }
                finally
                {
                    LastSqlStatement = ee.LastSqlStatement;
                }
            }

            return all;
        }



        /// <summary>
        /// Insert the entity into its corresponding table.
        /// </summary>
        /// <typeparam name="Entity"></typeparam>
        /// <param name="entity"></param>
        public void Insert<Entity>(Entity entity)
        {
            using (var ee = new EntityController<Entity>(_UnderlyingConnection))
            {
                try
                {
                    ee.Insert(entity);
                }
                finally
                {
                    LastSqlStatement = ee.LastSqlStatement;
                }
            }
        }


        /// <summary>
        /// Updates the entity in the database.
        /// </summary>
        /// <typeparam name="Entity"></typeparam>
        /// <param name="entity"></param>
        public void Update<Entity>(Entity entity)
        {
            using (var ee = new EntityController<Entity>(_UnderlyingConnection))
            {
                try
                {
                    ee.Update(entity);
                }
                finally
                {
                    LastSqlStatement = ee.LastSqlStatement;
                }
            }
        }



        /// <summary>
        /// Execute sql statement against database provider.
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public int Execute(string sql)
        {
            return _UnderlyingConnection.Execute(sql);
        }



        /// <summary>
        /// Execute free sql statement and return the result in data table object.
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public DataTable ExecuteDataTable(string sql)
        {

            LastSqlStatement = sql;

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


        /// <summary>
        /// Returns the count of entity based on optional criteria.
        /// </summary>
        /// <typeparam name="Entity"></typeparam>
        /// <param name="where"></param>
        /// <returns></returns>
        public long Count<Entity>(string where = "")
        {
            long count = -1;
            using (var ee = new EntityController<Entity>(_UnderlyingConnection))
            {
                try
                {
                    if (string.IsNullOrEmpty(where))
                        count  = ee.Count();
                    else
                        count = ee.Count(where);
                }
                finally
                {
                    LastSqlStatement = ee.LastSqlStatement;
                }
            }

            return count;
        }


        /// <summary>
        /// Execute the sql statement and return scalar value of the type parameter.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <returns></returns>
        public T ExecuteScalar<T>(string sql)
        {
            LastSqlStatement = sql;

            _UnderlyingConnection.OpenConnection();

            T data = default(T);

            try
            {
                 data  = (T)_UnderlyingConnection.ExecuteScalar(sql);
            }
            finally
            {
                _UnderlyingConnection.CloseConnection();
            }

            return data;
        }


        /// <summary>
        /// Execute the sql and return array of the type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <returns></returns>
        public ICollection<T> ExecuteArray<T>(string sql)
        {
            LastSqlStatement = sql;

            _UnderlyingConnection.OpenConnection();

            List<T> data = new List<T>();

            try
            {
                using (var reader = _UnderlyingConnection.ExecuteReader(sql))
                {
                    while (reader.Read())
                    {
                        data.Add((T)reader.GetValue(0));
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


}
