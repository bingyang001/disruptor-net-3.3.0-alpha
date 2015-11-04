using System;
using System.Threading;
using System.Diagnostics;

using Disruptor;
using Disruptor.Collections;


namespace disruptorpretest.Support
{
    public class Pinger : IEventHandler<ValueEvent>, ILifecycleAware
    {
        private RingBuffer<ValueEvent> buffer;
        private readonly long maxEvents;
        private readonly long pauseTimeNs;

        private long counter = 0;
        private Barrier barrier;
        private ManualResetEvent latch;
        private Histogram histogram;
        private long t0;

        public Pinger(RingBuffer<ValueEvent> buffer, long maxEvents, long pauseTimeNs)
        {
            this.buffer = buffer;
            this.maxEvents = maxEvents;
            this.pauseTimeNs = pauseTimeNs;
        }
        public void Reset(Barrier barrier, ManualResetEvent latch, Histogram histogram)
        {
            this.histogram = histogram;
            this.barrier = barrier;
            this.latch = latch;

            counter = 0;
        }
        #region IEventHandler<ValueEvent> 成员

        public void OnEvent(ValueEvent @event, long sequence, bool endOfBatch)
        {
            var t1 = Stopwatch.GetTimestamp();
            histogram.AddObservation(t1 - t0 - pauseTimeNs);
            if (@event.Value < maxEvents)
            {
                while (pauseTimeNs > (Stopwatch.GetTimestamp() - t1))
                {
                    Thread.Sleep(0);
                }

                Send();
            }
            else
            {
                latch.Set();
            }
        }

        private void Send()
        {
            t0 = Stopwatch.GetTimestamp();
            long next = buffer.Next();
            buffer[next].Value = counter;
            buffer.Publish(next);

            counter++;
        }


        #endregion

        #region ILifecycleAware 成员

        public void OnStart()
        {
            try
            {
                barrier.SignalAndWait();

                Thread.Sleep(1000);
                Send();
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
