using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace disruptorpretest.V3._3._0.Immutable
{
   public class EventHolder
    {
   
        public EventHolder newInstance()
        {
            return new EventHolder();
        }
    

    public SimpleEvent @event;
    }
}
