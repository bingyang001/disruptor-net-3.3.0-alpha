using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Disruptor;

namespace DisruptorGeneralTest.Dsl.stubs
{
    public class EventHandlerStub : IEventHandler<TestEvent>
    {
        private readonly CountdownEvent ce;
        public EventHandlerStub(CountdownEvent ce)
        {
            this.ce = ce;
        }
        #region IEventHandler<TestEvent> 成员

        public void OnEvent(TestEvent @event, long sequence, bool endOfBatch)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
