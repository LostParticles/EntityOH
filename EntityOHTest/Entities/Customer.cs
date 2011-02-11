using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EntityOHTest.Entities
{
    public class Customer : Person
    {
        public string preferences { get; set; }

        public Guid? SystemId { get; set; }
    }
}
