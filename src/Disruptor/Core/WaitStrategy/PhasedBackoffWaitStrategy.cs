using System;
using System.Threading;
using System.Diagnostics;

namespace Disruptor
{
    /// <summary>
    ///  <p>Phased wait strategy for waiting <see cref="EventProcessor"/> s on a barrier.</p>
    ///
    /// <p>This strategy can be used when throughput and low-latency are not as important as CPU resource.
    /// Spins, then yields, then blocks on the configured BlockingStrategy.</p>
    /// </summary>
    public sealed class PhasedBackoffWaitStrategy : IWaitStrategy
    {
        private static readonly int SPIN_TRIES = 10000;
        private readonly TimeSpan spinTimeoutNanos;
        private readonly TimeSpan yieldTimeoutNanos;
        private readonly IBlockingStrategy lockingStrategy;

        private  PhasedBackoffWaitStrategy(TimeSpan spinTimeoutMillis,
                                     TimeSpan yieldTimeoutMillis,
                                     IBlockingStrategy lockingStrategy)
        {
            this.spinTimeoutNanos = spinTimeoutMillis;
            this.yieldTimeoutNanos = spinTimeoutNanos.Add(yieldTimeoutMillis);
            this.lockingStrategy = lockingStrategy;
        }
      
        public static PhasedBackoffWaitStrategy WithLock(TimeSpan spinTimeoutMillis,
                                                    TimeSpan yieldTimeoutMillis
                                                    )
        {
            return new PhasedBackoffWaitStrategy(spinTimeoutMillis, yieldTimeoutMillis,
                                              new LockBlockingStrategy());
        }

        public static PhasedBackoffWaitStrategy WithSleep(TimeSpan spinTimeoutMillis,
                                                     TimeSpan yieldTimeoutMillis
                                                     )
        {
            return new PhasedBackoffWaitStrategy(spinTimeoutMillis, yieldTimeoutMillis,
                                                  new SleepBlockingStrategy());
        }

        /// <summary>
        /// Wait for the given sequence to be available.  It is possible for this method to return a value
        /// less than the sequence number supplied depending on the implementation of the WaitStrategy.  A common
        /// use for this is to signal a timeout.  Any EventProcessor that is using a WaitStragegy to get notifications
        /// about message becoming available should remember to handle this case.  The {@link BatchEventProcessor} explicitly
        /// handles this case and will signal a timeout if required.
        /// </summary>
        /// <param name="sequence">to be waited on.</param>
        /// <param name="cursor">the main sequence from ringbuffer. Wait/notify strategies will need this as it's the only sequence that is also notified upon update.</param>
        /// <param name="dependentSequence">on which to wait.</param>
        /// <param name="barrier">the processor is waiting on.</param>
        /// <returns></returns>
        /// <exception cref="AlertException">if the status of the Disruptor has changed.</exception>
        /// <exception cref="System.Threading.ThreadInterruptedException">if the thread is interrupted.</exception>
        /// <exception cref="TimeoutException"></exception>
        /// Date:2013/8/27   
        /// Author:liguo
        public long WaitFor(long sequence, Sequence cursor, Sequence dependentSequence, ISequenceBarrier barrier)
        {
            long availableSequence;
            long startTime = 0;
            int counter = SPIN_TRIES;
            var stopWatch = Stopwatch.StartNew();
            do
            {
                if ((availableSequence = dependentSequence.Value) >= sequence)
                {
                    return availableSequence;
                }

                if (0 == --counter)
                {
                    if (0 == startTime)
                    {
                        startTime = stopWatch.ElapsedTicks;
                    }
                    else
                    {
                        var timeDelta = stopWatch.Elapsed;
                        if (timeDelta > yieldTimeoutNanos)
                        {
                            return lockingStrategy.WaitOnLock(sequence, cursor, dependentSequence, barrier, spinTimeoutNanos);
                        }
                        else if (timeDelta > spinTimeoutNanos)
                        {
                            Thread.Sleep(0);
                        }
                    }
                    counter = SPIN_TRIES;
                }
            }
            while (true);
        }


        public void SignalAllWhenBlocking()
        {
            lockingStrategy.SignalAllWhenBlocking();
        }

        public interface IBlockingStrategy
        {
            long WaitOnLock(long sequence,
                        Sequence cursorSequence,
                        Sequence dependentSequence, ISequenceBarrier barrier, TimeSpan timeOut);


            void SignalAllWhenBlocking();
        }

        private class LockBlockingStrategy : IBlockingStrategy
        {
            private volatile int _numWaiters = 0;
            private readonly object _gate = new object();

            public long WaitOnLock(long sequence,
                        Sequence cursorSequence,
                        Sequence dependentSequence, ISequenceBarrier barrier,TimeSpan timeOut)
            {
                var availableSequence = cursorSequence.Value; // volatile read
                if (availableSequence < sequence)
                {
                    Monitor.Enter(_gate);
                    try
                    {
                        ++_numWaiters;
                        while ((availableSequence = cursorSequence.Value) < sequence) // volatile read
                        {
                            barrier.CheckAlert();
                            Monitor.Wait(_gate, timeOut);
                        }
                    }
                    finally
                    {
                        --_numWaiters;
                        Monitor.Exit(_gate);
                    }
                }
                while ((availableSequence = dependentSequence.Value) < sequence)
                {
                    barrier.CheckAlert();
                }
                return availableSequence;
            }

            public void SignalAllWhenBlocking()
            {
                if (0 != _numWaiters)
                    lock (_gate)
                        Monitor.PulseAll(_gate);
            }
        }

        private class SleepBlockingStrategy : IBlockingStrategy
        {
            public long WaitOnLock(long sequence,
                        Sequence cursorSequence,
                        Sequence dependentSequence, ISequenceBarrier barrier, TimeSpan timeOut)
            {
                long availableSequence;
                var spinWait = default(SpinWait);

                while ((availableSequence = dependentSequence.Value) < sequence)
                {
                    spinWait.SpinOnce();
                }

                return availableSequence;
            }

            public void SignalAllWhenBlocking()
            {

            }

        }
    }
}
