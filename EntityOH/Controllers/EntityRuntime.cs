using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using EntityOH.Attributes;
using System.Data;
using System.Reflection;
using System.Data.Common;
using System.Data.SqlClient;

namespace EntityOH.Controllers
{

    /// <summary>
    /// Hold the runtime information of entity.
    /// </summary>
    /// <typeparam name="Entity"></typeparam>
    internal static class EntityRuntime<Entity>
    {

        /// <summary>
        /// Current Entity Database Physical Name
        /// </summary>
        public static string PhysicalName { get; private set; }

        public static Type BaseEntityType;


        /// <summary>
        /// Indicates if the entity is inherited from another entity.
        /// The flag is false in case of inheriting non entity class.
        /// </summary>
        public static bool Inherited { get; private set; }

        public static LambdaExpression MappingExpression { get; private set; }

        /// <summary>
        /// Mapping between data reader and the whole entity public writable properties.
        /// </summary>
        public static Func<IDataReader, Entity> MappingFunction { get; private set; }


        public static LambdaExpression PartialMappingExpression { get; private set; }
        public static Func<SmartReader, Entity> PartialMappingFunction { get; private set; }

        public static Dictionary<string, EntityFieldRuntime> FieldsRuntime = new Dictionary<string, EntityFieldRuntime>(StringComparer.OrdinalIgnoreCase);

        
        static EntityRuntime()
        {
            Type EntityType = typeof(Entity);

            // add suitable properties into the fields runtime

            foreach (var pp in EntityRuntimeHelper.EntityProperties(EntityType))
            {
                FieldsRuntime.Add(pp.Name, new EntityFieldRuntime(pp));
            }

            var attr = (EntityAttribute)EntityType.GetCustomAttributes(typeof(EntityAttribute), false).FirstOrDefault();

            // Set the true name of the entity in the database  (the mapping I mean)
            PhysicalName = attr.PhysicalName.Trim();


            Inherited = EntityRuntimeHelper.IsEntityInherited(EntityType, out BaseEntityType);

            // Prepare Mapping Expression
            Type IDataRecordType = typeof(IDataRecord);

            ParameterExpression RecordParameterExpression = Expression.Parameter(IDataRecordType, "reader");

            // generate the expression that map entity with datareader.
            //  expression function is on the signature  Entity Map(IDataReader)

            var NewEntity = Expression.New(EntityType);

            List<MemberBinding> bindings = new List<MemberBinding>();

            // search for every field in DataRecord and return its value in initializer then compile all of this.
            foreach (var fr in FieldsRuntime)
            {
                //  int IDataReader.GetOrdinal(string pp.Name)
                var ReaderOrdinal = Expression.Call(RecordParameterExpression, IDataRecordType.GetMethod("GetOrdinal"), Expression.Constant(EntityType.Name + "." + fr.Value.FieldPropertyInfo.Name)); //selecting inside reader with the property name because we are renaming the return columns with the names of the entity properties.
                
                // object IDataReader.GetObject(int ordinal )
                var RecordValue = Expression.Call(RecordParameterExpression, IDataRecordType.GetMethod("GetValue"), ReaderOrdinal);


                //check if the type of the field is foriegn field.
                if (fr.Value.Foriegn)
                {
                    // here I should get the data from the select statement that were executed by left join

                    // field is type and its data will be found based on its type name in the reader
                    // as TypeName.FieldName

                    NewExpression NewRefEntity = Expression.New(fr.Value.FieldType);
                    List<MemberBinding> RefBindings = new List<MemberBinding>();
                    var RefFields = EntityRuntimeHelper.EntityRuntimeFields(fr.Value.FieldType);
                    foreach (var rf in RefFields)
                    {
                        var RefReaderOrdinal = Expression.Call(RecordParameterExpression, IDataRecordType.GetMethod("GetOrdinal"), Expression.Constant(fr.Value.FieldType.Name + "." + rf.FieldPropertyInfo.Name));
                        var RefRecordValue = Expression.Call(RecordParameterExpression, IDataRecordType.GetMethod("GetValue"), RefReaderOrdinal);
                        var RefValue = Expression.Convert(RefRecordValue, rf.FieldType);
                        var Refbinding = Expression.Bind(rf.FieldPropertyInfo, RefValue);

                        RefBindings.Add(Refbinding);
                    }
                    var RefInitializer = Expression.MemberInit(NewRefEntity, RefBindings.ToArray());

                    var binding = Expression.Bind(fr.Value.FieldPropertyInfo, RefInitializer);

                    bindings.Add(binding);

                }
                else
                {

                    // and then convert the value to the target type
                    var Value = Expression.Convert(RecordValue, fr.Value.FieldType);

                    // bind this value to the initial assigning list.
                    var binding = Expression.Bind(fr.Value.FieldPropertyInfo, Value);
                    bindings.Add(binding);
                }
            }

            // initialize the object expression.  
            // this is the same like  var p = new Point(){x=3,y=5};
            Expression initializer = Expression.MemberInit(NewEntity, bindings.ToArray());

            // Make the lambda expression
            MappingExpression = Expression.Lambda<Func<IDataReader, Entity>>(initializer, RecordParameterExpression);

            // compile the lambda expression into function to be called as var entity = MappingFunction(reader)
            MappingFunction = (Func<IDataReader, Entity>)MappingExpression.Compile();


            #region Partial Mapping Function
            // Prepare Mapping Expression

            Type SmartReaderType = typeof(SmartReader);

            RecordParameterExpression = Expression.Parameter(SmartReaderType, "reader");


            bindings = new List<MemberBinding>();

            // search for every field in DataRecord and return its value in initializer then compile all of this.
            foreach (var fr in FieldsRuntime)
            {
                var FieldValueMethod = SmartReaderType.GetMethod("GetFieldValue").MakeGenericMethod(fr.Value.FieldType);                
                
                var ReaderValue = Expression.Call(RecordParameterExpression, FieldValueMethod, Expression.Constant(fr.Value.PhysicalName));

                var Value = Expression.Convert(ReaderValue, fr.Value.FieldType);

                var binding = Expression.Bind(fr.Value.FieldPropertyInfo, Value);

                bindings.Add(binding);
            }

            initializer = Expression.MemberInit(NewEntity, bindings.ToArray());

            PartialMappingExpression = Expression.Lambda<Func<SmartReader, Entity>>(initializer, RecordParameterExpression);

            PartialMappingFunction = (Func<SmartReader, Entity>)PartialMappingExpression.Compile();

            #endregion
            
        }


    }
}
