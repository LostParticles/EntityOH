using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Configuration;
using System.Data.Common;

namespace EntityOH.Controllers.Connections
{
    public class SmartConnection : IDisposable
    {
        private static string _DefaultConnectionKey;


        /// <summary>
        /// Default key that will create the connection.
        /// </summary>
        public static string DefaultConnectionKey 
        {
            get
            {
                return _DefaultConnectionKey;
            }
            set
            {
                _DefaultConnectionKey = value;
            }
        }


        public static string DefaultConnectionString
        {
            get
            {
                return ConfigurationManager.ConnectionStrings[_DefaultConnectionKey].ConnectionString;
            }
        }
        

        /// <summary>
        /// 
        /// </summary>
        static SmartConnection()
        {
            if (ConfigurationManager.ConnectionStrings.Count > 0)
            {
                DefaultConnectionKey = ConfigurationManager.ConnectionStrings[0].Name;
            }
        }

        /// <summary>
        /// Create a connection based on the default connection key.
        /// </summary>
        /// <returns></returns>
        public static SmartConnection GetSmartConnection()
        {
            return new SmartConnection(DefaultConnectionString);
        }


        public static SmartConnection GetSmartConnection(string connectionKey)
        {
            var cnn = ConfigurationManager.ConnectionStrings[connectionKey];

            return new SmartConnection(cnn.ConnectionString, cnn.ProviderName);
        }

        public string ConnectionString
        {
            get;
            private set;
        }

        /// <summary>
        /// Specify the provider of the database functionality
        /// the default is System.Data.SqlClient  for sql server
        /// other usages is also System.Data.SqlServerCe  for sql server compact.
        /// </summary>
        public string Provider
        {
            get;
            private set;
        }

        DbConnection _InternalConnection;

        public SmartConnection(string connectionString, string provider = "System.Data.SqlClient")
        {

            ConnectionString = connectionString;

            if (string.IsNullOrEmpty(provider)) Provider = "System.Data.SqlClient";
            else Provider = provider;

            _InternalConnection = DbCommandsFactory.GetConnection(Provider, connectionString);

        }

        public DbCommandsMaker<Entity> GetCommandsMaker<Entity>()
        {
            return DbCommandsFactory.GetCommandsMaker<Entity>(Provider);
        }
        

        Stack<bool> OpenedConnections = new Stack<bool>();

        public void OpenConnection()
        {
            if (string.IsNullOrEmpty(_InternalConnection.ConnectionString))
            {
                // the connection has been disposed here so we need to create it again

                _InternalConnection = DbCommandsFactory.GetConnection(Provider, ConnectionString);

                //_InternalConnection.ConnectionString = this.ConnectionString;
                //throw new EntityException("Where is the connection string ????");
            }

            OpenedConnections.Push(true);

            if (_InternalConnection.State == ConnectionState.Closed)
            {
                _InternalConnection.Open();

                //Console.WriteLine("Opening Connection");

                while (_InternalConnection.State != ConnectionState.Open)
                {
                    // wait here until we make sure connection were opened.
                }
            }
            else
            {
                //Console.WriteLine("already opened");
            }
        }

        public void CloseConnection()
        {

            if (OpenedConnections.Count > 0)
            {

                OpenedConnections.Pop();

                if (OpenedConnections.Count == 0)
                {
                    _InternalConnection.Close();

                    //Console.WriteLine("Closing connection");
                    while (_InternalConnection.State != ConnectionState.Closed)
                    {
                        // wait until the connection closed.
                    }

                }
            }
            else
            {
                //Console.WriteLine("Already Closed");
            }
        }

        /// <summary>
        /// Execute the reader command
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public IDataReader ExecuteReader(DbCommand command)
        {
            while (_InternalConnection.State == ConnectionState.Connecting)
            {
            }

            command.Connection = _InternalConnection;

            return command.ExecuteReader(CommandBehavior.Default);

        }


        public IDataReader ExecuteReader(string text)
        {
            DbCommand cmd = DbCommandsFactory.GetCommand(Provider);

            cmd.CommandText = text;

            return ExecuteReader(cmd);
        }

        public object ExecuteScalar(DbCommand command)
        {
            OpenConnection();

            command.Connection = _InternalConnection;
            object result = command.ExecuteScalar();

            CloseConnection();

            return result;
        }

        

        /// <summary>
        /// Execute the operation without closing the underlieng connection.
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public int ExecuteNonQueryWithoutClose(DbCommand command)
        {

            command.Connection = _InternalConnection;

            int returnvalue = command.ExecuteNonQuery();

            return returnvalue;

        }

        public int ExecuteNonQuery(DbCommand command)
        {
            OpenConnection();

            command.Connection = _InternalConnection;
            
            int returnvalue = command.ExecuteNonQuery();

            CloseConnection();

            return returnvalue;
        }


        /// <summary>
        /// Returns command for any select command.
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        internal DbCommand GetExecuteCommand(string sql)
        {

            var cmd = DbCommandsFactory.GetCommand(Provider);
            cmd.CommandText = sql;
            return cmd;

        }



        #region Tools


        public object ExecuteScalar(string sql)
        {
            DbCommand cmd = DbCommandsFactory.GetCommand(Provider);

            return ExecuteScalar(cmd);
        }


        /// <summary>
        /// Execute and sql statement against the database provider.
        /// </summary>
        /// <param name="sql"></param>
        public int Execute(string sql)
        {
            var cmd = this.GetExecuteCommand(sql);

            return this.ExecuteNonQuery(cmd);
        }

        /// <summary>
        /// Reset the identity of the table to the required seed.
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="seed"></param>
        public void ResetIdentity(string tableName, int seed = 0)
        {
            string g = string.Format("DBCC CHECKIDENT ({0}, reseed, {1})", tableName, seed);
            this.Execute(g);
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            CloseConnection();
            _InternalConnection.Dispose();
        }

        #endregion



    }
}
