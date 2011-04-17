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
    internal  partial class EntityRuntime<Entity>
    {


        private string _RunningPhysicalName;

        /// <summary>
        /// Current Entity Database Physical Name as was given to the controller while initializing 
        /// </summary>
        public string RunningPhysicalName
        {
            get
            {
                if (string.IsNullOrEmpty(_RunningPhysicalName))
                    return EntityRuntimeHelper.EntityPhysicalName(typeof(Entity));
                else
                    return _RunningPhysicalName;
            }
            set
            {
                _RunningPhysicalName = value;
            }
        }


        /// <summary>
        /// Physical name of entity just as declared in attribute
        /// </summary>
        public static string PhysicalName
        {
            get
            {
                return EntityRuntimeHelper.EntityPhysicalName(typeof(Entity));
            }
        }

        public static string FromClause
        {
            get
            {
                var ofrom = PhysicalName;


                EntityFieldRuntime pkey = null;

                foreach (var f in EntityRuntimeHelper.EntityRuntimeFields(typeof(Entity)))
                {
                    if (f.Primary) pkey = f;
                    if (f.Foriegn)
                    {
                        ofrom += " LEFT JOIN " + EntityRuntimeHelper.EntityPhysicalName(f.ForiegnReferenceType);
                        ofrom += " ON ";
                        ofrom += PhysicalName + "." + f.PhysicalName;
                        ofrom += " = ";
                        ofrom += EntityRuntimeHelper.EntityPhysicalName(f.ForiegnReferenceType) + "." + f.ForiegnReferenceField;
                    }
                }

                if (pkey != null)
                {
                    Type bt;
                    if (EntityRuntimeHelper.IsEntityInherited(typeof(Entity), out bt))
                    {
                        string bname = EntityRuntimeHelper.EntityPhysicalName(bt); //how to know the field name that is one to one with the parent table.

                        // assuming the same name.

                        ofrom += " INNER JOIN " + bname + " ON " + PhysicalName + "." +
                            pkey.PhysicalName + " = " + EntityRuntimeHelper.EntityPhysicalName(bt) + "." + pkey.PhysicalName;
                    }
                }
                return ofrom;

            }
        }


        public string RunningFromClause
        {
            get
            {
                var ofrom = this.RunningPhysicalName;


                EntityFieldRuntime pkey = null;

                foreach (var f in EntityRuntimeHelper.EntityRuntimeFields(typeof(Entity)))
                {
                    if (f.Primary) pkey = f;
                    if (f.Foriegn)
                    {
                        ofrom += " LEFT JOIN " + EntityRuntimeHelper.EntityPhysicalName(f.ForiegnReferenceType);
                        ofrom += " ON ";
                        ofrom += RunningPhysicalName + "." + f.PhysicalName;
                        ofrom += " = ";
                        ofrom += EntityRuntimeHelper.EntityPhysicalName(f.ForiegnReferenceType) + "." + f.ForiegnReferenceField;
                    }
                }

                if (pkey != null)
                {
                    Type bt;
                    if (EntityRuntimeHelper.IsEntityInherited(typeof(Entity), out bt))
                    {
                        string bname = EntityRuntimeHelper.EntityPhysicalName(bt); //how to know the field name that is one to one with the parent table.

                        // assuming the same name.

                        ofrom += " INNER JOIN " + bname + " ON " + RunningPhysicalName + "." +
                            pkey.PhysicalName + " = " + EntityRuntimeHelper.EntityPhysicalName(bt) + "." + pkey.PhysicalName;
                    }
                }
                return ofrom;

            }
        }

        public static IEnumerable<EntityFieldRuntime> EntityRuntimeFields
        {
            get
            {

                return EntityRuntimeHelper.EntityRuntimeFields(typeof(Entity));
            }
        }

        public IEnumerable<string> RunningFieldsList
        {
            get
            {
                return EntityRuntimeHelper.FieldsList(typeof(Entity), RunningPhysicalName);
            }
        }

        public static IEnumerable<string> FieldsList
        {
            get
            {
                return EntityRuntimeHelper.FieldsList(typeof(Entity));
            }
        }
        public string RunningGroupByExpression
        {

            get
            {
                return EntityRuntimeHelper.GroupByExpression(typeof(Entity), RunningPhysicalName);
            }
        }


        public static string GroupByExpression
        {

            get
            {
                return EntityRuntimeHelper.GroupByExpression(typeof(Entity));
            }
        }

        
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



        static void ConstructTheMappingFunction()
        {
            Type EntityType = typeof(Entity);

            // add suitable properties into the fields runtime

            foreach (var pp in EntityFieldRuntime.EntityProperties(EntityType))
            {
                FieldsRuntime.Add(pp.Name, new EntityFieldRuntime(pp));
            }

            Inherited = EntityRuntimeHelper.IsEntityInherited(EntityType, out BaseEntityType);

            // Prepare Mapping Expression
            Type IDataRecordType = typeof(IDataRecord);

            ParameterExpression RecordParameterExpression = Expression.Parameter(IDataRecordType, "reader");

            // generate the expression that map entity with datareader.
            //  expression function is on the signature  Entity Map(IDataReader)

            var NewEntity = Expression.New(EntityType);

            List<MemberBinding> bindings = new List<MemberBinding>();

            MethodInfo GetOrdinalInfo = IDataRecordType.GetMethod("GetOrdinal");
            MethodInfo GetValueInfo = IDataRecordType.GetMethod("GetValue");
            MethodInfo IsDBNullInfo = IDataRecordType.GetMethod("IsDBNull");

            // search for every field in DataRecord and return its value in initializer then compile all of this.
            foreach (var fr in FieldsRuntime)
            {
                //  int IDataReader.GetOrdinal(string pp.Name)
                var ReaderOrdinal = Expression.Call(RecordParameterExpression, GetOrdinalInfo, Expression.Constant(EntityType.Name + EntityRuntimeHelper.AliasSeparator + fr.Value.FieldPropertyInfo.Name)); //selecting inside reader with the property name because we are renaming the return columns with the names of the entity properties.

                // object IDataReader.GetObject(int ordinal )
                var RecordValue = Expression.Call(RecordParameterExpression, GetValueInfo, ReaderOrdinal);

                //check if the type of the field is foriegn field.
                if (fr.Value.Foriegn)
                {
                    #region Foriegn field mapping to another entity
                    // here I should get the data from the select statement that were executed by left join

                    // field is type and its data will be found based on its type name in the reader
                    // as TypeName__FieldName

                    NewExpression NewRefEntity = Expression.New(fr.Value.FieldType);
                    List<MemberBinding> RefBindings = new List<MemberBinding>();
                    var RefFields = EntityRuntimeHelper.EntityRuntimeFields(fr.Value.FieldType);

                    foreach (var rf in RefFields)
                    {
                        // prevent second level indirection in referencing.
                        if (rf.FieldType.IsValueType == true || rf.FieldType == typeof(string) || rf.FieldType == typeof(byte[]))
                        {
                            var RefReaderOrdinal = Expression.Call(RecordParameterExpression, GetOrdinalInfo, Expression.Constant(fr.Value.FieldType.Name + EntityRuntimeHelper.AliasSeparator + rf.FieldPropertyInfo.Name));
                            var RefRecordValue = Expression.Call(RecordParameterExpression, GetValueInfo, RefReaderOrdinal);
                            var RefValue = Expression.Convert(RefRecordValue, rf.FieldType);
                            var Refbinding = Expression.Bind(rf.FieldPropertyInfo, RefValue);

                            RefBindings.Add(Refbinding);
                        }
                    }


                    var IsValueNull = Expression.Call(RecordParameterExpression, IsDBNullInfo, ReaderOrdinal);

                    var RefInitializer = Expression.MemberInit(NewRefEntity, RefBindings.ToArray());  //the reference type initialized here

                    // binding here should depend if there is value in reader or not.
                    var Ref = Expression.Condition(IsValueNull, Expression.Constant(null, fr.Value.FieldType), RefInitializer);

                    var binding = Expression.Bind(fr.Value.FieldPropertyInfo, Ref);

                    bindings.Add(binding);
                    #endregion
                }
                else
                {
                    #region Normal Field

                    if (fr.Value.FieldType.IsGenericType && fr.Value.FieldType.IsValueType)
                    {
                        // Nullable types are the only types that are value type and generictype in the same time

                        // Nullable type like int?  
                        //  int? value = 5 or null

                        var IsValueNull = Expression.Call(RecordParameterExpression, IsDBNullInfo, ReaderOrdinal);

                        var Value = Expression.Convert(RecordValue, fr.Value.FieldType);

                        var ToBeOrNotToBe = Expression.Condition(IsValueNull, Expression.Constant(null, fr.Value.FieldType), Value);

                        // bind this value to the initial assigning list.
                        var binding = Expression.Bind(fr.Value.FieldPropertyInfo, ToBeOrNotToBe);

                        bindings.Add(binding);

                    }
                    else if (fr.Value.FieldType.IsClass)
                    {
                        // normal class maybe string 
                        var IsValueNull = Expression.Call(RecordParameterExpression, IsDBNullInfo, ReaderOrdinal);

                        var Value = Expression.Convert(RecordValue, fr.Value.FieldType);

                        var ToBeOrNotToBe = fr.Value.FieldType == typeof(string) ? Expression.Condition(IsValueNull, Expression.Constant(string.Empty, typeof(string)), Value) : Expression.Condition(IsValueNull, Expression.Constant(null, fr.Value.FieldType), Value);

                        // bind this value to the initial assigning list.
                        var binding = Expression.Bind(fr.Value.FieldPropertyInfo, ToBeOrNotToBe);

                        bindings.Add(binding);
                    }
                    else
                    {
                        // this is a normal value type.

                        // and then convert the value to the target type
                        var Value = Expression.Convert(RecordValue, fr.Value.FieldType);


                        // bind this value to the initial assigning list.
                        var binding = Expression.Bind(fr.Value.FieldPropertyInfo, Value);

                        bindings.Add(binding);
                    }
                    #endregion
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
        
        static EntityRuntime()
        {
            ConstructTheMappingFunction();
        }

    }
}
