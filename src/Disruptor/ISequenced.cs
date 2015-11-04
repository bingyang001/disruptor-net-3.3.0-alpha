using System;

namespace Disruptor
{
    public interface ISequenced
    {
        /// <summary>
        /// The capacity of the data structure to hold entries.                         
        /// </summary>
        /// <returns>the size of the RingBuffer.</returns>
        int GetBufferSize { get; }

        /// <summary>
        /// Has the buffer got capacity to allocate another sequence.  This is a concurrent
        /// method so the response should only be taken as an indication of available capacity.
        /// @param requiredCapacity in the buffer
        /// @return true if the buffer has the capacity to allocate the next sequence otherwise false.          
        /// </summary>
        /// <param name="requiredCapacity">in the buffer</param>
        /// <returns>true if the buffer has the capacity to allocate the next sequence otherwise false.</returns>
        bool HasAvailableCapacity(int requiredCapacity);

        /// <summary>
        /// Get the remaining capacity for this sequencer.
        /// </summary>
        /// <returns>The number of slots remaining.</returns>
        long RemainingCapacity();

        /// <summary>
        /// Claim the next event in sequence for publishing.
        /// </summary>
        /// <returns>the claimed sequence value</returns>
        long Next();

        /// <summary>
        /// Claim the next n events in sequence for publishing.  This is for batch event producing.  Using batch producing
        /// requires a little care and some math.
        /// <pre>
        /// int n = 10;
        /// long hi = sequencer.next(n);
        /// long lo = hi - (n - 1);
        /// for (long sequence = lo; sequence &lt;= hi; sequence++) {
        ///     // Do work.
        /// }
        /// sequencer.publish(lo, hi);
        /// </pre> 
        /// </summary>
        /// <param name="n">the number of sequences to claim</param>
        /// <returns>the highest claimed sequence value</returns>
        long Next(int n);

        /// <summary>
        /// Attempt to claim the next event in sequence for publishing.  Will return the
        /// number of the slot if there is at least <code>requiredCapacity</code> slots
        /// available. 
        /// </summary>
        /// <returns>the claimed sequence value</returns>
        /// <exception cref="InsufficientCapacityException"></exception>
        long TryNext();

        /// <summary>
        /// Attempt to claim the next n events in sequence for publishing.  Will return the
        /// highest numbered slot if there is at least <code>requiredCapacity</code> slots
        /// available.  Have a look at {@link Sequencer#next()} for a description on how to
        /// use this method. 
        /// </summary>
        /// <param name="n">the number of sequences to claim</param>
        /// <returns> the claimed sequence value</returns>
        /// <exception cref="InsufficientCapacityException"></exception>
        long TryNext(int n);

        /// <summary>
        /// Publishes a sequence. Call when the event has been filled.
        /// </summary>
        /// <param name="sequence"></param>
        void Publish(long sequence);

        /// <summary>
        /// Batch publish sequences.  Called when all of the events have been filled.
        /// </summary>
        /// <param name="lo"> first sequence number to publish</param>
        /// <param name="hi">last sequence number to publish</param>
        void Publish(long lo, long hi);
    }
}
