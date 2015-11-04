using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Disruptor;

namespace disruptorpretest.Support.V3._3._0
{
    public class EventCountingAndReleasingWorkHandler :
    IWorkHandler<ValueEvent>, IEventReleaseAware
    {
        private readonly _Volatile.PaddedLong[] counters;
        private readonly int index;
        private IEventReleaser eventReleaser;
        public EventCountingAndReleasingWorkHandler(_Volatile.PaddedLong[] counters
        , int index)
        {
            this.counters = counters;
            this.index = index;
        }
        #region IWorkHandler<ValueEvent> 成员

        public void OnEvent(ValueEvent @event)
        {
            eventReleaser.Release();
            counters[index].WriteUnfenced(counters[index].ReadUnfenced() + 1L);
        }

        #endregion

        #region IEventReleaseAware 成员

        public void SetEventReleaser(IEventReleaser eventReleaser)
        {
            this.eventReleaser = eventReleaser;
        }

        #endregion
    }
}
