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
    public class EntityFieldRuntime
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


        public Type FieldType
        {
            get
            {
                return FieldPropertyInfo.PropertyType;
            }
        }


        /// <summary>
        /// True when the field should be included in the group by.
        /// </summary>
        public bool GroupedBy { get; private set; }



        public PropertyInfo FieldPropertyInfo { get; private set; }

        public bool CalculatedExpression { get; set; }



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
                if (
                    pp.PropertyType.IsValueType                // integers and numbers in general.
                    || pp.PropertyType == typeof(string)       // text ofcourse.
                    || pp.PropertyType == typeof(byte[])       // bytes of data.
                    )
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



        /// <summary>
        /// Constructor for the property of the entity that will take its data from the database.
        /// </summary>
        /// <param name="info"></param>
        public EntityFieldRuntime(PropertyInfo info)
        {
            FieldPropertyInfo = info;
            PhysicalName = info.Name;
            
            
            if (info.PropertyType.IsValueType == false && info.PropertyType != typeof(string) && info.PropertyType != typeof(byte[]))
            {
                // then it must be a foriegn reference field to another entity
                // that because the constructor will not be called unless the type of the field is pointing to another entity.

                // set the field as foriegn field.
                Foriegn = true;
                ForiegnReferenceType = info.PropertyType;

                // go get the first {primary field} you encounter in the foriegn type.
                
                // and throw exception if there are no primary fields there
                var fproperties =  EntityProperties(ForiegnReferenceType);
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
                    GroupedBy = attr.GroupedBy;

                    // may be there is another primary field that we don't know and the programmer wrote it in the attribute.
                    if (!string.IsNullOrEmpty(attr.ReferencePropertyName))
                    {
                        var fp = EntityProperties(ForiegnReferenceType).FirstOrDefault((pi) => pi.Name.Equals(attr.ReferencePropertyName, StringComparison.OrdinalIgnoreCase));
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
