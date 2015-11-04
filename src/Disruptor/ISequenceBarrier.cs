namespace Disruptor
{
    public interface ISequenceBarrier
    {
        /// <summary>        
        /// Wait for the given sequence to be available for consumption.              
        /// </summary>
        /// <param name="sequence"></param>
        /// <returns></returns>
        long WaitFor(long sequence);

        /// <summary>         
        /// Get the current cursor value that can be read.        
        /// @return value of the cursor for entries that have been published.
        /// </summary>
        /// <returns></returns>
        long GetCursor { get; }

        /// <summary>
        ///
        /// The current alert status for the barrier.
        ///
        /// @return true if in alert otherwise false.
        /// </summary>
        /// <returns></returns>
        bool IsAlerted { get; }

        /// <summary>        
        /// Alert the {@link EventProcessor}s of a status change and stay in this status until cleared.
        /// </summary>
        void Alert();

        /// <summary>        
        /// Clear the current alert status.
        /// </summary>
        void ClearAlert();

        /// <summary>
        /// 
        /// Check if an alert has been raised and throw an {@link AlertException} if it has.
        ///
        /// @throws AlertException if alert has been raised.
        /// </summary>
        void CheckAlert();
    }
}
