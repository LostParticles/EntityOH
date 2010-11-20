using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using EntityOH.Attributes;
using System.Linq.Expressions;
using System.Reflection.Emit;

namespace EntityOH.Controllers
{
    /// <summary>
    /// Contains the runtime information of entity property and its relation to the database.
    /// </summary>
    internal class EntityFieldRuntime
    {
        /// <summary>
        /// Physical expression that will be executed on database.
        /// </summary>
        public string PhysicalName { get; private set; }

        /// <summary>
        /// Primary Field
        /// </summary>
        public bool Primary { get; private set; }
        

        /// <summary>
        /// Foriegner field
        /// </summary>
        public bool Foriegn { get; private set; }


        /// <summary>
        /// Foriegner Table
        /// </summary>
        public Type ForiegnReferenceType { get; private set; }


        /// <summary>
        /// Foriegner Table Key
        /// </summary>
        public string ForiegnReferenceField { get; private set; }

        /// <summary>
        /// True if field is autoincremented.
        /// </summary>
        public bool Identity { get; private set; }


        public Type FieldType { get; private set; }


        public PropertyInfo FieldPropertyInfo { get; private set; }

        public bool CalculatedExpression { get; set; }
        
        /// <summary>
        /// Constructor for the property of the entity that will take its data from the database.
        /// </summary>
        /// <param name="info"></param>
        public EntityFieldRuntime(PropertyInfo info)
        {
            FieldPropertyInfo = info;
            PhysicalName = info.Name;
            FieldType = info.PropertyType;
            
            if (info.PropertyType.IsValueType == false && info.PropertyType != typeof(string))
            {
                // then it must be a foriegn reference field to another entity
                // that because the constructor will not be called unless the type of the field is pointing to another entity.

                // set the field as foriegn field.
                Foriegn = true;
                ForiegnReferenceType = info.PropertyType;

                // go get the first primary field you encounter in the foriegn type.
                
                // and throw exception if there are no primary fields there
                var fproperties =  EntityRuntimeHelper.EntityProperties(ForiegnReferenceType);
                foreach(var fp in fproperties)
                {
                    var fpA = fp.GetCustomAttributes(typeof(EntityFieldAttribute), false).FirstOrDefault() as EntityFieldAttribute;

                    if (fpA != null)
                    {
                        if (fpA.Primary)
                        {
                            // primary field found
                            // assign its physical name to our foriegn reference property or its name if physical name wasn't found.

                            if (!string.IsNullOrEmpty(fpA.PhysicalNameOrExpression)) ForiegnReferenceField = fpA.PhysicalNameOrExpression;
                            else ForiegnReferenceField = fp.Name;

                            break;
                        }
                    }
                }

                if (string.IsNullOrEmpty(ForiegnReferenceField)) throw new EntityException("The foriegn type doesn't have any primary fields to reference with");
                                  
            }
            
            // find if there are any attributes on the property
            var attrs = info.GetCustomAttributes(typeof(EntityFieldAttribute), true);

            if (attrs.Length > 0)
            {
                var attr = attrs[0] as EntityFieldAttribute;

                if (attr != null)
                {
                    if (!string.IsNullOrEmpty(attr.PhysicalNameOrExpression)) PhysicalName = attr.PhysicalNameOrExpression;
                    Primary = attr.Primary;
                    Identity = attr.Identity;
                    CalculatedExpression = attr.CalculatedExpression;

                    // may be there is another primary field that we don't know and the programmer wrote it in the attribute.
                    if (!string.IsNullOrEmpty(attr.ReferencePropertyName))
                    {
                        var fp = EntityRuntimeHelper.EntityProperties(ForiegnReferenceType).FirstOrDefault((pi) => pi.Name.Equals(attr.ReferencePropertyName, StringComparison.OrdinalIgnoreCase));
                        if (fp == null) throw new EntityException("Reference field of this name wasn't found in the target entity type");

                        var fpA = fp.GetCustomAttributes(typeof(EntityFieldAttribute), false).FirstOrDefault() as EntityFieldAttribute;
                        if (fpA != null)
                        {
                            if (fpA.Primary)
                            {
                                // primary field found
                                // assign its physical name to our foriegn reference property or its name if physical name wasn't found.

                                if (!string.IsNullOrEmpty(fpA.PhysicalNameOrExpression)) ForiegnReferenceField = fpA.PhysicalNameOrExpression;
                                else ForiegnReferenceField = fp.Name;
                            }
                            else
                            {
                                throw new EntityException("The referenced property is not decorated with primary in its attribute");
                            }
                        }
                        else
                        {
                            throw new EntityException("The referenced property is not decorated with any attributes, which make me feel that it is not a primary field :)");
                        }
                    }
                 
                }
            }
        }


        /// <summary>
        /// Read the property value.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public object FieldReader(object entity)
        {
            return FieldPropertyInfo.GetValue(entity, null);
        }

        /// <summary>
        /// Wrtie to the property value.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="value"></param>
        public void FieldWriter(object entity, object value)
        {
            FieldPropertyInfo.SetValue(entity, value, null);
        }
        
    }

}
