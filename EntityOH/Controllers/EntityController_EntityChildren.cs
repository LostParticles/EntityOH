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
    public partial class EntityController<Entity> : IDisposable
    {

        /// <summary>
        /// Select the children of the child entity whose parent is the entity
        /// </summary>
        /// <typeparam name="ChildEntity"></typeparam>
        /// <param name="parentEntity"></param>
        /// <returns></returns>
        public ICollection<ChildEntity> SelectChildren<ChildEntity>(Entity parentEntity)
        {
            ExecutePreOperations();

            string SelectAllStatement = "SELECT {0} FROM {1} WHERE {2}";

            string FieldsList=string.Empty;
            foreach (var fld in EntityRuntimeHelper.FieldsList(typeof(ChildEntity)))
                FieldsList += fld + ",";

            FieldsList = FieldsList.TrimEnd(',');


            string ForiegnWhereStatement = string.Empty;

            // loop through child fields to know the field that will be used in criteria
            foreach (var fld in EntityRuntime<ChildEntity>.FieldsRuntime)
            {
                if (fld.Value.ForiegnReferenceType == typeof(Entity))
                {
                    // found required key.
                    ForiegnWhereStatement += EntityRuntimeHelper.EntityPhysicalName(typeof(ChildEntity)) + "." + fld.Value.PhysicalName + " = " + EntityRuntime<Entity>.FieldsRuntime[fld.Value.ForiegnReferenceField].FieldReader(parentEntity).ToString();
                    break;
                }
            }

            if (string.IsNullOrEmpty(ForiegnWhereStatement)) throw new Exception("The child entity has no foriegn key for this table");

            // store the child runtime here for 


            string SelectAll = string.Format(SelectAllStatement, FieldsList, EntityRuntimeHelper.FromClause(typeof(ChildEntity)), ForiegnWhereStatement);

            List<ChildEntity> ets = new List<ChildEntity>();

            using (var reader = _Connection.ExecuteReader(SelectAll))
            {

                while (reader.Read())
                {
                    ets.Add(EntityRuntime<ChildEntity>.MappingFunction(reader));
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
            ExecutePreOperations();

            string SelectAllStatement = "SELECT {0} FROM {1}";

            string fieldsList = string.Empty;
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
       


    }
}
