namespace Disruptor
{
    /// <summary>
    /// Busy Spin strategy that uses a busy spin loop for <see cref="IEventProcessor"/>s waiting on a barrier.
    ///
    /// This strategy will use CPU resource to avoid syscalls which can introduce latency jitter.  It is best
    /// used when threads can be bound to specific CPU cores.
    /// </summary>
    public sealed class BusySpinWaitStrategy : IWaitStrategy
    {
        #region IWaitStrategy 成员

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

            while ((availableSequence = dependentSequence.Value) < sequence)
            {
                barrier.CheckAlert();
            }

            return availableSequence;
        }

        public void SignalAllWhenBlocking()
        {

        }

        #endregion
    }
}
