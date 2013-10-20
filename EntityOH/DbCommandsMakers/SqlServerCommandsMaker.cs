using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data.Common;
using System.Data;
using EntityOH.Controllers;

namespace EntityOH.DbCommandsMakers
{
    public class SqlServerCommandsMaker<Entity> : DbCommandsMaker<Entity>
    {


        public SqlServerCommandsMaker(EntityRuntime<Entity> entityRuntimeInformation)
            : base(entityRuntimeInformation)
        {
        }


        public override bool SupportsMultipleQueries
        {
            get { return true; }
        }


        public override DbCommand GetIdentityCommand()
        {
            SqlCommand sc = new SqlCommand("SELECT @@IDENTITY");
            return sc;
        }

        /// <summary>
        /// Make insert command of the entity.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="identityExist"></param>
        /// <returns></returns>
        public override DbCommand GetInsertCommand(out EntityFieldRuntime identityFieldRuntime)
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

            string InsertStatement = string.Format(InsertStatementTemplate, _EntityRuntimeInformation.RunningPhysicalName, FinalFields, FinalParameters);

            if (identityFieldRuntime != null)
            {

                InsertStatement += "; SELECT SCOPE_IDENTITY()";

            }


            DbCommand command = new SqlCommand(InsertStatement);


            return command;
        }

        public override DbParameter GetParameter(string parameterName, object value)
        {
            object val = value;
            if (val == null) val = DBNull.Value;

            SqlParameter sp = new SqlParameter(GetValidParameterName(parameterName), val);
            return sp;
        }

        public override string GetValidParameterName(string parameterName)
        {
            return "@" + parameterName;
        }
        
        public override DbCommand GetSelectCommand()
        {
            string SelectTemplate = "SELECT * FROM " + _EntityRuntimeInformation.RunningPhysicalName + " WHERE {0}";

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


        public override DbCommand GetCountCommand()
        {
            string SelectTemplate = "SELECT COUNT(*) FROM " + EntityRuntime<Entity>.FromClause;

            var finalSelect = string.Format(SelectTemplate);

            return new SqlCommand(finalSelect);
        }

        public override DbCommand GetCountCommand(string whereClause)
        {
            string SelectTemplate = "SELECT COUNT(*) FROM " + EntityRuntime<Entity>.FromClause;

            if (!string.IsNullOrEmpty(whereClause)) SelectTemplate += " WHERE " + whereClause;

            var finalSelect = string.Format(SelectTemplate);

            return new SqlCommand(finalSelect);
        }



        public override DbCommand GetAggregateFunctionCommand(string aggregateFunction, string field)
        {
            string SelectTemplate = "SELECT {0}({1}) FROM " + EntityRuntime<Entity>.FromClause;

            var finalSelect = string.Format(SelectTemplate, aggregateFunction, field);

            return new SqlCommand(finalSelect);
        }

        public override DbCommand GetAggregateFunctionCommand(string whereClause, string aggregateFunction, string field)
        {
            string SelectTemplate = "SELECT {0}({1}) FROM " + EntityRuntime<Entity>.FromClause;
            SelectTemplate += " WHERE " + whereClause;

            var finalSelect = string.Format(SelectTemplate, aggregateFunction, field);

            return new SqlCommand(finalSelect);
        }

        public override DbCommand GetDeleteCommand()
        {
            string DeleteTemplate = "DELETE " + _EntityRuntimeInformation.RunningPhysicalName + " WHERE {0}";
            

            string Conditions = string.Empty;

            foreach (var fr in EntityRuntime<Entity>.FieldsRuntime)
            {
                if (fr.Value.Primary)
                {
                    Conditions += fr.Value.PhysicalName + " = @" + fr.Value.PhysicalName + " AND ";
                }
            }

            if (string.IsNullOrEmpty(Conditions)) throw new NotImplementedException("Selecting entity without primary field is not implemented\nPlease consider adding decorating your entity fields with one or more primary ids.");

            Conditions = Conditions.Substring(0, Conditions.Length - 5);

            var finalDelete = string.Format(DeleteTemplate, Conditions);

            return new SqlCommand(finalDelete);
        }


        public override DbCommand GetUpdateCommand()
        {
            string UpdateTemplate = "UPDATE " + _EntityRuntimeInformation.RunningPhysicalName + " SET {0} WHERE {1}";
            

            string Conditions = string.Empty;
            string updatelist = string.Empty;


            foreach (var fr in EntityRuntime<Entity>.FieldsRuntime)
            {
                if (fr.Value.Primary)
                {
                    Conditions += fr.Value.PhysicalName + " = @" + fr.Value.PhysicalName + " AND ";
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

            Conditions = Conditions.Substring(0, Conditions.Length - 5);
            updatelist = updatelist.TrimEnd(',');

            var UpdateSelect = string.Format(UpdateTemplate, updatelist, Conditions);

            return new SqlCommand(UpdateSelect);


        }


        public override DbCommand GetStoredProcedureCommand(string procName)
        {
            var sq = new SqlCommand(procName);
            sq.CommandType = CommandType.StoredProcedure;

            return sq;
        }


        public override DbCommand GetCreateTableCommand()
        {

            string tblPhysicalName = _EntityRuntimeInformation.RunningPhysicalName;
            

            string cTable = "CREATE TABLE " + tblPhysicalName + " (\n{0}\n);";

            StringBuilder flds = new StringBuilder();


            foreach (var f in EntityRuntime<Entity>.EntityRuntimeFields)
            {

                flds.Append(f.PhysicalName);

                flds.Append(" ");
                flds.Append(DbTypeFromCLRType(f.FieldType));

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

            foreach (var f in EntityRuntime<Entity>.EntityRuntimeFields)
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

        public override DbCommand GetDropTableCommand()
        {
            var ss = "DROP TABLE " + _EntityRuntimeInformation.RunningPhysicalName;

            return new SqlCommand(ss);
        }

        public override string DbTypeFromCLRType(Type clrType)
        {
            Type type = default(Type);

            if (clrType.IsGenericType && clrType.IsValueType)
                type = Nullable.GetUnderlyingType(clrType);  //because it is nullable type.
            else
                type = clrType;


            if (type == typeof(byte)) return "TinyInt";
            if (type == typeof(short)) return "SmallInt";
            if (type == typeof(int)) return "Int";
            if (type == typeof(long)) return "BigInt";

            if (type == typeof(DateTime)) return "DateTime";

            if (type == typeof(string)) return "NVarChar(MAX)";

            if (type == typeof(double)) return "Float";
            if (type == typeof(Single)) return "Real";
            if (type == typeof(Guid)) return "UniqueIdentifier";

            throw new NotImplementedException(type.ToString() + " Type doesn't have corresponding sql type");
        }


        public override DbConnection GetNewConnection(string connectionString)
        {
            return new SqlConnection(connectionString);
        }

        public override DbCommand GetNewCommand()
        {
            return new SqlCommand();
        }

        public override DbCommandsMaker<AnyEntity> GetThisMaker<AnyEntity>()
        {
            return new SqlServerCommandsMaker<AnyEntity>(new EntityRuntime<AnyEntity>());
        }

    }
}
