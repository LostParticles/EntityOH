using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EntityOH.Attributes;

namespace EntityOHSamples
{

    
    class Person
    {

        [EntityField(Primary = true, Identity=true)]
        public int ID { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }

        public string Email { get; set; }

        public string Phone { get; set; }


    }
}
