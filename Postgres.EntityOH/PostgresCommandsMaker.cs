using EntityOH.Attributes;
using EntityOH.DbCommandsMakers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using EntityOH.Controllers;
using Npgsql;
using System.Data;

namespace Postgres.EntityOH
{
    [CommandsMaker("Npgsql")]
    public class PostgresCommandsMaker<Entity> : DbCommandsMaker<Entity>
    {
        static Dictionary<Type, string> DbTypeFromCLR = new Dictionary<Type, string>();

        static PostgresCommandsMaker()
        {
            DbTypeFromCLR.Add(typeof(long), "int8");
            DbTypeFromCLR.Add(typeof(bool), "bool");
            DbTypeFromCLR.Add(typeof(byte[]), "bytea");
            DbTypeFromCLR.Add(typeof(DateTime), "timestamp");
            DbTypeFromCLR.Add(typeof(double), "float8");
            DbTypeFromCLR.Add(typeof(int), "int4");
            DbTypeFromCLR.Add(typeof(decimal), "numeric"); // or money in postgres
            DbTypeFromCLR.Add(typeof(Single), "float4");
            DbTypeFromCLR.Add(typeof(short), "int2");
            DbTypeFromCLR.Add(typeof(string), "text");
            DbTypeFromCLR.Add(typeof(TimeSpan), "interval");
            DbTypeFromCLR.Add(typeof(System.Net.IPAddress), "inet");
            DbTypeFromCLR.Add(typeof(Guid), "uuid");

        }

        public PostgresCommandsMaker(EntityRuntime<Entity> entityRuntimeInformation)
            : base(entityRuntimeInformation)
        {
        }

        public override DbCommand GetIdentityCommand()
        {
            throw new NotImplementedException();
        }

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

                FinalParameters += ":" + f;
                FinalParameters += ",";

            }

            FinalFields = FinalFields.TrimEnd(',');
            FinalParameters = FinalParameters.TrimEnd(',');

            string InsertStatementTemplate = "INSERT INTO {0} ({1}) VALUES ({2}) RETURNING ";

            // identity field name
            InsertStatementTemplate += identityFieldRuntime.PhysicalName;

            string InsertStatement = string.Format(InsertStatementTemplate, _EntityRuntimeInformation.RunningPhysicalName, FinalFields, FinalParameters);

            DbCommand command = new NpgsqlCommand(InsertStatement);


            return command;
        }

        public override DbParameter GetParameter(string parameterName, object value)
        {
            object val = value;
            if (val == null) val = DBNull.Value;

            NpgsqlParameter sp = new NpgsqlParameter(GetValidParameterName(parameterName), val);
            
            return sp;
        }

        public override string GetValidParameterName(string parameterName)
        {
            return parameterName;
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

            return new NpgsqlCommand(finalSelect);
        }

        public override DbCommand GetCountCommand()
        {
            string SelectTemplate = "SELECT COUNT(*) FROM " + EntityRuntime<Entity>.FromClause;

            var finalSelect = string.Format(SelectTemplate);

            return new NpgsqlCommand(finalSelect);
        }

        public override DbCommand GetCountCommand(string whereClause)
        {
            string SelectTemplate = "SELECT COUNT(*) FROM " + EntityRuntime<Entity>.FromClause;

            if (!string.IsNullOrEmpty(whereClause)) SelectTemplate += " WHERE " + whereClause;

            var finalSelect = string.Format(SelectTemplate);

            return new NpgsqlCommand(finalSelect);
        }

        public override DbCommand GetAggregateFunctionCommand(string aggregateFunction, string field)
        {
            string SelectTemplate = "SELECT {0}({1}) FROM " + EntityRuntime<Entity>.FromClause;

            var finalSelect = string.Format(SelectTemplate, aggregateFunction, field);

            return new NpgsqlCommand(finalSelect);
        }

        public override DbCommand GetAggregateFunctionCommand(string whereClause, string aggregateFunction, string field)
        {
            string SelectTemplate = "SELECT {0}({1}) FROM " + EntityRuntime<Entity>.FromClause;
            SelectTemplate += " WHERE " + whereClause;

            var finalSelect = string.Format(SelectTemplate, aggregateFunction, field);

            return new NpgsqlCommand(finalSelect);
        }

        public override DbCommand GetDeleteCommand()
        {
            string DeleteTemplate = "DELETE FROM " + _EntityRuntimeInformation.RunningPhysicalName + " WHERE {0}";


            string Conditions = string.Empty;

            foreach (var fr in EntityRuntime<Entity>.FieldsRuntime)
            {
                if (fr.Value.Primary)
                {
                    Conditions += fr.Value.PhysicalName + " = :" + fr.Value.PhysicalName + " AND ";
                }
            }

            if (string.IsNullOrEmpty(Conditions)) throw new NotImplementedException("Selecting entity without primary field is not implemented\nPlease consider adding decorating your entity fields with one or more primary ids.");

            Conditions = Conditions.Substring(0, Conditions.Length - 5);

            var finalDelete = string.Format(DeleteTemplate, Conditions);

            return new NpgsqlCommand(finalDelete);
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
                    Conditions += fr.Value.PhysicalName + " = :" + fr.Value.PhysicalName + " AND ";
                }
                else
                {
                    // normal field
                    if (!fr.Value.Identity)
                    {
                        updatelist += fr.Value.PhysicalName + " = :" + fr.Value.PhysicalName + ",";
                    }
                }
            }

            if (string.IsNullOrEmpty(Conditions)) throw new NotImplementedException("Selecting entity without primary field is not implemented\nPlease consider adding decorating your entity fields with one or more primary ids.");

            Conditions = Conditions.Substring(0, Conditions.Length - 5);
            updatelist = updatelist.TrimEnd(',');

            var UpdateSelect = string.Format(UpdateTemplate, updatelist, Conditions);

            return new NpgsqlCommand(UpdateSelect);
        }

        public override DbCommand GetStoredProcedureCommand(string procName)
        {
            var sq = new NpgsqlCommand(procName);
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
                

                if (f.Identity) 
                    flds.Append("  SERIAL");
                else 
                    flds.Append(DbTypeFromCLRType(f.FieldType));

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
                    flds.Append("CONSTRAINT PK_" + tblPhysicalName + " PRIMARY KEY (" + f.PhysicalName + ")");
                    flds.Append(',');
                }
            }

            string CreateTable = string.Format(cTable, flds.ToString().TrimEnd(','));


            return new NpgsqlCommand(CreateTable);
        }

        public override DbCommand GetDropTableCommand()
        {
            var ss = "DROP TABLE " + _EntityRuntimeInformation.RunningPhysicalName;

            return new NpgsqlCommand(ss);
        }

        public override string DbTypeFromCLRType(Type clrType)
        {
            Type type = default(Type);

            if (clrType.IsGenericType && clrType.IsValueType)
                type = Nullable.GetUnderlyingType(clrType);  //because it is nullable type.
            else
                type = clrType;

            if (type.IsArray)
            {
                throw new NotImplementedException("Arrays of CLR types into postgres not implemented yet.");
            }
            
            string dbType;
            if (DbTypeFromCLR.TryGetValue(type, out dbType))
            {
                return dbType;
            }
            
            throw new NotImplementedException(type.ToString() + " Type doesn't have corresponding sql type");
        }

        public override bool CanReturnIdentityAfterInsert
        {
            get { return true; }
        }

        public override DbConnection GetNewConnection(string connectionString)
        {
            return new NpgsqlConnection(connectionString);
        }

        public override DbCommand GetNewCommand()
        {
            return new NpgsqlCommand();
        }

        public override DbCommandsMaker<AnyEntity> GetThisMaker<AnyEntity>()
        {
            return new PostgresCommandsMaker<AnyEntity>(new EntityRuntime<AnyEntity>());
        }


        public override string TablesSchemaSelectStatement
        {
            get
            {
                return "SELECT * FROM INFORMATION_SCHEMA.TABLES where table_schema='public'";
            }
        }
    }
}
