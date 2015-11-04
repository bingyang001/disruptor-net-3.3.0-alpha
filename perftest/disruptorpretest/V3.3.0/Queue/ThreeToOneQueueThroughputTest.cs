/**
 * <pre>
 *
 * Sequence a series of events from multiple publishers going to one event processor.
 *
 * +----+
 * | P1 |------+
 * +----+      |
 *             v
 * +----+    +-----+
 * | P1 |--->| EP1 |
 * +----+    +-----+
 *             ^
 * +----+      |
 * | P3 |------+
 * +----+
 *
 *
 * Queue Based:
 * ============
 *
 * +----+  put
 * | P1 |------+
 * +----+      |
 *             v   take
 * +----+    +====+    +-----+
 * | P2 |--->| Q1 |<---| EP1 |
 * +----+    +====+    +-----+
 *             ^
 * +----+      |
 * | P3 |------+
 * +----+
 *
 * P1  - Publisher 1
 * P2  - Publisher 2
 * P3  - Publisher 3
 * Q1  - Queue 1
 * EP1 - EventProcessor 1
 *
 * </pre>
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;
using disruptorpretest.Support.V3._3._0;
using System.Threading.Tasks;
using System.Diagnostics;


namespace disruptorpretest.V3._3._0.Queue
{
    public class ThreeToOneQueueThroughputTest : AbstractPerfTestQueueVsDisruptor
    {
        private static readonly int NUM_PUBLISHERS = 3;
        private static readonly int BUFFER_SIZE = 1024 * 64;
        private static readonly long ITERATIONS = 1000L * 1000L * 20L;
        private readonly Barrier cyclicBarrier = new Barrier(NUM_PUBLISHERS + 1);// 3p+1c

        private readonly ConcurrentQueue<long> blockingQueue = new ConcurrentQueue<long>();
        //消费者
        private readonly ValueAdditionQueueProcessor queueProcessor;
        //生产者
        private readonly ValueQueuePublisher[] valueQueuePublishers = new ValueQueuePublisher[NUM_PUBLISHERS];
        public ThreeToOneQueueThroughputTest()
            : base(Test_Queue, ITERATIONS)
        {
            queueProcessor =
            new ValueAdditionQueueProcessor(blockingQueue, ((ITERATIONS / NUM_PUBLISHERS) * NUM_PUBLISHERS) - 1L);
            for (int i = 0; i < NUM_PUBLISHERS; i++)
            {
                valueQueuePublishers[i] = new ValueQueuePublisher(cyclicBarrier, blockingQueue, ITERATIONS / NUM_PUBLISHERS);
            }
        }
        protected override long RunQueuePass()
        {
            //信号零为0时，则解除阻塞
            CountdownEvent latch = new CountdownEvent(1);
            queueProcessor.reset(latch);
             var start = Stopwatch.StartNew();
            
           
            //3个生产者
            Task[] futures = new Task[NUM_PUBLISHERS];
            for (int i = 0; i < NUM_PUBLISHERS; i++)
            {
                futures[i] = Task.Factory.StartNew( () =>  valueQueuePublishers[i].run());              
            }
          
            CancellationTokenSource cancel = new CancellationTokenSource();
           
            
            //等待生产者完成
            Task.WaitAll(futures);
             //1个消费者
            var pTask= Task.Run(() => queueProcessor.run()/*, cancel.Token*/); 
            cyclicBarrier.SignalAndWait();
           
         
            pTask.Start();
           
            latch.Wait();

            long opsPerSecond = (ITERATIONS * 1000L) / (start.ElapsedMilliseconds);
            queueProcessor.halt();
            //cancel.Cancel();

            return opsPerSecond;
        }

        protected override long RunDisruptorPass()
        {
           return 0;
        }

        protected override void ShouldCompareDisruptorVsQueues()
        {
            throw new NotImplementedException();
        }
    }
}
