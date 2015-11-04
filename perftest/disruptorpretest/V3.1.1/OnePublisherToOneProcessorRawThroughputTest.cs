using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Disruptor;
using Disruptor.Scheduler;
using System.Threading.Tasks;
using System.Diagnostics;

namespace disruptorpretest.V3._1._1
{
    public class OnePublisherToOneProcessorRawBatchThroughputTest : AbstractPerfTestQueueVsDisruptor
    {      
        private ISequencer sequencer;
        private MyRunnable myRunnable;

        public OnePublisherToOneProcessorRawBatchThroughputTest()
            : base(TestName, 1000L * 1000L * 100L)
        {
            sequencer = new SingleProducerSequencer(BUFFER_SIZE, new YieldingWaitStrategy());
            myRunnable = new MyRunnable(sequencer);
            sequencer.AddGatingSequences(myRunnable.GetSequence);
        }

        protected override long RunQueuePass()
        {
            throw new NotImplementedException();
        }

        protected override long RunDisruptorPass()
        {
            int batchSize = 10;
            var latch = new ManualResetEvent(false);
            long expectedCount = myRunnable.GetSequence.Value + (ITERATIONS * batchSize);
            myRunnable.Reset(latch, expectedCount);
            var _scheduler = new RoundRobinThreadAffinedTaskScheduler(4);
            Task.Factory.StartNew(myRunnable.Run, CancellationToken.None, TaskCreationOptions.None, _scheduler);
            var start = Stopwatch.StartNew();
            var spinwait = default(SpinWait);
            var sequencer = this.sequencer;

            for (long i = 0; i < ITERATIONS; i++)
            {
                long next = sequencer.Next(batchSize);
                sequencer.Publish((next - (batchSize - 1)), next);
            }

            latch.WaitOne();

            long opsPerSecond = (ITERATIONS * 1000L * batchSize) / start.ElapsedMilliseconds;
            WaitForEventProcessorSequence(expectedCount, spinwait,myRunnable);

            return opsPerSecond;
        }

        protected override void ShouldCompareDisruptorVsQueues()
        {
            throw new NotImplementedException();
        }       
    }

}
