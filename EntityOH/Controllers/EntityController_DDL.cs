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
        /// Create the table in the database based on the entity class anatomy ;)
        /// </summary>
        public void CreateTable()
        {
            ExecutePreOperations();

            var cmd = GetCreateTableCommand();

            _Connection.ExecuteNonQuery(cmd);
        }


        private static string SqlTypeFromCLRType(Type clrType, bool treatDateTimeAsDate = false)
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

            if (type == typeof(DateTime))
            {
                if (treatDateTimeAsDate) return "Date";
                else return "DateTime";
            }

            if (type == typeof(string)) return "NVarChar(MAX)";

            if (type == typeof(double)) return "Float";
            if (type == typeof(Single)) return "Real";
            if (type == typeof(Guid)) return "UniqueIdentifier";

            throw new NotImplementedException(type.ToString() + " Type hasn't corresponding sql type");

        }


    }




}