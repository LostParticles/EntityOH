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

        /// <summary>
        /// Default key that will create the connection.
        /// </summary>
        public static string DefaultConnectionKey { get; set; }


        /// <summary>
        /// 
        /// </summary>
        static SmartConnection()
        {
            if (ConfigurationManager.ConnectionStrings.Count > 0)
                DefaultConnectionKey = ConfigurationManager.ConnectionStrings[0].Name;

        }

        /// <summary>
        /// Create a connection based on the default connection key.
        /// </summary>
        /// <returns></returns>
        public static SmartConnection GetSmartConnection()
        {
            return new SmartConnection(ConfigurationManager.ConnectionStrings[DefaultConnectionKey].ConnectionString);
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

            if (_InternalConnection.State == ConnectionState.Closed) _InternalConnection.Open();

            return cmd.ExecuteReader(CommandBehavior.CloseConnection);
        }

        public IDataReader ExecuteReader(DbCommand command)
        {
            if (_InternalConnection.State == ConnectionState.Closed) _InternalConnection.Open();

            command.Connection = _InternalConnection;

            return command.ExecuteReader(CommandBehavior.CloseConnection);
        }

        public object ExecuteScalar(DbCommand command)
        {
            if (_InternalConnection.State == ConnectionState.Closed) _InternalConnection.Open();

            command.Connection = _InternalConnection;
            object result = command.ExecuteScalar();

            _InternalConnection.Close();

            return result;
        }


        /// <summary>
        /// Execute the operation without closing the underlieng connection.
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public int ExecuteNonQueryWithoutClose(DbCommand command)
        {
            if (_InternalConnection.State == ConnectionState.Closed) _InternalConnection.Open();

            command.Connection = _InternalConnection;

            int returnvalue = command.ExecuteNonQuery();

            return returnvalue;

        }

        public int ExecuteNonQuery(DbCommand command)
        {
            if (_InternalConnection.State == ConnectionState.Closed) _InternalConnection.Open();

            command.Connection = _InternalConnection;
            
            int returnvalue = command.ExecuteNonQuery();

            _InternalConnection.Close();

            return returnvalue;
        }


        /// <summary>
        /// Make insert command of the entity.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="identityExist"></param>
        /// <returns></returns>
        internal DbCommand GetInsertCommand<Entity>(out EntityFieldRuntime identityFieldRuntime)
        {
            List<string> Fields = new List<string>();

            identityFieldRuntime = null;

            foreach (var fr in EntityRuntime<Entity>.FieldsRuntime)
            {
                if (!fr.Value.Identity)
                {
                    Fields.Add(fr.Value.PhysicalName);
                }
                else
                {
                    identityFieldRuntime = fr.Value;
                }
            }

            string FinalParameters = string.Empty;
            string FinalFields = string.Empty;
            foreach (var f in Fields)
            {
                FinalFields += f;
                FinalFields += ",";

                FinalParameters += "@" + f;
                FinalParameters += ",";

            }

            FinalFields = FinalFields.TrimEnd(',');
            FinalParameters = FinalParameters.TrimEnd(',');

            string InsertStatementTemplate = "INSERT INTO {0} ({1}) VALUES ({2})";

            string InsertStatement = string.Format(InsertStatementTemplate, EntityRuntime<Entity>.PhysicalName, FinalFields, FinalParameters);

            if (identityFieldRuntime != null)
            {

                InsertStatement += "; SELECT @@IDENTITY";
                
            }


            DbCommand command = new SqlCommand(InsertStatement);


            return command;
        }

        internal DbParameter GetParameter(string parameterName, object value)
        {
            SqlParameter sp = new SqlParameter(GetValidParameterName(parameterName), value);
            return sp;
        }

        internal string GetValidParameterName(string parameterName)
        {
            return "@" + parameterName;
        }

        internal DbCommand GetSelectCommand<Entity>()
        {
            string SelectTemplate = "SELECT * FROM " + EntityRuntime<Entity>.PhysicalName + " WHERE {0}";

            string Conditions = string.Empty;

            foreach (var fr in EntityRuntime<Entity>.FieldsRuntime)
            {
                if (fr.Value.Primary)
                {
                    Conditions += fr.Value.PhysicalName + " = @" + fr.Value.PhysicalName + ",";
                }
            }

            if (string.IsNullOrEmpty(Conditions)) throw new NotImplementedException("Selecting entity without primary field is not implemented\nPlease consider adding decorating your entity fields with one or more primary ids.");

            Conditions = Conditions.TrimEnd(',');

            var finalSelect = string.Format(SelectTemplate, Conditions);

            return new SqlCommand(finalSelect);
        }


        internal DbCommand GetCountCommand<Entity>()
        {
            string SelectTemplate = "SELECT COUNT(*) FROM " + EntityRuntimeHelper.FromClause(typeof(Entity));

            var finalSelect = string.Format(SelectTemplate);

            return new SqlCommand(finalSelect);
        }

        internal DbCommand GetCountCommand<Entity>(string whereClause)
        {
            string SelectTemplate = "SELECT COUNT(*) FROM " + EntityRuntimeHelper.FromClause(typeof(Entity));
            SelectTemplate += " WHERE " + whereClause;


            var finalSelect = string.Format(SelectTemplate);

            return new SqlCommand(finalSelect);
        }



        internal DbCommand GetAggregateFunctionCommand<Entity>(string aggregateFunction, string field)
        {
            string SelectTemplate = "SELECT {0}({1}) FROM " + EntityRuntimeHelper.FromClause(typeof(Entity));

            var finalSelect = string.Format(SelectTemplate, aggregateFunction, field);

            return new SqlCommand(finalSelect);
        }

        internal DbCommand GetAggregateFunctionCommand<Entity>(string whereClause, string aggregateFunction, string field)
        {
            string SelectTemplate = "SELECT {0}({1}) FROM " + EntityRuntimeHelper.FromClause(typeof(Entity));
            SelectTemplate += " WHERE " + whereClause;

            var finalSelect = string.Format(SelectTemplate, aggregateFunction, field);

            return new SqlCommand(finalSelect);
        }

        internal DbCommand GetDeleteCommand<Entity>()
        {
            string DeleteTemplate = "DELETE " + EntityRuntime<Entity>.PhysicalName + " WHERE {0}";

            string Conditions = string.Empty;

            foreach (var fr in EntityRuntime<Entity>.FieldsRuntime)
            {
                if (fr.Value.Primary)
                {
                    Conditions += fr.Value.PhysicalName + " = @" + fr.Value.PhysicalName + ",";
                }
            }

            if (string.IsNullOrEmpty(Conditions)) throw new NotImplementedException("Selecting entity without primary field is not implemented\nPlease consider adding decorating your entity fields with one or more primary ids.");

            Conditions = Conditions.TrimEnd(',');

            var finalDelete = string.Format(DeleteTemplate, Conditions);

            return new SqlCommand(finalDelete);
        }


        internal DbCommand GetUpdateCommand<Entity>()
        {
            string UpdateTemplate = "UPDATE " + EntityRuntime<Entity>.PhysicalName + " SET {0} WHERE {1}";

            string Conditions = string.Empty;
            string updatelist = string.Empty;


            foreach (var fr in EntityRuntime<Entity>.FieldsRuntime)
            {
                if (fr.Value.Primary)
                {
                    Conditions += fr.Value.PhysicalName + " = @" + fr.Value.PhysicalName + ",";
                }
                else
                {
                    // normal field
                    if (!fr.Value.Identity)
                    {
                        updatelist += fr.Value.PhysicalName + " = @" + fr.Value.PhysicalName + ",";
                    }
                }
            }

            if (string.IsNullOrEmpty(Conditions)) throw new NotImplementedException("Selecting entity without primary field is not implemented\nPlease consider adding decorating your entity fields with one or more primary ids.");

            Conditions = Conditions.TrimEnd(',');
            updatelist = updatelist.TrimEnd(',');

            var UpdateSelect = string.Format(UpdateTemplate, updatelist, Conditions);

            return new SqlCommand(UpdateSelect);


        }


        internal DbCommand GetStoredProcedureCommand<Entity>(string procName)
        {
            var sq = new SqlCommand(procName);
            sq.CommandType = CommandType.StoredProcedure;

            return sq;
        }


        internal DbCommand GetCreateTableCommand<Entity>()
        {

            string tblPhysicalName = EntityRuntimeHelper.EntityPhysicalName(typeof(Entity));
            string cTable = "CREATE TABLE " + tblPhysicalName + " (\n{0}\n);";

            StringBuilder flds = new StringBuilder();


            foreach (var f in EntityRuntimeHelper.EntityRuntimeFields(typeof(Entity)))
            {
                
                flds.Append(f.PhysicalName);
                
                flds.Append(" ");
                flds.Append(EntityRuntimeHelper.SqlTypeFromCLRType(f.FieldType));

                if (f.Identity) flds.Append(" IDENTITY(1,1)");
                if (f.Primary) flds.Append(" NOT NULL");
                else
                {
                    if (f.FieldType.IsValueType)
                    {
                        //if nullable then set it with null
                        if (f.FieldType.IsGenericType)
                            flds.Append(" NULL");
                        else
                            flds.Append(" NOT NULL");
                    }
                    else
                    {
                        flds.Append(" NULL");
                    }
                }

                flds.Append(",\n");

            }

            // make the constraints

            foreach (var f in EntityRuntimeHelper.EntityRuntimeFields(typeof(Entity)))
            {
                if (f.Primary)
                {
                    flds.Append("constraint [PK_" + tblPhysicalName + "] primary key clustered ([" + f.PhysicalName + "])");
                    flds.Append(',');
                }
            }

            string CreateTable = string.Format(cTable, flds.ToString().TrimEnd(','));


            return new SqlCommand(CreateTable);

            
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

        #region IDisposable Members

        public void Dispose()
        {
            if(_InternalConnection.State == ConnectionState.Open) _InternalConnection.Close();
            _InternalConnection.Dispose();
        }

        #endregion
    }
}
