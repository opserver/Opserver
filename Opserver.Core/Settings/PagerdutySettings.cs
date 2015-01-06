using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StackExchange.Opserver
{
    public class PagerdutySettings : Settings<PagerdutySettings>, IAfterLoadActions
    {
        public override bool Enabled { get { return APIKey.HasValue(); } }
        public string APIKey { get; set; }

        public void AfterLoad() 
        { 
        }


    }
}
