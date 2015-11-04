using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Disruptor;

using System.Threading;
namespace DisruptorGeneralTest.Dsl.stubs
{
    public class SleepingEventHandler : IEventHandler<TestEvent>
    {
        #region IEventHandler<TestEvent> 成员

        public void OnEvent(TestEvent @event, long sequence, bool endOfBatch)
        {
            Thread.Sleep(1000);
        }

        #endregion
    }
}
