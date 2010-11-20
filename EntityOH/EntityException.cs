using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EntityOH
{
    [Serializable]
    public class EntityException : Exception
    {
        public EntityException() : base() { }

        public EntityException(string msg)
            : base(msg) 
        { }
    }
}
