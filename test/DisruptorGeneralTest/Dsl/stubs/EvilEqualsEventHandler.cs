using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Disruptor;

namespace DisruptorGeneralTest.Dsl.stubs
{
    public class EvilEqualsEventHandler : IEventHandler<TestEvent>
    {
        #region IEventHandler<TestEvent> 成员

        public void OnEvent(TestEvent @event, long sequence, bool endOfBatch)
        {
            //throw new NotImplementedException();
        }

        #endregion
    }
}
