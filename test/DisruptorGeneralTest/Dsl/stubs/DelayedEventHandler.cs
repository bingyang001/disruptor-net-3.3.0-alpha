using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Disruptor;


namespace DisruptorGeneralTest.Dsl.stubs
{
    public class DelayedEventHandler : IEventHandler<TestEvent>, ILifecycleAware
    {
        private readonly _Volatile.Boolean readyToProcessEvent = new _Volatile.Boolean(false);
        private volatile bool stopped = false;
        private readonly Barrier barrier;

        public DelayedEventHandler(Barrier barrier)
        {
            this.barrier = barrier;
        }

        public DelayedEventHandler()
            : this(new Barrier(2))
        {

        }
        private void WaitForAndSetFlag(bool newValue)
        {
            while (!stopped && Thread.CurrentThread.IsAlive &&
                   !readyToProcessEvent.AtomicCompareExchange(!newValue, newValue))
            {
                Thread.Sleep(0);
            }
        }
        public void awaitStart()
        {
            barrier.SignalAndWait();
        }
        public void ProcessEvent()
        {
            WaitForAndSetFlag(true);
        }

        public void StopWaiting()
        {
            stopped = true;
        }
        #region IEventHandler<TestEvent> 成员

        public void OnEvent(TestEvent @event, long sequence, bool endOfBatch)
        {
            WaitForAndSetFlag(false);
        }

        #endregion

        #region ILifecycleAware 成员

        public void OnStart()
        {
            try
            {
                barrier.SignalAndWait();
            }
            catch (Exception e)
            {
                throw;
            }
        }

        public void OnShutdown()
        {
        }

        #endregion
    }
}
