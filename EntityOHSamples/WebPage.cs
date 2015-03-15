using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EntityOH.Attributes;

namespace EntityOHSamples
{
    public class WebPage
    {

        [EntityField(Primary = true, Identity = true)]
        public int ID { get; set; }

        public string Url { get; set; }

        public string Source { get; set; }
    }
}
