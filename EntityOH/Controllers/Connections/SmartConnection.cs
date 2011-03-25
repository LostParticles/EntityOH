using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Data.Common;
using System.Data.OleDb;

namespace EntityOH.Controllers.Connections
{
    public class SmartConnection : IDisposable
    {


        private static string _DefaultConnectionString;

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
                _DefaultConnectionString = ConfigurationManager.ConnectionStrings[value].ConnectionString;
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
            return new SmartConnection(_DefaultConnectionString);
        }


        public static SmartConnection GetSmartConnection(string connectionKey)
        {
            return new SmartConnection(ConfigurationManager.ConnectionStrings[connectionKey].ConnectionString);
        }

        public string ConnectionString
        {
            get;
            private set;
        }

        DbConnection _InternalConnection;
        SmartConnectionType _ConnectionType;

        private SmartConnection(string connectionString)
        {

            ConnectionString = connectionString;

            // we need to know the provider.
            if (ConnectionString.Contains("OLEDB"))
            {
                _InternalConnection = new OleDbConnection(ConnectionString);
                _ConnectionType = SmartConnectionType.OleConnection;
            }
            else
            {
                // connection here for sql server.
                _InternalConnection = new SqlConnection(ConnectionString);
                _ConnectionType = SmartConnectionType.SqlServerConnection;
            }

        }
        

        Stack<bool> OpenedConnections = new Stack<bool>();

        public void OpenConnection()
        {
            if (string.IsNullOrEmpty(_InternalConnection.ConnectionString))
            {
                _InternalConnection.ConnectionString = this.ConnectionString;
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
            DbCommand cmd = null;
            if (_ConnectionType == SmartConnectionType.SqlServerConnection)
            {
                cmd = new SqlCommand(text, (SqlConnection)_InternalConnection);
            }
            else
            {
                cmd = new OleDbCommand(text, (OleDbConnection)_InternalConnection);
            }

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

            return new SqlCommand(sql);
        }



        #region Tools


        public object ExecuteScalar(string sql)
        {

            if (this._ConnectionType == SmartConnectionType.OleConnection)
            {
                DbCommand cmd = new OleDbCommand(sql);
                return ExecuteScalar(cmd);
            }
            else if (this._ConnectionType == SmartConnectionType.SqlServerConnection)
            {
                DbCommand cmd = new SqlCommand(sql);
                return ExecuteScalar(cmd);
            }
            else
            {
                throw new NotImplementedException();
            }
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
