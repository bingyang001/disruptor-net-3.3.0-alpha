using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Disruptor;

namespace DisruptorGeneralTest.Dsl.stubs
{
    public class ExceptionThrowingEventHandler : IEventHandler<TestEvent>
    {
        #region IEventHandler<TestEvent> 成员

        public void OnEvent(TestEvent @event, long sequence, bool endOfBatch)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
