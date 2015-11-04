//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;
//using Disruptor;

//namespace disruptorpretest.V3._3._0.Offheap
//{
//    public class OneToOneOnHeapThroughputTest : AbstractPerfTestQueueVsDisruptor
//    {
//        private static readonly int BLOCK_SIZE = 256;
//        private static readonly int BUFFER_SIZE = 1024 * 1024;
//        private static readonly long ITERATIONS = 1000 * 1000 * 10L;
//        private readonly IWaitStrategy waitStrategy;
//        private readonly ByteBufferHandler handler;
//        private readonly RingBuffer<byte[]> buffer;
//        private readonly BatchEventProcessor<ByteBuffer> processor;
//        private readonly Random r = new Random(1);
//        private readonly byte[] data = new byte[BLOCK_SIZE];

//        public OneToOneOnHeapThroughputTest()
//            : base("dsiruptor", ITERATIONS)
//        {
//            handler = new ByteBufferHandler();
//            waitStrategy = new YieldingWaitStrategy();
//            buffer = RingBuffer<byte[]>.CreateSingleProducer(() => new byte[BLOCK_SIZE], BUFFER_SIZE, waitStrategy);

//            processor =
//            new BatchEventProcessor<ByteBuffer>(buffer, buffer.NewBarrier(), handler);

//            buffer.AddGatingSequences(processor.Sequence);

//            r.NextBytes(data);
//        }

//        protected override long RunQueuePass()
//        {
//            throw new NotImplementedException();
//        }

//        protected override long RunDisruptorPass()
//        {
//            byte[] data = this.data;

//            CountdownEvent latch = new CountdownEvent(1);
//            long expectedCount = processor.Sequence.Value + ITERATIONS;
//            handler.reset(latch, ITERATIONS);
//            Task.Factory.StartNew(() => processor.Run());

//            var start = Stopwatch.StartNew();

//            RingBuffer<ByteBuffer> rb = buffer;

//            for (long i = 0; i < ITERATIONS; i++)
//            {
//                long next = rb.Next();
//                var @event = rb.Get(next);
//                @event.clear();
//                @event.put(data);
//                @event.flip();
//                rb.Publish(next);
//            }

//            latch.Wait();
//            long opsPerSecond = (ITERATIONS * 1000L) / (start.ElapsedMilliseconds);
//            waitForEventProcessorSequence(expectedCount);
//            processor.Halt();

//            return opsPerSecond;
//        }

//        protected override void ShouldCompareDisruptorVsQueues()
//        {
//            throw new NotImplementedException();
//        }
//        private void waitForEventProcessorSequence(long expectedCount)
//        {
//            while (processor.Sequence.Value < expectedCount)
//            {
//                Thread.Sleep(0);                
//            }
//        }
//        public class ByteBufferHandler : IEventHandler<byte[]>
//        {
//            private long total = 0;
//            private long expectedCount;
//            private System.Threading.CountdownEvent latch = new CountdownEvent(1);


//            public void OnEvent(byte[] @event, long sequence, bool endOfBatch)
//            {
//                for (int i = 0; i < BLOCK_SIZE; i += 8)
//                {
//                    total += @event.Length;
//                }

//                if (--expectedCount == 0)
//                {
//                    latch.Signal();
//                }
//            }

//            public long getTotal()
//            {
//                return total;
//            }

//            public void reset(CountdownEvent latch, long expectedCount)
//            {
//                this.latch = latch;
//                this.expectedCount = expectedCount;
//            }
//        }
//    }
//}
