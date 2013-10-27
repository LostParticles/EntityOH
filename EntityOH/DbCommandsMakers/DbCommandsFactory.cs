using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;

namespace EntityOH.DbCommandsMakers
{
    public static class DbCommandsFactory
    {
        static Dictionary<string, Type> DbCommandsProviders = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

        static DbCommandsFactory()
        {
            // register this library built in commands makers.

            DbCommandsProviders.Add("System.Data.SqlClient", typeof(SqlServerCommandsMaker<>));
            DbCommandsProviders.Add("System.Data.OleDb", typeof(OleDbCommandsMaker<>));
            DbCommandsProviders.Add("System.Data.SqlServerCe", typeof(SqlServerCompactCommandsMaker<>));

            // find classes that holds specific attribute
        }


        /// <summary>
        /// Register DbCommands Type for the sake of implementing DbCommands later for other providers.
        /// </summary>
        /// <param name="DbCommandsMakerType"></param>
        /// <param name="provider">providername string that appear in the configuration connection string entry </param>
        public static void RegisterDbCommandsMaker(string provider, Type DbCommandsMakerType)
        {
            DbCommandsProviders.Add(provider, DbCommandsMakerType);
        }

        static Dictionary<Type, DbCommandsMaker<EmptyEntity>> EmptyDbCommands = new Dictionary<Type, DbCommandsMaker<EmptyEntity>>();


        /// <summary>
        /// Gets the commands maker of this provider
        /// </summary>
        /// <param name="provider"></param>
        /// <returns></returns>
        public static DbCommandsMaker<EmptyEntity> GetProviderCommandsMaker(string provider)
        {
            Type dbs = DbCommandsProviders[provider];

            DbCommandsMaker<EmptyEntity> EmptyDbCommand;

            if (!EmptyDbCommands.TryGetValue(dbs, out EmptyDbCommand))
            {
                var dbs_object = dbs.MakeGenericType(typeof(EmptyEntity));
                object[] args = new object[] { null };

                var oo = Activator.CreateInstance(dbs_object, args);

                EmptyDbCommand = (DbCommandsMaker<EmptyEntity>)oo;

                EmptyDbCommands.Add(dbs, EmptyDbCommand);
            }
            return EmptyDbCommand;
        }

        // create instance method overhead is negligible because i am caching the results later.

        public static DbConnection GetConnection(string provider, string connectionString)
        {
            var EmptyDbCommand = GetProviderCommandsMaker(provider);
            return EmptyDbCommand.GetNewConnection(connectionString);
        }

        public static DbCommand GetCommand(string provider)
        {
            var EmptyDbCommand = GetProviderCommandsMaker(provider);

            return EmptyDbCommand.GetNewCommand();
        }

        public static DbCommandsMaker<Entity> GetCommandsMaker<Entity>(string provider)
        {
            var EmptyDbCommand = GetProviderCommandsMaker(provider);
            return EmptyDbCommand.GetThisMaker<Entity>();
        }
    
    }
}
