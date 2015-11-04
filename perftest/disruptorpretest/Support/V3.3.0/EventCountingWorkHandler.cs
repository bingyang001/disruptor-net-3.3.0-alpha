using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Disruptor;

namespace disruptorpretest.Support.V3._3._0
{
    public class EventCountingWorkHandler
     : IWorkHandler<ValueEvent>
    {
        private readonly _Volatile.PaddedLong[] counters;
        private readonly int index;

        public EventCountingWorkHandler(_Volatile.PaddedLong[] counters, int index)
        {
            this.counters = counters;
            this.index = index;
        }


        public void OnEvent(ValueEvent @event)
        {
            counters[index].WriteUnfenced(counters[index].ReadUnfenced() + 1L);
        }
    }
}
