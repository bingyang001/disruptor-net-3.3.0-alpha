using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Disruptor;

namespace DisruptorGeneralTest.Dsl.stubs
{
    public class TestWorkHandler : IWorkHandler<TestEvent>
    {
        private readonly _Volatile.Boolean readyToProcessEvent = new _Volatile.Boolean(false);
        private volatile bool stopped = false;

        #region IWorkHandler<TestEvent> 成员

        public void OnEvent(TestEvent @event)
        {
            WaitForAndSetFlag(false);
        }

        #endregion
        public void processEvent()
        {
            WaitForAndSetFlag(true);
        }

        public void stopWaiting()
        {
            stopped = true;
        }

        private void WaitForAndSetFlag(bool newValue)
        {
            while (!stopped && Thread.CurrentThread.IsAlive &&
                   !readyToProcessEvent.AtomicCompareExchange(!newValue, newValue))
            {
                Thread.Sleep(0);
            }
        }
    }
}
