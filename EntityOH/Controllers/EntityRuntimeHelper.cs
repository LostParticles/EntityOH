using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using EntityOH.Attributes;

namespace EntityOH.Controllers
{
    internal partial class EntityRuntime<Entity>
    {
        private static class EntityRuntimeHelper
        {

            internal const string AliasSeparator = "__";

            private static Dictionary<Type, string> EntitiesPhysicalName = new Dictionary<Type, string>();


            internal static string EntityPhysicalName(Type entityType)
            {
                string ph;

                lock (EntitiesPhysicalName)
                {
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
                lock (EntitiesFields)
                {
                    if (!EntitiesFields.TryGetValue(entityType, out efs))
                    {
                        efs = new List<EntityFieldRuntime>();

                        foreach (var pp in EntityFieldRuntime.EntityProperties(entityType))
                        {
                            efs.Add(new EntityFieldRuntime(pp));
                        }

                        EntitiesFields.Add(entityType, efs);
                    }
                }

                return efs;
            }


            /// <summary>
            /// 
            /// </summary>
            /// <param name="entityType"></param>
            /// <param name="physicalTableName">Override the attribute name of entity or entity name in case of specific table name that wasn't mapped mentioned before.</param>
            /// <returns></returns>
            internal static IEnumerable<string> FieldsList(Type entityType, string physicalTableName = "")
            {
                List<string> _FieldsList = new List<string>();
                if (string.IsNullOrEmpty(physicalTableName)) physicalTableName = EntityRuntimeHelper.EntityPhysicalName(entityType);
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

                        _FieldsList.Add("(" + fld.PhysicalName + ") AS " + entityType.Name + AliasSeparator + fld.FieldPropertyInfo.Name);
                    }
                    else
                    {
                        if (inherited)
                        {
                            if (fld.FieldPropertyInfo.DeclaringType == bt)
                            {
                                _FieldsList.Add(BaseTableName + "." + fld.PhysicalName + " AS " + entityType.Name + AliasSeparator + fld.FieldPropertyInfo.Name);
                                goto gg;
                            }
                        }

                        // Format:  TableName.FieldPhysicalName AS EntityName__PropertyName
                        _FieldsList.Add(physicalTableName + "." + fld.PhysicalName + " AS " + entityType.Name + AliasSeparator + fld.FieldPropertyInfo.Name);

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


            internal static string GroupByExpression(Type entityType, string physicalTableName = "")
            {
                string GroupedByFields = string.Empty;

                if (string.IsNullOrEmpty(physicalTableName)) physicalTableName = EntityRuntimeHelper.EntityPhysicalName(entityType);

                foreach (var f in EntityRuntimeFields(entityType))
                {
                    if (f.GroupedBy)
                    {
                        if (f.Foriegn)
                        {
                            // if the key is forienger which mean it is another entity
                            // then all fields on this other enitity should be included in the group by.

                            foreach (var ff in EntityRuntimeFields(f.ForiegnReferenceType))
                            {
                                GroupedByFields += "," + EntityPhysicalName(f.ForiegnReferenceType) + "." + ff.PhysicalName;
                            }
                        }
                        else
                        {
                            GroupedByFields += "," + physicalTableName + "." + f.PhysicalName;
                        }
                    }
                }

                if (string.IsNullOrEmpty(GroupedByFields))
                {
                    return string.Empty;
                }
                else
                {
                    return GroupedByFields.TrimStart(',');
                }
            }
        }
    }
}
