using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Disruptor;

namespace disruptorpretest.V3._3._0.Immutable
{
    public class EventHolderHandler : IEventHandler<EventHolder>
    {
        private readonly IEventHandler<SimpleEvent> @delegate;

        public EventHolderHandler(IEventHandler<SimpleEvent> @delegate)
        {
            this.@delegate = @delegate;
        }


        public void OnEvent(EventHolder holder, long sequence, bool endOfBatch)
        {
            @delegate.OnEvent(holder.@event, sequence, endOfBatch);
            holder.@event = null;
        }
    }
}
