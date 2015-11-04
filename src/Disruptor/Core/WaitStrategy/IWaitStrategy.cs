namespace Disruptor
{
    /// <summary>
    /// Strategy employed for making {@link EventProcessor}s wait on a cursor {@link Sequence}.
    /// </summary>
    public interface IWaitStrategy
    {
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
        /// <exception cref="AlertException">if the status of the Disruptor has changed.</exception>
        /// <exception cref="System.Threading.ThreadInterruptedException">if the thread is interrupted.</exception>
        /// <exception cref="TimeoutException"></exception>
        /// <returns></returns>
        long WaitFor(long sequence, Sequence cursor, Sequence dependentSequence, ISequenceBarrier barrier);       
        /// <summary>
        ///  Implementations should signal the waiting {@link EventProcessor}s that the cursor has advanced.
        /// </summary>
        void SignalAllWhenBlocking();  
    }
}
