using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Disruptor;

namespace disruptorpretest.V3._3._0.Immutable
{
    public class SimpleEventHandler : IEventHandler<SimpleEvent>
    {
        public long counter;


        public void OnEvent(SimpleEvent arg0, long arg1, bool arg2)
        {
            counter += arg0.getCounter();
        }
    }
}
