/**
 * <pre>
 * UniCast a series of items between 1 publisher and 1 event processor.
 *
 * +----+    +-----+
 * | P1 |--->| EP1 |
 * +----+    +-----+
 *
 *
 * Queue Based:
 * ============
 *
 *        put      take
 * +----+    +====+    +-----+
 * | P1 |--->| Q1 |<---| EP1 |
 * +----+    +====+    +-----+
 *
 * P1  - Publisher 1
 * Q1  - Queue 1
 * EP1 - EventProcessor 1
 *
 *
 * Disruptor:
 * ==========
 *              track to prevent wrap
 *              +------------------+
 *              |                  |
 *              |                  v
 * +----+    +====+    +====+   +-----+
 * | P1 |--->| RB |<---| SB |   | EP1 |
 * +----+    +====+    +====+   +-----+
 *      claim      get    ^        |
 *                        |        |
 *                        +--------+
 *                          waitFor
 *
 * P1  - Publisher 1
 * RB  - RingBuffer
 * SB  - SequenceBarrier
 * EP1 - EventProcessor 1
 *
 * </pre>
 */



using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Disruptor;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Disruptor.Scheduler;

namespace disruptorpretest.V3._1._1
{
    /// <summary>
    /// 1个生产者1个消费者 
    /// </summary>
     //[TestFixture]
    public class OnePublisherToOneProcessorRawThroughputTest  : AbstractPerfTestQueueVsDisruptor
    {
        //private int BUFFER_SIZE = 1024 * 64;
        //private long ITERATIONS = 1000L * 1000L * 100L; //1 亿次
        private ISequencer sequencer;
        private _MyRunnable myRunnable;

        public OnePublisherToOneProcessorRawThroughputTest()
            : base(TestName, 1000L * 1000L * 100L)
        {
            sequencer = new SingleProducerSequencer(BUFFER_SIZE, new YieldingWaitStrategy());
            myRunnable = new _MyRunnable(sequencer);
            sequencer.AddGatingSequences(myRunnable.GetSequence);
        }

        //Queue
        protected override long RunQueuePass()
        {
            return 0L;
        }
              
        protected override long RunDisruptorPass()
        {

            var spinwait = default(SpinWait);
            var latch = new CountdownEvent(1);
            //var latch = new ManualResetEvent(false);
            var expectedCount = myRunnable.GetSequence.Value + ITERATIONS;
            myRunnable.Reset(latch, expectedCount);
            var _scheduler = new RoundRobinThreadAffinedTaskScheduler(4);
            //TaskScheduler.Default调度器 CPU 约在 50 % 两个繁忙，两个空闲
            //
            Task.Factory.StartNew(myRunnable.Run, CancellationToken.None, TaskCreationOptions.None, _scheduler);

            var start = Stopwatch.StartNew();

            var sequencer = this.sequencer;

            for (long i = 0; i < ITERATIONS; i++)
            {
                long next = sequencer.Next();
                sequencer.Publish(next);
            }

            latch.Wait();
            long opsPerSecond = (ITERATIONS * 1000L) / start.ElapsedMilliseconds;

            WaitForEventProcessorSequence(expectedCount, spinwait,myRunnable);

            return opsPerSecond;
        }
        //测试结果对比
        protected override void ShouldCompareDisruptorVsQueues()
        {
          
        }       
    }
}
