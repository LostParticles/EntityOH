using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;

using System.Data.OleDb;


namespace EntityOH.Controllers
{
    public class OleDbCommandsMaker<Entity> : DbCommandsMaker<Entity>
    {
        public OleDbCommandsMaker(EntityRuntime<Entity> entityRuntimeInformation)
            : base(entityRuntimeInformation)
        {
        }

        
        public override bool SupportsMultipleQueries
        {
            get { throw new NotImplementedException(); }
        }

        public override DbCommand GetIdentityCommand()
        {
            throw new NotImplementedException();
        }

        public override DbCommand GetInsertCommand(out EntityFieldRuntime identityFieldRuntime)
        {
            throw new NotImplementedException();
        }

        public override DbParameter GetParameter(string parameterName, object value)
        {
            throw new NotImplementedException();
        }

        public override string GetValidParameterName(string parameterName)
        {
            throw new NotImplementedException();
        }

        public override DbCommand GetSelectCommand()
        {
            throw new NotImplementedException();
        }

        public override DbCommand GetCountCommand()
        {
            throw new NotImplementedException();
        }

        public override DbCommand GetCountCommand(string whereClause)
        {
            throw new NotImplementedException();
        }

        public override DbCommand GetAggregateFunctionCommand(string aggregateFunction, string field)
        {
            throw new NotImplementedException();
        }

        public override DbCommand GetAggregateFunctionCommand(string whereClause, string aggregateFunction, string field)
        {
            throw new NotImplementedException();
        }

        public override DbCommand GetDeleteCommand()
        {
            throw new NotImplementedException();
        }

        public override DbCommand GetUpdateCommand()
        {
            throw new NotImplementedException();
        }

        public override DbCommand GetStoredProcedureCommand(string procName)
        {
            throw new NotImplementedException();
        }

        public override DbCommand GetCreateTableCommand()
        {
            throw new NotImplementedException();
        }

        public override string DbTypeFromCLRType(Type clrType)
        {
            throw new NotImplementedException();
        }


        public override DbConnection GetNewConnection(string connectionString)
        {
            return new OleDbConnection(connectionString);
        }

        public override DbCommand GetNewCommand()
        {
            return new OleDbCommand();
        }

        public override DbCommandsMaker<AnyEntity> GetThisMaker<AnyEntity>()
        {
            return new OleDbCommandsMaker<AnyEntity>(new EntityRuntime<AnyEntity>());
        }

       
    }
}
