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

            var cmd = DatabaseCommands.GetCreateTableCommand();

            _LastSqlStatement = cmd.CommandText;
            _Connection.ExecuteNonQuery(cmd);
        }

    }




}