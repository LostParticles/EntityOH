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

        /// <summary>
        /// Get all the records of entity in database.
        /// </summary>
        /// <returns></returns>
        public ICollection<Entity> Select()
        {
            string SelectAllStatement = "SELECT {0} FROM {1}";

            string SelectAll = string.Format(SelectAllStatement, FieldsList, FromExpression);

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
        /// Select based on where condition.
        /// </summary>
        /// <param name="whereClause"></param>
        /// <returns></returns>
        public ICollection<Entity> Select(string whereClause)
        {
            string SelectAllStatement = "SELECT {0} FROM {1} WHERE {2}";


            string SelectAll = string.Format(SelectAllStatement, FieldsList, FromExpression, whereClause);

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
        /// Select certain fields and return the objects with only those fields.
        /// </summary>
        /// <param name="fieldsList"></param>
        /// <returns></returns>
        public ICollection<Entity> SelectPartially(params string[] fields)
        {
            string SelectAllStatement = "SELECT {0} FROM {1}";

            string fieldsList=string.Empty;
            foreach (string fld in fields) fieldsList += fld + ",";
            fieldsList = fieldsList.TrimEnd(',');
             
            string SelectAll = string.Format(SelectAllStatement, fieldsList, FromExpression);

            List<Entity> ets = new List<Entity>();

            using (var reader = _Connection.ExecuteReader(SelectAll))
            {
                SmartReader sr = new SmartReader(reader);
                while (sr.Read())
                {

                    ets.Add(EntityRuntime<Entity>.PartialMappingFunction(sr));
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
            long result = 0;
            using (var CountCommand = _Connection.GetCountCommand<Entity>())
            {
                 result = long.Parse(_Connection.ExecuteScalar(CountCommand).ToString());
            }
            return result;
        }

        /// <summary>
        /// Get the sum of specific field.
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        public double Sum(string field)
        {
            double result = 0;
            using (var SumCommand = _Connection.GetAggregateFunctionCommand<Entity>("SUM", field))
            {
                result = double.Parse( _Connection.ExecuteScalar(SumCommand).ToString());
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
        /// Select the entity based on its primary ids
        /// Needs re-written to not create the whole object again.
        /// </summary>
        /// <param name="entity"></param>
        public void SelectEntity(ref Entity entity)
        {
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
        /// Execute procedure that returns collection of the entity.
        /// </summary>
        /// <param name="procName"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public ICollection<Entity> ExecuteProcedure(string procName)
        {
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
