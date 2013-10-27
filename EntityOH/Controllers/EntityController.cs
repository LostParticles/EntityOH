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
using System.Diagnostics;
using EntityOH.DbCommandsMakers;
using System.Text.RegularExpressions;


namespace EntityOH.Controllers
{

    /// <summary>
    /// The entity controller that is responsible in getting entities from database.
    /// </summary>
    /// <typeparam name="Entity"></typeparam>
    public partial class EntityController<Entity> : IDisposable
    {

        readonly DbCommandsMaker<Entity> DatabaseCommands;

        /// <summary>
        /// Current Entity Type
        /// </summary>
        public static Type EntityType
        {
            get
            {
                return typeof(Entity);
            }
        }

        private SmartConnection _Connection;

        public SmartConnection UnderlyingConnection
        {
            get
            {
                return _Connection;
            }
        }


        #region constructors

        public EntityController()
        {
            _Connection = SmartConnection.GetSmartConnection();
            DatabaseCommands = _Connection.GetCommandsMaker<Entity>();
        }

        public EntityController(string connectionKey)
        {
            _Connection = SmartConnection.GetSmartConnection(connectionKey);
            DatabaseCommands = _Connection.GetCommandsMaker<Entity>();
        }

        internal EntityController(SmartConnection sm)
        {
            _Connection = sm;
            DatabaseCommands = _Connection.GetCommandsMaker<Entity>();
        }

        /// <summary>
        /// Controller constructor for extra options in initating controller.
        /// </summary>
        /// <param name="options"></param>
        public EntityController(COptions options)
        {
            if (!string.IsNullOrEmpty(options.ConnectionKey))
            {
                _Connection = SmartConnection.GetSmartConnection(options.ConnectionKey);
                DatabaseCommands = _Connection.GetCommandsMaker<Entity>();
                DatabaseCommands.EntityRuntimeInformation.RunningPhysicalName = options.TableName;
            }
            else
            {
                _Connection = SmartConnection.GetSmartConnection();
                DatabaseCommands = _Connection.GetCommandsMaker<Entity>();
            }
        }

        #endregion

        private string _FieldsList = string.Empty;
        public string FieldsList
        {
            get
            {
                if (string.IsNullOrEmpty(_FieldsList))
                {
                    StringBuilder sb = new StringBuilder();
                    var fls = DatabaseCommands.EntityRuntimeInformation.RunningFieldsList;

                    foreach (var fl in fls)
                    {
                        sb.Append(fl);
                        sb.Append(',');
                    }

                    _FieldsList = sb.ToString().TrimEnd(',');
                }

                return _FieldsList;
            }
        }

        /// <summary>
        /// Form the from clause with any required joins
        /// </summary>
        public string FromExpression
        {
            get
            {
                return DatabaseCommands.EntityRuntimeInformation.RunningFromClause;
            }
        }

        public string GroupByExpression
        {
            get
            {
                return DatabaseCommands.EntityRuntimeInformation.RunningGroupByExpression;
            }
        }



        /// <summary>
        /// Hold some actions that is cleared after the next command of controller executed.
        /// </summary>
        private List<string> VolatilePreOperationsStatements = new List<string>();


        /// <summary>
        /// Hold static actions that executed every time any command of controller executed.
        /// </summary>
        private List<string> StaticPreOperationsStatements = new List<string>();



        /// <summary>
        /// Add a volatile pre opertations before executing any controller action.
        /// the operations are won't be remain after the controller action.
        /// </summary>
        /// <param name="sql"></param>
        public void PreExecute(string sql)
        {
            VolatilePreOperationsStatements.Add(sql);
        }

        public void StaticPreExecute(string sql)
        {
            StaticPreOperationsStatements.Add(sql);
        }

        private void ExecutePreOperations()
        {
            _Connection.OpenConnection();

            foreach (var op in StaticPreOperationsStatements)
            {
                var cmd = _Connection.GetExecuteCommand(op);

                _Connection.ExecuteNonQueryWithoutClose(cmd);
            }

            foreach (var op in VolatilePreOperationsStatements)
            {
                var cmd = _Connection.GetExecuteCommand(op);

                _Connection.ExecuteNonQueryWithoutClose(cmd);
            }

            VolatilePreOperationsStatements.Clear();
        }



        /// <summary>
        /// Get all the records of entity in database.
        /// </summary>
        /// <returns></returns>
        public ICollection<Entity> Select()
        {
            ExecutePreOperations();

            string SelectAllStatement = "SELECT {0} FROM {1}";

            string SelectAll = string.Format(SelectAllStatement, FieldsList, FromExpression);

            if (!string.IsNullOrEmpty(GroupByExpression))
                SelectAll += " GROUP BY " + GroupByExpression;


            _LastSqlStatement = SelectAll;

            List<Entity> ets = new List<Entity>();
            try
            {
                using (var reader = _Connection.ExecuteReader(SelectAll))
                {
                    while (reader.Read())
                    {
                        ets.Add(EntityRuntime<Entity>.MappingFunction(reader));
                    }
                    reader.Close();
                }
            }
            finally
            {
                _Connection.CloseConnection();
            }

            return ets;
        }


        /// <summary>
        /// Get all the records of entity in database.
        /// </summary>
        /// <returns></returns>
        public ICollection<Entity> SelectDistinct()
        {
            ExecutePreOperations();

            string SelectAllStatement = "SELECT DISTINCT {0} FROM {1}";

            string SelectAll = string.Format(SelectAllStatement, FieldsList, FromExpression);

            if (!string.IsNullOrEmpty(GroupByExpression))
                SelectAll += " GROUP BY " + GroupByExpression;


            _LastSqlStatement = SelectAll;
            List<Entity> ets = new List<Entity>();


            using (var reader = _Connection.ExecuteReader(SelectAll))
            {
                while (reader.Read())
                {
                    ets.Add(EntityRuntime<Entity>.MappingFunction(reader));
                }
                reader.Close();
            }
            
            _Connection.CloseConnection();
            return ets;
        }


        
        /// <summary>
        /// Select paged data based on parameters.
        /// </summary>
        /// <param name="pageIndex"></param>
        /// <param name="pageItemsCount"></param>
        /// <param name="totalDiscoveredCount"></param>
        /// <returns></returns>
        public ICollection<Entity> SelectPaged(int pageIndex, int pageItemsCount, out int totalDiscoveredCount)
        {

            ExecutePreOperations();


            string pid = DatabaseCommands.EntityRuntimeInformation.RunningPhysicalName + "." +
                EntityRuntime<Entity>.FieldsRuntime.First((fr) => fr.Value.Primary == true).Value.PhysicalName;

            string SelectAllStatement = "SELECT ROW_NUMBER() OVER (ORDER BY " + pid + ") AS Row_Count, {0} FROM {1}";
            
            string SelectAll = string.Format(SelectAllStatement, FieldsList, FromExpression);

            if (!string.IsNullOrEmpty(GroupByExpression))
                SelectAll += " GROUP BY " + GroupByExpression;


            string SelectPaged = "SELECT * FROM ({1}) AS PagedQuery WHERE Row_Count BETWEEN {2} and {3}";


            int fromRow = pageIndex * pageItemsCount;

            int toRow = fromRow + pageItemsCount;

            fromRow++;  // increase one to align the first item on the first of the page.

            string FinalSelect = string.Format(SelectPaged, FieldsList, SelectAll, fromRow, toRow);

            totalDiscoveredCount = (int)_Connection.ExecuteScalar(DatabaseCommands.GetCountCommand());

            _LastSqlStatement = FinalSelect;
            List<Entity> ets = new List<Entity>();
            using (var reader = _Connection.ExecuteReader(FinalSelect))
            {
                while (reader.Read())
                {
                    ets.Add(EntityRuntime<Entity>.MappingFunction(reader));
                }
                reader.Close();
            }
            _Connection.CloseConnection();

            return ets;


        }


        /// <summary>
        /// Raise exception if it founds semicolon in the where text :)
        /// </summary>
        /// <param name="where"></param>
        /// <returns></returns>
        public string CheckWhere(string where)
        {
            if (where.Contains(';'))
            {
                throw new EntityException("Semicolon character is not permitted in raw WHERE statements to avoid sql injection.\nTry using parameterized WHERE instead");
            }

            if (where.Contains('\''))
            {
                throw new EntityException("for sequrity reasons, single qoutation cannot be used in the raw WHERE statement to avoid sql injection.\nTry using parameterized WHERE instead");
            }

            if (where.Contains("--"))
            {
                throw new EntityException("double dash [--] is not permitted in the where statement to avoid sql injection\nTry using parameterized WHERE instead");
            }

            if (where.Contains("#"))
            {
                throw new EntityException("hash [#] is not permitted in the where statement to avoid sql injection\nTry using parameterized WHERE instead");
            }

            string[] ProhibitedKeywords = 
            {
                "DROP"
                , "INSERT"
                // , "SELECT"
                , "DELETE"
                , "TRUNCATE"
                , "ALTER"
                , "CREATE"
                , "UPDATE"
                , "ENABLE"
                , "DISABLE"
                , "COMMENT"
                , "RENAME"
                , "MERGE"
                , "CALL"
                , "EXPLAIN"
                , "LOCK"
                , "GRANT"
                , "REVOKE"
                , "COMMIT"
                , "SAVEPOINT"
                , "ROLLBACK"
                , "SET"
                , "EXEC"
                , "EXECUTE"
            };

            bool cc = false;
            
            foreach(var pro in ProhibitedKeywords)
            {
                cc = cc || Regex.Match(where, @"\s+" + pro + @"\s+", RegexOptions.IgnoreCase).Success;
            }



            if (cc)
                throw new EntityException(
                    "certain sql keywords are prohibited when written in raw [WHERE] statement to avoid sql injection\nTry using parameterized WHERE instead\nVulnerable Where Statement := \"" 
                    + where
                    + "\"");


            return where;
        }

        /// <summary>
        /// Incorporating parameters in the where statement ..
        /// this function must be used to avoid sql injection, specially when the codition is taken from the user interface.
        /// </summary>
        /// <param name="whereClause"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public ICollection<Entity> SelectParametrized(string whereClause, params CommandParameter[] parameters)
        {

            ExecutePreOperations();

            string SelectAllStatement = "SELECT {0} FROM {1}";

            string SelectAll = string.Format(SelectAllStatement, FieldsList, FromExpression);

            if (!string.IsNullOrEmpty(GroupByExpression))
                SelectAll += " GROUP BY " + GroupByExpression;


            _LastSqlStatement = SelectAll;

            List<Entity> ets = new List<Entity>();

            var SelectCommand = _Connection.GetCommand();

            if (!string.IsNullOrEmpty(whereClause))
            {
                if (parameters.Length > 0)
                {
                    // add parameter values to the statement
                    foreach (var par in parameters)
                    {
                        SelectCommand.Parameters.Add(DatabaseCommands.GetParameter(par.Name, par.Value));
                    }
                    SelectAll += " WHERE " + whereClause;
                }
                else
                {
                    SelectAll += " WHERE " + CheckWhere(whereClause);
                }
            }


            SelectCommand.CommandText = SelectAll;


            using (var reader = SelectCommand.ExecuteReader())
            {

                while (reader.Read())
                {
                    ets.Add(EntityRuntime<Entity>.MappingFunction(reader));
                }

                reader.Close();
            }

            SelectCommand.Dispose();

            _Connection.CloseConnection();
            return ets;
        }

        /// <summary>
        /// Select based on where condition.
        /// </summary>
        /// <param name="whereClause"></param>
        /// <returns></returns>
        public ICollection<Entity> Select(string whereClause)
        {

            ExecutePreOperations();

            string SelectAllStatement = "SELECT {0} FROM {1}";

            string SelectAll = string.Format(SelectAllStatement, FieldsList, FromExpression);
            
            if (!string.IsNullOrEmpty(GroupByExpression))
                SelectAll += " GROUP BY " + GroupByExpression;


            _LastSqlStatement = SelectAll;

            List<Entity> ets = new List<Entity>();

            var SelectCommand = _Connection.GetCommand();

            if (!string.IsNullOrEmpty(whereClause))
            {
                SelectAll += " WHERE " + CheckWhere(whereClause);
            }
            
            SelectCommand.CommandText = SelectAll;
            using (var reader = SelectCommand.ExecuteReader())
            {

                while (reader.Read())
                {
                    ets.Add(EntityRuntime<Entity>.MappingFunction(reader));
                }

                reader.Close();
            }

            SelectCommand.Dispose();

            _Connection.CloseConnection();
            return ets;
        }


        /// <summary>
        /// Select based on where condition and order by 
        /// </summary>
        /// <param name="whereClause"></param>
        /// <param name="orderByClause"></param>
        /// <returns></returns>
        public ICollection<Entity> SelectWithOrder(string whereClause, string orderByClause)
        {

            ExecutePreOperations();

            string SelectAllStatement = "SELECT {0} FROM {1}";

            string SelectAll = string.Format(SelectAllStatement, FieldsList, FromExpression);

            if (!string.IsNullOrEmpty(GroupByExpression))
                SelectAll += " GROUP BY " + GroupByExpression;

            if (!string.IsNullOrEmpty(whereClause))
                SelectAll += " WHERE " + whereClause;

            if(!string.IsNullOrEmpty(orderByClause))
                SelectAll += " ORDER BY " + orderByClause;


            _LastSqlStatement = SelectAll;
            List<Entity> ets = new List<Entity>();


            using (var reader = _Connection.ExecuteReader(SelectAll))
            {

                while (reader.Read())
                {
                    ets.Add(EntityRuntime<Entity>.MappingFunction(reader));
                }

                reader.Close();
            }


            _Connection.CloseConnection();
            return ets;
        }


        /// <summary>
        /// Select based on where condition.
        /// </summary>
        /// <param name="whereClause"></param>
        /// <returns></returns>
        public ICollection<Entity> SelectDistinct(string whereClause)
        {

            ExecutePreOperations();

            string SelectAllStatement = "SELECT DISTINCT {0} FROM {1}";

            string SelectAll = string.Format(SelectAllStatement, FieldsList, FromExpression);

            if (!string.IsNullOrEmpty(GroupByExpression))
                SelectAll += " GROUP BY " + GroupByExpression;

            if (!string.IsNullOrEmpty(whereClause))
                SelectAll += " WHERE " + whereClause;


            _LastSqlStatement = SelectAll;

            List<Entity> ets = new List<Entity>();
            using (var reader = _Connection.ExecuteReader(SelectAll))
            {

                while (reader.Read())
                {
                    ets.Add(EntityRuntime<Entity>.MappingFunction(reader));
                }

                reader.Close();
            }

            
            _Connection.CloseConnection();
            return ets;
        }


        /// <summary>
        /// Select by paging enabled.
        /// </summary>
        /// <param name="whereClause"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageItemsCount"></param>
        /// <param name="totalDiscoveredCount"></param>
        /// <returns></returns>
        public ICollection<Entity> SelectPaged(string whereClause, int pageIndex, int pageItemsCount, out int totalDiscoveredCount)
        {

            ExecutePreOperations();


            string pid = DatabaseCommands.EntityRuntimeInformation.RunningPhysicalName + "." +
                EntityRuntime<Entity>.FieldsRuntime.First((fr) => fr.Value.Primary == true).Value.PhysicalName;

            string SelectAllStatement = "SELECT ROW_NUMBER() OVER (ORDER BY " + pid + ") AS Row_Count, {0} FROM {1}";

            string SelectAll = string.Format(SelectAllStatement, FieldsList, FromExpression);

            if (!string.IsNullOrEmpty(GroupByExpression))
                SelectAll += " GROUP BY " + GroupByExpression;

            if (!string.IsNullOrEmpty(whereClause))
                SelectAll += " WHERE " + whereClause;


            string SelectPaged = "SELECT * FROM ({1}) AS PagedQuery WHERE Row_Count BETWEEN {2} and {3}";


            int fromRow = pageIndex * pageItemsCount;

            int toRow = fromRow + pageItemsCount;

            fromRow++;  // increase one to align the first item on the first of the page.

            string FinalSelect = string.Format(SelectPaged, FieldsList, SelectAll, fromRow, toRow);

            totalDiscoveredCount = (int)_Connection.ExecuteScalar(DatabaseCommands.GetCountCommand(whereClause));

            _LastSqlStatement = FinalSelect;

            List<Entity> ets = new List<Entity>();

            using (var reader = _Connection.ExecuteReader(FinalSelect))
            {
                while (reader.Read())
                {
                    ets.Add(EntityRuntime<Entity>.MappingFunction(reader));
                }
                reader.Close();
            }


            _Connection.CloseConnection();

            return ets;


        }


        /// <summary> 
        /// Select by paging enabled with where and order by clauses
        /// </summary>
        /// <param name="whereClause"></param>
        /// <param name="orderByClause"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageItemsCount"></param>
        /// <param name="totalDiscoveredCount"></param>
        /// <returns></returns>
        public ICollection<Entity> SelectPagedWithOrder(string whereClause, string orderByClause
            , int pageIndex, int pageItemsCount
            , out int totalDiscoveredCount)
        {

            ExecutePreOperations();

            
            // The paging required ROW_NUMBER() function OVER (ORDER BY clause)
            // first thing .. the primary id is used as orderby clause as default 
            // but if the user specified something other than that .. it is used instead.


            
            string OrderByDefault = string.Empty;

            if (!string.IsNullOrEmpty(orderByClause))
            {
                OrderByDefault = orderByClause;
            }
            else
            {

                OrderByDefault = DatabaseCommands.EntityRuntimeInformation.RunningPhysicalName + "." +
                    EntityRuntime<Entity>.FieldsRuntime.FirstOrDefault((fr) => fr.Value.Primary == true).Value.PhysicalName;

                if (string.IsNullOrEmpty(OrderByDefault))
                {
                    throw new EntityException("SelectPagedWithOrder Function needs the entity to have a field marked with primary to order based on it.\nEither mark one of the fields as primary field, or write down the required order by clause while calling this function");
                }
            }

            string SelectAllStatement = "SELECT ROW_NUMBER() OVER (ORDER BY " + OrderByDefault + ") AS Row_Count, {0} FROM {1}";

            string SelectAll = string.Format(SelectAllStatement, FieldsList, FromExpression);

            if (!string.IsNullOrEmpty(GroupByExpression))
                SelectAll += " GROUP BY " + GroupByExpression;

            if (!string.IsNullOrEmpty(whereClause))
                SelectAll += " WHERE " + whereClause;


            string SelectPaged = "SELECT * FROM ({1}) AS PagedQuery WHERE Row_Count BETWEEN {2} and {3}";


            int fromRow = pageIndex * pageItemsCount;

            int toRow = fromRow + pageItemsCount;

            fromRow++;  // increase one to align the first item on the first of the page.

            string FinalSelect = string.Format(SelectPaged, FieldsList, SelectAll, fromRow, toRow);




            totalDiscoveredCount = (int)_Connection.ExecuteScalar(DatabaseCommands.GetCountCommand(whereClause));

            _LastSqlStatement = FinalSelect;

            List<Entity> ets = new List<Entity>();

            using (var reader = _Connection.ExecuteReader(FinalSelect))
            {
                while (reader.Read())
                {
                    ets.Add(EntityRuntime<Entity>.MappingFunction(reader));
                }
                reader.Close();
            }


            _Connection.CloseConnection();

            return ets;
        }


        /// <summary>
        /// Get the count.
        /// </summary>
        /// <returns></returns>
        public long Count()
        {
            ExecutePreOperations();

            long result = 0;
            using (var CountCommand = DatabaseCommands.GetCountCommand())
            {
                _LastSqlStatement = CountCommand.CommandText;
                 result = long.Parse(_Connection.ExecuteScalar(CountCommand).ToString());
                 
            }
            return result;
        }


        /// <summary>
        /// gets the count of entities based on criteria
        /// </summary>
        /// <param name="whereClause"></param>
        /// <returns></returns>
        public long Count(string whereClause)
        {
            ExecutePreOperations();

            long result = 0;
            using (var CountCommand = DatabaseCommands.GetCountCommand(whereClause))
            {
                _LastSqlStatement = CountCommand.CommandText;
                result = long.Parse(_Connection.ExecuteScalar(CountCommand).ToString());
            }
            return result;
        }


        /// <summary>
        /// Get the sum of specific field or zero if return value is null.
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        public double Sum(string field)
        {
            ExecutePreOperations();

            double result = 0;
            using (var SumCommand = DatabaseCommands.GetAggregateFunctionCommand("SUM", field))
            {
                _LastSqlStatement = SumCommand.CommandText;
                var reo = _Connection.ExecuteScalar(SumCommand);
                
                if (!reo.Equals(DBNull.Value))
                {
                    result = double.Parse(reo.ToString());
                }
            }
            return result;
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="field"></param>
        /// <param name="where"></param>
        /// <returns></returns>
        public double Sum(string field, string where)
        {
            ExecutePreOperations();

            double result = 0;
            using (var SumCommand = DatabaseCommands.GetAggregateFunctionCommand(where, "SUM", field))
            {
                _LastSqlStatement = SumCommand.CommandText;
                var reo = _Connection.ExecuteScalar(SumCommand);

                if (!reo.Equals(DBNull.Value))
                {
                    result = double.Parse(reo.ToString());
                }
            }
            return result;
        }


        /// <summary>
        /// Insert entity into the database.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public void Insert(Entity entity)
        {
            ExecutePreOperations();

            EntityFieldRuntime IdentityFieldRuntime;


            using (DbCommand command = DatabaseCommands.GetInsertCommand(out IdentityFieldRuntime))
            {
                _LastSqlStatement = command.CommandText;

                foreach (var f in EntityRuntime<Entity>.FieldsRuntime)
                {
                    if (!f.Value.Identity)
                    {
                        command.Parameters.Add(DatabaseCommands.GetParameter(f.Value.PhysicalName, f.Value.FieldReader(entity)));
                    }
                }

                if (IdentityFieldRuntime != null)
                {
                    object identity = null;

                    if (DatabaseCommands.SupportsMultipleQueries)
                    {
                        //update identity field in entity.
                        identity = _Connection.ExecuteScalar(command);
                    }
                    else
                    {
                        // run the insert statement.
                        _Connection.ExecuteNonQuery(command);

                        identity = _Connection.ExecuteScalar(DatabaseCommands.GetIdentityCommand());
                    }

                    object converted = Convert.ChangeType(identity, IdentityFieldRuntime.FieldType);

                    // set the identity field of this instance
                    
                    IdentityFieldRuntime.FieldWriter(entity, converted);

                }
                else
                {
                    // do nothing
                    _Connection.ExecuteNonQuery(command);
                }
            }
        }



        private string _LastSqlStatement;

        /// <summary>
        /// Display last sql statement executed in the controller.
        /// </summary>
        public string LastSqlStatement
        {
            get
            {
                return _LastSqlStatement;
            }
        }

        /// <summary>
        /// Insert bulk of entities {optimitzed for fast execution)
        /// </summary>
        /// <param name="entities"></param>
        public void Insert(IEnumerable<Entity> entities)
        {
            ExecutePreOperations();

            EntityFieldRuntime IdentityFieldRuntime;

            
            using (DbCommand command = DatabaseCommands.GetInsertCommand(out IdentityFieldRuntime))
            {
                _LastSqlStatement = command.CommandText;

                int ie = 0;
                foreach (var entity in entities)
                {
                    foreach (var f in EntityRuntime<Entity>.FieldsRuntime)
                    {
                        if (!f.Value.Identity)
                        {
                            if (ie == 0)
                                command.Parameters.Add(DatabaseCommands.GetParameter(f.Value.PhysicalName, f.Value.FieldReader(entity)));
                            else
                            {
                                // already declared
                                command.Parameters[DatabaseCommands.GetValidParameterName(f.Value.PhysicalName)].Value = f.Value.FieldReader(entity);
                            }
                        }
                    }
                    
                    _Connection.ExecuteNonQuery(command);

                    ie++;
                }
            }
        }


        /// <summary>
        /// Select the entity based on its primary ids
        /// </summary>
        /// <param name="entity"></param>
        public void Select(ref Entity entity)
        {
            ExecutePreOperations();

            using (DbCommand command = DatabaseCommands.GetSelectCommand())
            {
                foreach (var f in EntityRuntime<Entity>.FieldsRuntime)
                {
                    if (f.Value.Primary)
                    {
                        command.Parameters.Add(DatabaseCommands.GetParameter(f.Value.PhysicalName, f.Value.FieldReader(entity)));
                    }
                }

                _LastSqlStatement = command.CommandText;
                using (var reader = _Connection.ExecuteReader(command))
                {
                    reader.Read();

                    entity = EntityRuntime<Entity>.MappingFunction(reader);

                    reader.Close();
                }
            }

            _Connection.CloseConnection();
        }


        /// <summary>
        /// Delete the entity based on data
        /// </summary>
        /// <param name="entity"></param>
        public void Delete(Entity entity)
        {
            ExecutePreOperations();

            using (DbCommand command = DatabaseCommands.GetDeleteCommand())
            {
                foreach (var f in EntityRuntime<Entity>.FieldsRuntime)
                {
                    if (f.Value.Primary)
                    {
                        command.Parameters.Add(DatabaseCommands.GetParameter(f.Value.PhysicalName, f.Value.FieldReader(entity)));
                    }
                }
                _LastSqlStatement = command.CommandText;
                _Connection.ExecuteNonQuery(command);
            }

            _Connection.CloseConnection();
            
        }



        public void Update(Entity entity)
        {
            ExecutePreOperations();

            using (DbCommand command = DatabaseCommands.GetUpdateCommand())
            {
                
                foreach (var f in EntityRuntime<Entity>.FieldsRuntime)
                {
                    command.Parameters.Add(DatabaseCommands.GetParameter(f.Value.PhysicalName, f.Value.FieldReader(entity)));
                }

                _LastSqlStatement = command.CommandText;
                _Connection.ExecuteNonQuery(command);
            }
            _Connection.CloseConnection();
        }


        /// <summary>
        /// Execute and sql statement against the database provider.
        /// </summary>
        /// <param name="sql"></param>
        public int Execute(string sql)
        {
            ExecutePreOperations();

            var cmd = _Connection.GetExecuteCommand(sql);

            return _Connection.ExecuteNonQuery(cmd);
        }


        /// <summary>
        /// Execute procedure that returns collection of the entity.
        /// </summary>
        /// <param name="procName"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public ICollection<Entity> ExecuteProcedure(string procName)
        {

            ExecutePreOperations();

            List<Entity> ets = new List<Entity>();

            using (var command = DatabaseCommands.GetStoredProcedureCommand(procName))
            {
                using (var reader = _Connection.ExecuteReader(command))
                {
                    while (reader.Read())
                    {
                        ets.Add(EntityRuntime<Entity>.MappingFunction(reader));
                    }
                    reader.Close();
                }
            }

            _Connection.CloseConnection();

            return ets;
        }




        #region IDisposable Members

        public void Dispose()
        {
            this._Connection.Dispose();
        }

        #endregion



        /// <summary>
        /// The name as it appears in inner sql statements
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public string FieldInnerName(string fieldName)
        {
            string fld = DatabaseCommands.EntityRuntimeInformation.RunningPhysicalName + EntityRuntime<Entity>.AliasSeparator + fieldName;

            return fld;
        }

        public void DropTable()
        {
            ExecutePreOperations();
            using (DbCommand command = DatabaseCommands.GetDropTableCommand())
            {
                _LastSqlStatement = command.CommandText;
                _Connection.ExecuteNonQuery(command);
            }
            _Connection.CloseConnection();            
        }
    }
}
