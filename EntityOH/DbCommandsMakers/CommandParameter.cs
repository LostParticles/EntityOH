using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EntityOH.DbCommandsMakers
{
    
    


        // Select("last name like @ln", 
        //            CommandParameter.Parameter<int>("ln", "hello ya menaiel")
        //                    , Parameter<int>

        // Select("name=@nm", EntityParameter<string>("name", "sadek"))

    // Select ("@hola", CommandParameter.Parameter("@hola", "d
    

    public class CommandParameter
    {
        public string Name;
        public object Value;

        public static CommandParameter Parameter(string name, object value)
        {
            return new CommandParameter { Name = name, Value = value };
        }

        public static CommandParameter Parameter<T>(string name, T value)
        {
            return new CommandParameter { Name = name, Value = value };
        }
    }

}
