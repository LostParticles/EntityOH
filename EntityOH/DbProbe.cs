using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using EntityOH.Controllers.Connections;
using System.Text.RegularExpressions;
using EntityOH.Controllers;
using EntityOH.Schema;
using EntityOH.DbCommandsMakers;

namespace EntityOH
{
    /// <summary>
    /// Database Probe for fast access to the database data.
    /// for any complex data access please <see cref="EntityController"/> Instead.
    /// </summary>
    public class DbProbe:IDisposable
    {
        /// <summary>
        /// Shows the last sql statement done on the database.
        /// </summary>
        public string LastSqlStatement { get; private set; }

        /// <summary>
        /// The smart connection used by the probe.
        /// </summary>
        private readonly SmartConnection _UnderlyingConnection;
        private DbProbe(SmartConnection sm)
        {
            _UnderlyingConnection = sm;
        }


        private DbProbe(string connectionString, string provider)
        {
            _UnderlyingConnection = new SmartConnection(connectionString, provider);
        }

        public static DbProbe Db()
        {
            DbProbe dbp = new DbProbe(SmartConnection.GetSmartConnection());
            return dbp;
        }

        /// <summary>
        /// Get Database Probe by connection key.
        /// </summary>
        /// <param name="connectionKey">the name of connection in the configuration file.</param>
        /// <returns></returns>
        public static DbProbe Db(string connectionKey)
        {
            DbProbe dbp = new DbProbe(SmartConnection.GetSmartConnection(connectionKey));
            return dbp;
        }

        public static DbProbe Db(string connectionString, string provider)
        {
            DbProbe dbp = new DbProbe(connectionString, provider);
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

                LastSqlStatement = sql;

                var data = _UnderlyingConnection.ExecuteDataTable(sql);
                
                return data;
            }
        }

        public ICollection<Entity> SelectParameterized<Entity>(string where, params CommandParameter[] parameters)
        {
            ICollection<Entity> all = null;

            var ee = new EntityController<Entity>(_UnderlyingConnection);
            try
            {
                if (string.IsNullOrEmpty(where))
                    throw new EntityException("Where statement must be feeded for this function");
                else
                    all = ee.SelectParameterized(where, parameters );
            }
            finally
            {
                LastSqlStatement = ee.LastSqlStatement;
            }

            return all;
        }

        /// <summary>
        /// Execute the query and try to map the result to the entity public properties .. direclty without name decoration on the database server side.
        /// </summary>
        /// <typeparam name="Entity"></typeparam>
        /// <param name="sql"></param>
        /// <returns></returns>
        public ICollection<Entity> ExecuteEntities<Entity>(string sql)
        {
            ICollection<Entity> all = null;

            var ee = new EntityController<Entity>(_UnderlyingConnection);
            try
            {
                all = ee.ExecuteEntities(sql);
            }
            finally
            {
                LastSqlStatement = ee.LastSqlStatement;
            }

            return all;

        }

        /// <summary>
        /// Select entities from the database directly.
        /// </summary>
        /// <typeparam name="Entity"></typeparam>
        /// <returns></returns>
        public ICollection<Entity> Select<Entity>(string where="")
        {
            ICollection<Entity> all = null;

            var ee = new EntityController<Entity>(_UnderlyingConnection);
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

            return all;
        }


        public ICollection<Entity> SelectWithOrder<Entity>(string where = "", string orderby="")
        {
            ICollection<Entity> all = null;

            var ee = new EntityController<Entity>(_UnderlyingConnection);
            try
            {
                all = ee.SelectWithOrder(where, orderby);
            }
            finally
            {
                LastSqlStatement = ee.LastSqlStatement;
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
        public ICollection<Entity> SelectPage<Entity>(int pageIndex, int pageItemsCount, string where = "", string orderby = "")
        {
            ICollection<Entity> all = null;
            int totalDiscoveredCount;
            var ee = new EntityController<Entity>(_UnderlyingConnection);
            try
            {
                all = ee.SelectPagedWithOrder(where, orderby, pageIndex, pageItemsCount, out totalDiscoveredCount);
            }
            finally
            {
                LastSqlStatement = ee.LastSqlStatement;
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

            var ee = new EntityController<Entity>(_UnderlyingConnection);
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

            return all;
        }



        /// <summary>
        /// Insert the entity into its corresponding table.
        /// </summary>
        /// <typeparam name="Entity"></typeparam>
        /// <param name="entity"></param>
        public void Insert<Entity>(Entity entity)
        {
            var ee = new EntityController<Entity>(_UnderlyingConnection);
            try
            {
                ee.Insert(entity);
            }
            finally
            {
                LastSqlStatement = ee.LastSqlStatement;
            }
        }


        /// <summary>
        /// Updates the entity in the database.
        /// </summary>
        /// <typeparam name="Entity"></typeparam>
        /// <param name="entity"></param>
        public void Update<Entity>(Entity entity)
        {
            var ee = new EntityController<Entity>(_UnderlyingConnection);
            try
            {
                ee.Update(entity);
            }
            finally
            {
                LastSqlStatement = ee.LastSqlStatement;
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

            var data = _UnderlyingConnection.ExecuteDataTable(sql);

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
            var ee = new EntityController<Entity>(_UnderlyingConnection);
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

            T data = default(T);

            try
            {
                 data  = (T)_UnderlyingConnection.ExecuteScalar(sql);
            }
            finally
            {
                LastSqlStatement = sql;
            }

            return data;
        }


        /// <summary>
        /// Execute the sql and return single array vector of the type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <returns></returns>
        public ICollection<T> ExecuteVector<T>(string sql)
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

       


        public void Delete<Entity>(Entity entity)
        {
            var ee = new EntityController<Entity>(_UnderlyingConnection);
            try
            {
                ee.Delete(entity);
            }
            finally
            {
                LastSqlStatement = ee.LastSqlStatement;
            }
        }

        public void CreateTable<Entity>()
        {
            var ee = new EntityController<Entity>(_UnderlyingConnection);
            try
            {
                ee.CreateTable();
            }
            finally
            {
                LastSqlStatement = ee.LastSqlStatement;
            }
        }

        public void Dispose()
        {
            _UnderlyingConnection.Dispose();
        }

        public void DropTable<Entity>()
        {
            var ee = new EntityController<Entity>(_UnderlyingConnection);
            ee.DropTable();
        }

        public ICollection<TableInformation> Tables
        {
            get
            {
                return _UnderlyingConnection.GetTablesInformation();
            }
        }
    }
}
