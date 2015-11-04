/**
 * <pre>
 *
 * Ping pongs between 2 event handlers and measures the latency of
 * a round trip.
 *
 * Queue Based:
 * ============
 *               +---take---+
 *               |          |
 *               |          V
 *            +====+      +====+
 *    +------>| Q1 |      | P2 |-------+
 *    |       +====+      +====+       |
 *   put                              put
 *    |       +====+      +====+       |
 *    +-------| P1 |      | Q2 |<------+
 *            +====+      +====+
 *               ^          |
 *               |          |
 *               +---take---+
 *
 * P1 - QueuePinger
 * P2 - QueuePonger
 * Q1 - PingQueue
 * Q2 - PongQueue
 *
 * </pre>
 *
 * Note: <b>This test is only useful on a system using an invariant TSC in user space from the System.nanoTime() call.</b>
 */

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Linq;
using System.Text;
using Disruptor.Collections;

namespace disruptorpretest.V3._3._0.Queue
{
    public class PingPongQueueLatencyTest : AbstractPerfTestQueueVsDisruptor
    {
        private static readonly int BUFFER_SIZE = 1024;
        private static readonly long ITERATIONS = 1000L * 1000L * 30L;
        private static readonly long PAUSE_NANOS = 1000L;

        private readonly Histogram histogram = new Histogram(new long[] { 10000000000L, 4 });

        private readonly ConcurrentQueue<long> pingQueue = new ConcurrentQueue<long>();
        private readonly ConcurrentQueue<long> pongQueue = new ConcurrentQueue<long>();
        //private readonly QueuePinger qPinger;
        //private readonly QueuePonger qPonger;
        public PingPongQueueLatencyTest()
            : base(Test_Queue, ITERATIONS)
        {

        }
        protected override long RunQueuePass()
        {

            throw new NotImplementedException();
        }

        protected override long RunDisruptorPass()
        {
            throw new NotImplementedException();
        }

        protected override void ShouldCompareDisruptorVsQueues()
        {
            throw new NotImplementedException();
        }
    }
}
