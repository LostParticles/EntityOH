using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;

namespace EntityOH.Controllers
{
    /// <summary>
    /// Base class for all DbCommandsMakers  
    /// you should inherit from this class in every database type you want to access.
    /// </summary>
    /// <typeparam name="Entity"></typeparam>
    public abstract class DbCommandsMaker<Entity>
    {
        readonly protected EntityRuntime<Entity> _EntityRuntimeInformation = new EntityRuntime<Entity>();

        public EntityRuntime<Entity> EntityRuntimeInformation
        {
            get
            {
                return _EntityRuntimeInformation;
            }
        }

        public DbCommandsMaker()
        {
        }

        public DbCommandsMaker(EntityRuntime<Entity> entityRuntimeInformation)
        {
            _EntityRuntimeInformation = entityRuntimeInformation;
        }


        abstract public DbCommand GetIdentityCommand();

        abstract public DbCommand GetInsertCommand(out EntityFieldRuntime identityFieldRuntime);
        abstract public DbParameter GetParameter(string parameterName, object value);
        abstract public string GetValidParameterName(string parameterName);
        abstract public DbCommand GetSelectCommand();

        abstract public DbCommand GetCountCommand();
        abstract public DbCommand GetCountCommand(string whereClause);
        abstract public DbCommand GetAggregateFunctionCommand(string aggregateFunction, string field);
        abstract public DbCommand GetAggregateFunctionCommand(string whereClause, string aggregateFunction, string field);
        abstract public DbCommand GetDeleteCommand();
        abstract public DbCommand GetUpdateCommand();
        abstract public DbCommand GetStoredProcedureCommand(string procName);
        abstract public DbCommand GetCreateTableCommand();

        abstract public string DbTypeFromCLRType(Type clrType);


        /// <summary>
        /// Indicates that the database can execute multiple queries in the same time in one sql statement.
        /// </summary>
        abstract public bool SupportsMultipleQueries { get; }

        public abstract DbConnection GetNewConnection(string connectionString);

        public abstract DbCommand GetNewCommand();

        public abstract DbCommandsMaker<AnyEntity> GetThisMaker<AnyEntity>();
    }
}
