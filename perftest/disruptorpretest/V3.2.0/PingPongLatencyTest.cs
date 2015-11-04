using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using Disruptor;
using NUnit.Framework;
using Disruptor.Collections;
using disruptorpretest.Support;

namespace disruptorpretest.V3._2._0
{
    /// <summary>
    /// 
    /// <pre>
    ///
    /// Ping pongs between 2 event handlers and measures the latency of
    /// a round trip.
    ///
    /// Queue Based:
    /// ============
    ///               +---take---+
    ///               |          |
    ///               |          V
    ///            +====+      +====+
    ///    +------>| Q1 |      | P2 |-------+
    ///    |       +====+      +====+       |
    ///   put                              put
    ///    |       +====+      +====+       |
    ///    +-------| P1 |      | Q2 |<------+
    ///            +====+      +====+
    ///               ^          |
    ///               |          |
    ///               +---take---+
    ///
    /// P1 - QueuePinger
    /// P2 - QueuePonger
    /// Q1 - PingQueue
    /// Q2 - PongQueue
    ///
    /// Disruptor:
    /// ==========
    ///               +----------+
    ///               |          |
    ///               |   get    V
    ///  waitFor   +=====+    +=====+  claim
    ///    +------>| SB2 |    | RB2 |<------+
    ///    |       +=====+    +=====+       |
    ///    |                                |
    /// +-----+    +=====+    +=====+    +-----+
    /// | EP1 |--->| RB1 |    | SB1 |<---| EP2 |
    /// +-----+    +=====+    +=====+    +-----+
    ///       claim   ^   get    |  waitFor
    ///               |          |
    ///               +----------+
    ///
    /// EP1 - Pinger
    /// EP2 - Ponger
    /// RB1 - PingBuffer
    /// SB1 - PingBarrier
    /// RB2 - PongBuffer
    /// SB2 - PongBarrier
    ///
    /// </pre>
    ///
    /// Note: <b>This test is only useful on a system using an invariant TSC in user space from the System.nanoTime() call.</b>
    ///
    /// </summary>
    [TestFixture]
    public class PingPongLatencyTest
    {
        private const int BUFFER_SIZE = 1024;
        private const long ITERATIONS = 1000L * 1000L * 30L;
        private const long PAUSE_NANOS = 1000L;

        private Histogram histogram; //= new Histogram(new long[]{10000000000L, 4});

        private RingBuffer<ValueEvent> pingBuffer = RingBuffer<ValueEvent>.CreateSingleProducer(() => new ValueEvent(), BUFFER_SIZE, new YieldingWaitStrategy());
        private RingBuffer<ValueEvent> pongBuffer = RingBuffer<ValueEvent>.CreateSingleProducer(() => new ValueEvent(), BUFFER_SIZE, new YieldingWaitStrategy());
        private ISequenceBarrier pongBarrier;
        private Pinger pinger;
        private BatchEventProcessor<ValueEvent> pingProcessor;
        private ISequenceBarrier pingBarrier;
        private Ponger ponger;
        BatchEventProcessor<ValueEvent> pongProcessor;

        public PingPongLatencyTest()
        {
            InitHistogram();
            pongBarrier = pongBuffer.NewBarrier();
            pinger = new Pinger(pingBuffer, ITERATIONS, PAUSE_NANOS);
            pingProcessor = new BatchEventProcessor<ValueEvent>(pongBuffer, pongBarrier, pinger);

            pingBarrier = pingBuffer.NewBarrier();
            ponger = new Ponger(pongBuffer);
            pongProcessor = new BatchEventProcessor<ValueEvent>(pingBuffer, pingBarrier, ponger);

            pingBuffer.AddGatingSequences(pongProcessor.Sequence);
            pongBuffer.AddGatingSequences(pingProcessor.Sequence);
        }

        [Test]
        public void RunDisruptorPassTest()
        {
            const int runs = 3;

            var queueMeanLatency = new decimal[runs];
            var disruptorMeanLatency = new decimal[runs];
            for (int i = 0; i < runs; i++)
            {
                //System.gc();
                histogram.Clear();

                RunDisruptorPass();
                Assert.True(histogram.Count >= ITERATIONS);
                disruptorMeanLatency[i] = histogram.Mean;


                Console.WriteLine("run {0} Disruptor {1}", i, histogram.ToString());
                DumpHistogram();
            }

            for (int i = 0; i < runs; i++)
            {
                //Assert.True( queueMeanLatency[i] > disruptorMeanLatency[i],"run: " + i,);
                Console.WriteLine("run {0},{1}", i, disruptorMeanLatency[i]);
            }
        }

        private void DumpHistogram()
        {
            for (int i = 0, size = histogram.Size; i < size; i++)
            {
                Console.WriteLine("{0} {1}, {2}", i, histogram.GetUpperBoundAt(i), histogram.GetCountAt(i));
            }
        }

        private void RunDisruptorPass()
        {
            var latch = new ManualResetEvent(false);
            var barrier = new Barrier(3);
            pinger.Reset(barrier, latch, histogram);
            ponger.Reset(barrier);

            System.Threading.Tasks.Task.Factory.StartNew(() => pongProcessor.Run());
            System.Threading.Tasks.Task.Factory.StartNew(() => pingProcessor.Run());

            barrier.SignalAndWait();
            latch.WaitOne();

            pingProcessor.Halt();
            pongProcessor.Halt();
        }

        private void InitHistogram()
        {

            if (histogram == null)
            {
                var intervals = new long[31];
                var intervalUpperBound = 1L;
                for (var i = 0; i < intervals.Length - 1; i++)
                {
                    intervalUpperBound *= 2;
                    intervals[i] = intervalUpperBound;
                }

                intervals[intervals.Length - 1] = long.MaxValue;
                histogram = new Histogram(intervals);
            }
        }
    }
}
