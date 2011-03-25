using System;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using EntityOH.Attributes;
using EntityOH.Controllers.Connections;
using System.Data.SqlClient;
using System.Collections.Generic;


namespace EntityOH.Controllers
{

    public partial class EntityController<Entity> : IDisposable
    {

        #region Commands Prepators :) looool

        /// <summary>
        /// Make insert command of the entity.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="identityExist"></param>
        /// <returns></returns>
        internal DbCommand GetInsertCommand(out EntityFieldRuntime identityFieldRuntime)
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

            string InsertStatement = string.Format(InsertStatementTemplate, EntityRuntime.RunningPhysicalName, FinalFields, FinalParameters);

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

        internal DbCommand GetSelectCommand()
        {
            string SelectTemplate = "SELECT * FROM " + EntityRuntime.RunningPhysicalName + " WHERE {0}";

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


        internal DbCommand GetCountCommand()
        {
            string SelectTemplate = "SELECT COUNT(*) FROM " + EntityRuntime<Entity>.FromClause;

            var finalSelect = string.Format(SelectTemplate);

            return new SqlCommand(finalSelect);
        }

        internal DbCommand GetCountCommand(string whereClause)
        {
            string SelectTemplate = "SELECT COUNT(*) FROM " + EntityRuntime<Entity>.FromClause;
            SelectTemplate += " WHERE " + whereClause;


            var finalSelect = string.Format(SelectTemplate);

            return new SqlCommand(finalSelect);
        }



        internal DbCommand GetAggregateFunctionCommand(string aggregateFunction, string field)
        {
            string SelectTemplate = "SELECT {0}({1}) FROM " + EntityRuntime<Entity>.FromClause;

            var finalSelect = string.Format(SelectTemplate, aggregateFunction, field);

            return new SqlCommand(finalSelect);
        }

        internal DbCommand GetAggregateFunctionCommand(string whereClause, string aggregateFunction, string field)
        {
            string SelectTemplate = "SELECT {0}({1}) FROM " + EntityRuntime<Entity>.FromClause;
            SelectTemplate += " WHERE " + whereClause;

            var finalSelect = string.Format(SelectTemplate, aggregateFunction, field);

            return new SqlCommand(finalSelect);
        }

        internal DbCommand GetDeleteCommand()
        {
            string DeleteTemplate = "DELETE " + EntityRuntime.RunningPhysicalName + " WHERE {0}";

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


        internal DbCommand GetUpdateCommand()
        {
            string UpdateTemplate = "UPDATE " + EntityRuntime.RunningPhysicalName + " SET {0} WHERE {1}";

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


        internal DbCommand GetStoredProcedureCommand(string procName)
        {
            var sq = new SqlCommand(procName);
            sq.CommandType = CommandType.StoredProcedure;

            return sq;
        }


        internal DbCommand GetCreateTableCommand()
        {

            string tblPhysicalName = EntityRuntime.RunningPhysicalName;

            string cTable = "CREATE TABLE " + tblPhysicalName + " (\n{0}\n);";

            StringBuilder flds = new StringBuilder();


            foreach (var f in EntityRuntime<Entity>.EntityRuntimeFields)
            {

                flds.Append(f.PhysicalName);

                flds.Append(" ");
                flds.Append(SqlTypeFromCLRType(f.FieldType));

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



        #endregion

    }
}