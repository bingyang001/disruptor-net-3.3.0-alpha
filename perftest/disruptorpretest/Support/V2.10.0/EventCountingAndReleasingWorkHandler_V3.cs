using System.Threading;
using Disruptor;

namespace disruptorpretest.Support
{
    public class EventCountingAndReleasingWorkHandler_V3 : IWorkHandler<ValueEvent>, IEventReleaseAware
    {

        private _Volatile.PaddedLong[] counters;
        private int index;
        private IEventReleaser eventReleaser;

        public EventCountingAndReleasingWorkHandler_V3(_Volatile.PaddedLong[] counters, int index)
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
