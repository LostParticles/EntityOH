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


namespace EntityOH.Controllers
{

    /// <summary>
    /// The entity controller that is responsible in getting entities from database.
    /// </summary>
    /// <typeparam name="Entity"></typeparam>
    public partial class EntityController<Entity> : IDisposable
    {

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

        public EntityController()
        {
            _Connection = SmartConnection.GetSmartConnection();
        }

        public EntityController(string connectionKey)
        {
            _Connection = SmartConnection.GetSmartConnection(connectionKey);
        }


        private static string _FieldsList = string.Empty;
        public static string FieldsList
        {
            get
            {
                if (string.IsNullOrEmpty(_FieldsList))
                {
                    StringBuilder sb = new StringBuilder();
                    var fls = EntityRuntimeHelper.FieldsList(EntityType);

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
        public static string FromExpression
        {
            get
            {
                return EntityRuntimeHelper.FromClause(EntityType);
            }
        }

        public static string GroupByExpression
        {
            get
            {
                return EntityRuntimeHelper.GroupByExpression(EntityType);
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

            List<Entity> ets = new List<Entity>();

            using (var reader = _Connection.ExecuteReader(SelectAll))
            {
                while (reader.Read())
                {
                    ets.Add(EntityRuntime<Entity>.MappingFunction(reader));
                }
                reader.Close();
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

            List<Entity> ets = new List<Entity>();

            using (var reader = _Connection.ExecuteReader(SelectAll))
            {
                while (reader.Read())
                {
                    ets.Add(EntityRuntime<Entity>.MappingFunction(reader));
                }
                reader.Close();
            }

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


            string pid = EntityRuntime<Entity>.PhysicalName + "." +
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

            totalDiscoveredCount = (int)_Connection.ExecuteScalar(_Connection.GetCountCommand<Entity>());

            List<Entity> ets = new List<Entity>();

            using (var reader = _Connection.ExecuteReader(FinalSelect))
            {
                while (reader.Read())
                {
                    ets.Add(EntityRuntime<Entity>.MappingFunction(reader));
                }
                reader.Close();
            }

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

            if (!string.IsNullOrEmpty(whereClause))
                SelectAll += " WHERE " + whereClause;


            List<Entity> ets = new List<Entity>();

            using (var reader = _Connection.ExecuteReader(SelectAll))
            {

                while (reader.Read())
                {
                    ets.Add(EntityRuntime<Entity>.MappingFunction(reader));
                }

                reader.Close();
            }

            return ets;
        }


        public ICollection<Entity> SelectPaged(string whereClause, int pageIndex, int pageItemsCount, out int totalDiscoveredCount)
        {

            ExecutePreOperations();


            string pid = EntityRuntime<Entity>.PhysicalName + "." +
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

            totalDiscoveredCount = (int)_Connection.ExecuteScalar(_Connection.GetCountCommand<Entity>(whereClause));

            List<Entity> ets = new List<Entity>();

            using (var reader = _Connection.ExecuteReader(FinalSelect))
            {
                while (reader.Read())
                {
                    ets.Add(EntityRuntime<Entity>.MappingFunction(reader));
                }
                reader.Close();
            }

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
            using (var CountCommand = _Connection.GetCountCommand<Entity>())
            {
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
            using (var SumCommand = _Connection.GetAggregateFunctionCommand<Entity>("SUM", field))
            {
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
            using (var SumCommand = _Connection.GetAggregateFunctionCommand<Entity>(where, "SUM", field))
            {
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

            using (DbCommand command = _Connection.GetInsertCommand<Entity>(out IdentityFieldRuntime))
            {
                foreach (var f in EntityRuntime<Entity>.FieldsRuntime)
                {
                    if (!f.Value.Identity)
                    {
                        command.Parameters.Add(_Connection.GetParameter(f.Value.PhysicalName, f.Value.FieldReader(entity)));
                    }
                }

                if (IdentityFieldRuntime != null)
                {
                    //update identity field in entity.
                    object identity = _Connection.ExecuteScalar(command);

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



        /// <summary>
        /// Insert bulk of entities {optimitzed for fast execution)
        /// </summary>
        /// <param name="entities"></param>
        public void Insert(IEnumerable<Entity> entities)
        {
            ExecutePreOperations();

            EntityFieldRuntime IdentityFieldRuntime;

            using (DbCommand command = _Connection.GetInsertCommand<Entity>(out IdentityFieldRuntime))
            {
                int ie = 0;
                foreach (var entity in entities)
                {
                    foreach (var f in EntityRuntime<Entity>.FieldsRuntime)
                    {
                        if (!f.Value.Identity)
                        {
                            if (ie == 0)
                                command.Parameters.Add(_Connection.GetParameter(f.Value.PhysicalName, f.Value.FieldReader(entity)));
                            else
                            {
                                // already declared
                                command.Parameters[_Connection.GetValidParameterName(f.Value.PhysicalName)].Value = f.Value.FieldReader(entity);
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
        /// Needs re-written to not create the whole object again.
        /// </summary>
        /// <param name="entity"></param>
        public void SelectEntity(ref Entity entity)
        {
            ExecutePreOperations();

            using (DbCommand command = _Connection.GetSelectCommand<Entity>())
            {
                foreach (var f in EntityRuntime<Entity>.FieldsRuntime)
                {
                    if (f.Value.Primary)
                    {
                        command.Parameters.Add(_Connection.GetParameter(f.Value.PhysicalName, f.Value.FieldReader(entity)));
                    }
                }

                using (var reader = _Connection.ExecuteReader(command))
                {
                    reader.Read();

                    entity = EntityRuntime<Entity>.MappingFunction(reader);

                    reader.Close();
                }
            }
        }


        /// <summary>
        /// Delete the entity based on data
        /// </summary>
        /// <param name="entity"></param>
        public void DeleteEntity(Entity entity)
        {
            ExecutePreOperations();

            using (DbCommand command = _Connection.GetDeleteCommand<Entity>())
            {
                foreach (var f in EntityRuntime<Entity>.FieldsRuntime)
                {
                    if (f.Value.Primary)
                    {
                        command.Parameters.Add(_Connection.GetParameter(f.Value.PhysicalName, f.Value.FieldReader(entity)));
                    }
                }

                _Connection.ExecuteNonQuery(command);
            }
            
        }



        public void Update(Entity entity)
        {
            ExecutePreOperations();

            using (DbCommand command = _Connection.GetUpdateCommand<Entity>())
            {

                foreach (var f in EntityRuntime<Entity>.FieldsRuntime)
                {
                    command.Parameters.Add(_Connection.GetParameter(f.Value.PhysicalName, f.Value.FieldReader(entity)));
                }

                _Connection.ExecuteNonQuery(command);
            }
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

            using (var command = _Connection.GetStoredProcedureCommand<Entity>(procName))
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

            return ets;
        }


        #region IDisposable Members

        public void Dispose()
        {
            this._Connection.Dispose();
        }

        #endregion
    }
}
