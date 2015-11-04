using System;
using System.Threading;

using Disruptor;

namespace disruptorpretest.Support
{
    public class Ponger : IEventHandler<ValueEvent>, ILifecycleAware
    {

        private readonly RingBuffer<ValueEvent> buffer;
        private Barrier barrier;

        public Ponger(RingBuffer<ValueEvent> buffer)
        {
            this.buffer = buffer;
        }
        public void Reset(Barrier barrier)
        {
            this.barrier = barrier;
        }
        #region IEventHandler<ValueEvent> 成员

        public void OnEvent(ValueEvent @event, long sequence, bool endOfBatch)
        {
            long next = buffer.Next();
            buffer[next].Value = @event.Value;
            buffer.Publish(next);
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
            throw new NotImplementedException();
        }

        #endregion
    }
}
