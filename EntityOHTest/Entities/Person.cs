using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using EntityOH.Attributes;

namespace EntityOHTest.Entities
{

    [Entity]
    public class Person
    {
        [EntityField(Identity = true, Primary = true)]
        public int Id { get; set; }

        public string Name { get; set; }

        public byte? Age { get; set; }
    }
}
