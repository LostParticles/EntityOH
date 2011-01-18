using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using EntityOH.Attributes;

namespace EntityOH.Controllers
{
    internal static class EntityRuntimeHelper
    {

        internal const string AliasSeparator = "__";


        /// <summary>
        /// Gets the entity suitable properties that can be filled from reading in database.
        /// </summary>
        /// <param name="entityType"></param>
        /// <returns></returns>
        internal static IEnumerable<PropertyInfo> EntityProperties(Type entityType)
        {
            
            // loop through the entity public properties that is able to be written
            var PublicWritablProperties = from pp in entityType.GetProperties()
                                          where pp.CanWrite == true
                                          select pp;

            List<PropertyInfo> FilteredProperties = new List<PropertyInfo>();

            foreach (var pp in PublicWritablProperties)
            {
                // discover the runtime of field whether it is value type or string.
                if (pp.PropertyType.IsValueType || pp.PropertyType == typeof(string) || pp.PropertyType == typeof(byte[]))
                {
                    FilteredProperties.Add(pp);
                }
                else
                {
                    //check if it have custom attributes on its type that tells us that it is an entity from database
                    // we are searching for forieng attribute.
                    var z = pp.PropertyType.GetCustomAttributes(typeof(EntityAttribute), false);
                    if (z.Length > 0)
                    {
                        // found that the entity is really has a corresponding representation in database.

                        FilteredProperties.Add(pp);

                        // note that physical name of this property will be used as the foriegn key in row
                        // and the primary key in reference table will be used as primary key 
                    }
                }
            }

            return FilteredProperties;
        }


        private static Dictionary<Type, string> EntitiesPhysicalName = new Dictionary<Type, string>();
        internal static string EntityPhysicalName(Type entityType)
        {
            string ph;

            if (!EntitiesPhysicalName.TryGetValue(entityType, out ph))
            {
                var vv = entityType.GetCustomAttributes(typeof(EntityAttribute), false).FirstOrDefault() as EntityAttribute;

                if (vv != null)
                {
                    
                    if (string.IsNullOrEmpty(vv.PhysicalName)) ph = entityType.Name;
                    else ph = vv.PhysicalName;

                    EntitiesPhysicalName.Add(entityType, ph);
                }
                else
                {
                    // no attribute found
                    // use the name of the class as the name of the table.

                    ph = entityType.Name;
                    EntitiesPhysicalName.Add(entityType, ph);

                    //throw (new Exception(ph));
                }
            }

            return ph;
        }


        /// <summary>
        /// Discover if the entity is derived from another database entity.
        /// </summary>
        /// <param name="entityType"></param>
        /// <returns></returns>
        internal static bool IsEntityInherited(Type entityType, out Type baseEntity)
        {
            // Discover if the entity is inherited from another entity.
            Type BaseType = entityType.BaseType;
            var BaseAttribute = (EntityAttribute)BaseType.GetCustomAttributes(typeof(EntityAttribute), false).FirstOrDefault();
            if (BaseAttribute != null)
            {
                baseEntity = BaseType;
                return true;
            }

            baseEntity = null;
            return false;
        }

        /// <summary>
        /// every called type will be cached here.
        /// </summary>
        private static Dictionary<Type, List<EntityFieldRuntime>> EntitiesFields = new Dictionary<Type, List<EntityFieldRuntime>>();

        internal static IEnumerable<EntityFieldRuntime> EntityRuntimeFields(Type entityType)
        {
            List<EntityFieldRuntime> efs;
            if (!EntitiesFields.TryGetValue(entityType, out efs))
            {
                efs = new List<EntityFieldRuntime>();

                foreach (var pp in EntityRuntimeHelper.EntityProperties(entityType))
                {
                    efs.Add(new EntityFieldRuntime(pp));
                }

                EntitiesFields.Add(entityType, efs);
            }

            return efs;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="entityType"></param>
        /// <param name="prefix">when true, add the entity name before the property name in the AS [] Clause</param>
        /// <returns></returns>
        internal static IEnumerable<string> FieldsList(Type entityType)
        {
            List<string> _FieldsList = new List<string>();
            string TableName = EntityRuntimeHelper.EntityPhysicalName(entityType);
            Type bt;

            bool inherited = IsEntityInherited(entityType, out bt);
            string BaseTableName = string.Empty;
            if (inherited) BaseTableName = EntityRuntimeHelper.EntityPhysicalName(bt);


            foreach (var fld in EntityRuntimeFields(entityType))
            {
                if (fld.CalculatedExpression)
                {
                    // ignore the table name in case of calculated expressoins
                    // Format:  TableName.FieldPhysicalNameExpression AS [EntityName__PropertyName]
                    //   I have changed separator between [] from dot '.' into colon ':' because ole db driver didn't like dot inside alias name
                    //   then changed again into '_' because after testing with my sql it made an error
                    //    also omitted '[' ']' from begining and ending of alias

                    _FieldsList.Add(fld.PhysicalName + " AS " + entityType.Name + AliasSeparator + fld.FieldPropertyInfo.Name);
                }
                else
                {
                    if(inherited)
                    {
                        if (fld.FieldPropertyInfo.DeclaringType == bt)
                        {
                            _FieldsList.Add(BaseTableName + "." + fld.PhysicalName + " AS " + entityType.Name + AliasSeparator + fld.FieldPropertyInfo.Name);
                            goto gg;
                        }
                    }

                    // Format:  TableName.FieldPhysicalName AS EntityName:PropertyName
                    _FieldsList.Add(TableName + "." + fld.PhysicalName + " AS " + entityType.Name + AliasSeparator + fld.FieldPropertyInfo.Name );
                    
                }
            gg:
                if (fld.Foriegn)
                {
                    // add all other fields of this entity in the list also.

                    // then select it as ReferenceTable.ReferenceField.

                    var ForiegnFields = EntityRuntimeHelper.FieldsList(fld.FieldType);

                    foreach (var ffield in ForiegnFields)
                    {
                        _FieldsList.Add(ffield);
                    }
                }
            }

            return _FieldsList;

        }


        /// <summary>
        /// Predict the required from clause specially if the type has foriegn objects to select keys from
        /// </summary>
        /// <param name="entityType"></param>
        /// <returns></returns>
        internal static string FromClause(Type entityType)
        {
            var ofrom = EntityPhysicalName(entityType);

            EntityFieldRuntime pkey = null;

            foreach (var f in EntityRuntimeFields(entityType))
            {
                if (f.Primary) pkey = f;
                if (f.Foriegn)
                {
                    ofrom += " LEFT JOIN " + EntityPhysicalName(f.ForiegnReferenceType);
                    ofrom += " ON ";
                    ofrom += EntityPhysicalName(entityType) + "." + f.PhysicalName;
                    ofrom += " = ";
                    ofrom += EntityPhysicalName(f.ForiegnReferenceType) + "." + f.ForiegnReferenceField;
                }
            }

            if (pkey != null)
            {
                Type bt;
                if (IsEntityInherited(entityType, out bt))
                {
                    string bname = EntityPhysicalName(bt); //how to know the field name that is one to one with the parent table.

                    // assuming the same name.

                    ofrom += " INNER JOIN " + bname + " ON " + EntityPhysicalName(entityType) + "." + 
                        pkey.PhysicalName + " = " + EntityPhysicalName(bt) + "." + pkey.PhysicalName;
                }
            }
            return ofrom;

        }




        internal static string SqlTypeFromCLRType(Type type, bool treatDateTimeAsDate = false)
        {
            if (type == typeof(byte)) return "TinyInt";
            if (type == typeof(short)) return "SmallInt";
            if (type == typeof(int)) return "Int";
            if (type == typeof(long)) return "BigInt";

            if (type == typeof(DateTime))
            {
                if (treatDateTimeAsDate) return "Date";
                else return "DateTime";
            }

            if (type == typeof(string)) return "NVarChar(MAX)";



            throw new NotImplementedException(type.ToString() + " Type hasn't corresponding sql type");

        }
    
    }
}
