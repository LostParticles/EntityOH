using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EntityOH.Attributes
{

    /// <summary>
    /// Decorating the class to indicate it as an entity.
    /// </summary>
    public sealed class EntityAttribute : Attribute
    {

        /// <summary>
        /// The entity physical name in database.
        /// </summary>
        public string PhysicalName { get; set; }


    }

    
    /// <summary>
    /// Decorating field to indicate if it is primary field
    /// </summary>
    public class EntityFieldAttribute : Attribute
    {
        /// <summary>
        /// Physical Expression or Physical Name in database
        /// </summary>
        public string PhysicalNameOrExpression { get; set; }


        /// <summary>
        /// Indicates if the field is primary field or not.
        /// </summary>
        public bool Primary { get; set; }


        /// <summary>
        /// Indicates if the field is identity field or not.
        /// </summary>
        public bool Identity { get; set; }


        /// <summary>
        /// The property name in the reference type, if the decorated field type of another entity type.
        /// </summary>
        public string ReferencePropertyName { get; set; }


        /// <summary>
        /// Indicates that the property carrying this attribute is only intended to be used in Select statements not in update or insert.
        /// </summary>
        public bool CalculatedExpression { get; set; }
    }



}
