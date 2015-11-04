namespace Disruptor
{
    /// <summary>
    /// Coordinates claiming sequences for access to a data structure while tracking dependent <see cref="Sequence"/>s
    /// </summary>
    public interface ISequencer : ICursored, ISequenced
    {
        /// <summary>
        /// The capacity of the data structure to hold entries.                         
        /// </summary>
        /// <returns>the size of the RingBuffer.</returns>
        //int GetBufferSize { get; }

        /// <summary>
        /// Has the buffer got capacity to allocate another sequence.  This is a concurrent
        /// method so the response should only be taken as an indication of available capacity.
        /// @param requiredCapacity in the buffer
        /// @return true if the buffer has the capacity to allocate the next sequence otherwise false.          
        /// </summary>
        /// <param name="requiredCapacity">in the buffer</param>
        /// <returns>true if the buffer has the capacity to allocate the next sequence otherwise false.</returns>
        //bool HasAvailableCapacity(int requiredCapacity);

        /// <summary>
        /// Claim the next event in sequence for publishing.
        /// </summary>
        /// <returns>the claimed sequence value</returns>
        //long Next();

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
        //long Next(int n);

        /// <summary>
        /// Attempt to claim the next event in sequence for publishing.  Will return the
        /// number of the slot if there is at least <code>requiredCapacity</code> slots
        /// available. 
        /// </summary>
        /// <returns>the claimed sequence value</returns>
        /// <exception cref="InsufficientCapacityException"></exception>
        //long TryNext();

        /// <summary>
        /// Attempt to claim the next n events in sequence for publishing.  Will return the
        /// highest numbered slot if there is at least <code>requiredCapacity</code> slots
        /// available.  Have a look at {@link Sequencer#next()} for a description on how to
        /// use this method. 
        /// </summary>
        /// <param name="n">the number of sequences to claim</param>
        /// <returns> the claimed sequence value</returns>
        /// <exception cref="InsufficientCapacityException"></exception>
        //long TryNext(int n);

        /// <summary>
        /// Get the remaining capacity for this sequencer.
        /// </summary>
        /// <returns>The number of slots remaining.</returns>
        //long RemainingCapacity();

        /// <summary>
        /// Claim a specific sequence.  Only used if initialising the ring buffer to a specific value.
        /// </summary>
        /// <param name="sequence">The sequence to initialise too.</param>
        void Claim(long sequence);

        /// <summary>
        /// Publishes a sequence. Call when the event has been filled.
        /// </summary>
        /// <param name="sequence"></param>
        //void Publish(long sequence);

        /// <summary>
        /// Batch publish sequences.  Called when all of the events have been filled.
        /// </summary>
        /// <param name="lo"> first sequence number to publish</param>
        /// <param name="hi">last sequence number to publish</param>
        //void Publish(long lo, long hi);

        /// <summary>
        ///  Confirms if a sequence is published and the event is available for use; non-blocking.
        /// </summary>
        /// <param name="sequence"> of the buffer to check</param>
        /// <returns>true if the sequence is available for use, false if not</returns>
        bool IsAvailable(long sequence);

        /// <summary>
        /// Add the specified gating sequences to this instance of the Disruptor.  They will
        /// safely and atomically added to the list of gating sequences.
        /// </summary>
        /// <param name="gatingSequencess"> The sequences to add.</param>
        void AddGatingSequences(params Sequence[] gatingSequencess);

        /// <summary>
        /// Remove the specified sequence from this sequencer.
        /// </summary>
        /// <param name="sequence">to be removed.</param>
        /// <returns><tt>true</tt> if this sequence was found, <tt>false</tt> otherwise.</returns>
        bool RemoveGatingSequence(Sequence sequence);

        /// <summary>
        /// Create a new SequenceBarrier to be used by an EventProcessor to track which messages
        /// are available to be read from the ring buffer given a list of sequences to track.
        /// </summary>
        /// <param name="sequencesToTrack"></param>
        /// <returns>A sequence barrier that will track the specified sequences.</returns>
        ISequenceBarrier NewBarrier(params Sequence[] sequencesToTrack);

        /// <summary>
        /// Get the minimum sequence value from all of the gating sequences added to this ringBuffer.
        /// </summary>
        /// <returns> The minimum gating sequence or the cursor sequence if no sequences have been added.</returns>
        long GetMinimumSequence();

        /// <summary>
        ///  Get the highest sequence number that can be safely read from the ring buffer.  Depending
        /// on the implementation of the Sequencer this call may need to scan a number of values
        /// in the Sequencer.  The scan will range from nextSequence to availableSequence.  If
        ///there are no available values <code>&gt;= nextSequence</code> the return value will be
        /// <code>nextSequence - 1</code>.  To work correctly a consumer should pass a value that
        /// it 1 higher than the last sequence that was successfully processed.
        /// </summary>
        /// <param name="sequence">nextSequence The sequence to start scanning from.</param>
        /// <param name="availableSequence">availableSequence The sequence to scan to.</param>
        /// <returns>The highest value that can be safely read, will be at least <code>nextSequence - 1</code>.</returns>
        long GetHighestPublishedSequence(long sequence, long availableSequence);
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="gatingSequences"></param>
        /// <returns></returns>
        EventPoller<T> NewPoller<T>(IDataProvider<T> provider,params Sequence[] gatingSequences);
    }
}
