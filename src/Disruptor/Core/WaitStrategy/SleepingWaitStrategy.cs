using System.Threading;

namespace Disruptor
{
    /// <summary>
    /// Sleeping strategy that initially spins, then uses a Thread.yield(), and eventually for the minimum number of nanos
    /// the OS and JVM will allow while the {@link com.lmax.disruptor.EventProcessor}s are waiting on a barrier.
    ///
    /// This strategy is a good compromise between performance and CPU resource. Latency spikes can occur after quiet periods.
    /// </summary>
    public sealed class SleepingWaitStrategy : IWaitStrategy
    {
        private static readonly int RETRIES = 200;
        private SpinWait spinWait = default(SpinWait);

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
            int counter = RETRIES;

            while ((availableSequence = dependentSequence.Value) < sequence)
            {
                counter = ApplyWaitMethod(barrier, counter);
            }

            return availableSequence;
        }


        public void SignalAllWhenBlocking()
        {

        }

        private int ApplyWaitMethod(ISequenceBarrier barrier, int counter)
        {
            barrier.CheckAlert();

            if (counter > 100)
            {
                --counter;
            }
            else if (counter > 0)
            {
                --counter;
                Thread.Sleep(0);
            }
            else
            {              
                spinWait.SpinOnce();
                //LockSupport.parkNanos(1L);
            }

            return counter;
        }
    }
}
