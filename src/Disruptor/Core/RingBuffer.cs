using System;
using Disruptor.Dsl;

namespace Disruptor
{
    /// <summary>
    /// Ring based store of reusable entries containing the data representing
    /// an event being exchanged between event producer and {@link EventProcessor}s.
    /// </summary>
    /// <typeparam name="T">implementation storing the data for sharing during exchange or parallel coordination of an event.</typeparam>
    public sealed class RingBuffer<T> :
                                        ICursored/*, IDataProvider<T>*/
                                        , IEventSequencer<T>
                                        , IEventSink<T> where T : class
    {
        // public static readonly long INITIAL_CURSOR_VALUE = InitialCursorValue.INITIAL_CURSOR_VALUE;
        private static readonly int BUFFER_PAD = 128 / 4;
        private static readonly int REF_ELEMENT_SHIFT = 2;
        private static readonly long REF_ARRAY_BASE = 140;
        private readonly long indexMask; //2015.07.19
        private readonly int bufferSize;
        private readonly ISequencer sequencer;
        private readonly T[] entries;

        /// <summary>
        /// Construct a RingBuffer with the full option set.
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        /// <param name="eventFactory">Implementations should instantiate an event object, with all memory already allocated where possible.</param>
        /// <param name="sequencer">sequencer to handle the ordering of events moving through the RingBuffer.</param>
        private RingBuffer(Func<T> eventFactory,
                ISequencer sequencer)
        {
            this.sequencer = sequencer;
            this.bufferSize = sequencer.GetBufferSize;
            this.VerifyBufferSize();
            this.indexMask = bufferSize - 1;
            this.entries = new T[sequencer.GetBufferSize /*+ 2 * BUFFER_PAD*/];
            this.FillInstance(eventFactory);
        }

        /// <summary>
        /// Create a new multiple producer RingBuffer with the specified wait strategy.
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        /// <param name="factory">used to Create the events within the ring buffer.</param>
        /// <param name="bufferSize">number of elements to Create within the ring buffer.</param>
        /// <param name="waitStrategy">used to determine how to wait for new elements to become available</param>
        /// <returns></returns>
        public static RingBuffer<T> CreateMultiProducer(Func<T> factory,
                                                       int bufferSize,
                                                       IWaitStrategy waitStrategy)
        {
            var sequencer = new MultiProducerSequencer(bufferSize, waitStrategy);

            return new RingBuffer<T>(factory, sequencer);
        }

        /// <summary>
        /// Create a new multiple producer RingBuffer using the default wait strategy  {@link BlockingWaitStrategy}.
        /// </summary>
        /// <param name="factory">used to Create the events within the ring buffer</param>
        /// <param name="bufferSize">number of elements to Create within the ring buffer.</param>
        /// <returns></returns>
        public static RingBuffer<T> CreateMultiProducer(Func<T> factory, int bufferSize)
        {
            return CreateMultiProducer(factory, bufferSize, new BlockingWaitStrategy());
        }

        /// <summary>
        /// Create a new single producer RingBuffer with the specified wait strategy.
        /// </summary>
        /// <param name="factory">used to Create the events within the ring buffer</param>
        /// <param name="bufferSize">number of elements to Create within the ring buffer</param>
        /// <param name="waitStrategy">used to determine how to wait for new elements to become available.</param>
        /// <returns></returns>
        public static RingBuffer<T> CreateSingleProducer(Func<T> factory,
                                                         int bufferSize,
                                                         IWaitStrategy waitStrategy)
        {
            var sequencer = new SingleProducerSequencer(bufferSize, waitStrategy);

            return new RingBuffer<T>(factory, sequencer);
        }

        /// <summary>
        /// Create a new single producer RingBuffer using the default wait strategy  {@link BlockingWaitStrategy}.
        /// </summary>
        /// <param name="factory">used to Create the events within the ring buffer</param>
        /// <param name="bufferSize">number of elements to Create within the ring buffer.</param>
        /// <returns></returns>
        public static RingBuffer<T> CreateSingleProducer(Func<T> factory, int bufferSize)
        {
            return CreateSingleProducer(factory, bufferSize, new BlockingWaitStrategy());
        }

        /// <summary>
        /// Create a new Ring Buffer with the specified producer type (SINGLE or MULTI)
        /// </summary>
        /// <param name="producerType">producer type to use {@link ProducerType}.</param>
        /// <param name="factory">used to Create events within the ring buffer.</param>
        /// <param name="bufferSize"> number of elements to Create within the ring buffer.</param>
        /// <param name="waitStrategy">used to determine how to wait for new elements to become available.</param>
        /// <exception cref="ArgumentException">if bufferSize is less than 1 or not a power of 2</exception>
        /// <returns></returns>
        public static RingBuffer<T> Create(ProducerType producerType,
                                          Func<T> factory,
                                          int bufferSize,
                                          IWaitStrategy waitStrategy)
        {
            switch (producerType)
            {
                case ProducerType.SINGLE:
                    return CreateSingleProducer(factory, bufferSize, waitStrategy);
                case ProducerType.MULTI:
                    return CreateMultiProducer(factory, bufferSize, waitStrategy);
                default:
                    throw new ArgumentException(producerType.ToString());
            }
        }

        /// <summary>
        ///  <p>Get the event for a given sequence in the RingBuffer.</p>
        ///
        /// <p>This call has 2 uses.  Firstly use this call when publishing to a ring buffer.
        /// After calling {@link RingBuffer#Next()} use this call to get hold of the
        /// preallocated event to fill with data before calling {@link RingBuffer#Publish(long)}.</p>
        ///
        /// <p>Secondly use this call when consuming data from the ring buffer.  After calling
        /// {@link SequenceBarrier#WaitFor(long)} call this method with any value greater than
        /// that your current consumer sequence and less than or equal to the value returned from
        /// the {@link SequenceBarrier#WaitFor(long)} method.</p>
        /// </summary>
        /// <param name="sequence">sequence for the event</param>
        /// <returns>the event for the given sequence</returns>
        public T this[long sequence]
        {
            //get { return (T)entries[REF_ARRAY_BASE + ((sequence & indexMask) << REF_ELEMENT_SHIFT)]; }
            get { return (T)entries[sequence & indexMask]; }
        }

        public T Get(long sequence)
        {
            //return (T)entries[sequence & indexMask];
            return this[sequence];
        }

        [Obsolete]
        public T GetPreallocated(long sequence)
        {
            return this[sequence];
        }

        [Obsolete]
        public T GetPublished(long sequence)
        {
            return this[sequence];
        }

        /// <summary>
        /// Increment and return the next sequence for the ring buffer.  Calls of this
        /// method should ensure that they always publish the sequence afterward.  E.g.       
        /// <p>
        /// long sequence = ringBuffer.next();
        /// try {
        ///     Event e = ringBuffer.get(sequence);
        ///     // Do some work with the event.
        /// } finally {
        ///     ringBuffer.publish(sequence);
        /// }
        /// </p>       
        /// <see cref="RingBuffer#publish(long)"/>
        /// <see cref="RingBuffer#get(long)"/>
        /// </summary>             
        /// <returns>The next sequence to publish to.</returns>
        public long Next()
        {
            return sequencer.Next();
        }

        /// <summary>
        /// The same functionality as {@link RingBuffer#next()}, but allows the caller to claim the next n sequences.
        /// <see cref="RingBuffer.next()"/>
        /// </summary>
        /// <param name="n">number of slots to claim</param>
        /// <returns>sequence number of the highest slot claimed</returns>
        public long Next(int n)
        {
            return sequencer.Next(n);
        }

        /// <summary>
        ///  
        /// <p>Increment and return the next sequence for the ring buffer.  Calls of this
        /// method should ensure that they always publish the sequence afterward.  E.g.
        /// </p>
        /// <pre>
        /// long sequence = ringBuffer.next();
        /// try {
        ///     Event e = ringBuffer.get(sequence);
        ///     // Do some work with the event.
        /// } finally {
        ///     ringBuffer.publish(sequence);
        /// }
        /// </pre>
        /// <p>This method will not block if there is not space available in the ring
        /// buffer, instead it will throw an {@link InsufficientCapacityException}.
        /// </p>      
        /// </summary>
        /// <exception cref="InsufficientCapacityException">if the necessary space in the ring buffer is not available</exception>      
        /// <returns>The next sequence to publish to.</returns>       
        public long TryNext()
        {
            return sequencer.TryNext();
        }

        /// <summary>
        /// The same functionality as {@link RingBuffer#tryNext()}, but allows the caller to attempt
        /// to claim the next n sequences.
        /// </summary>
        /// <param name="n">number of slots to claim</param>
        /// <exception cref="InsufficientCapacityException">if the necessary space in the ring buffer is not available</exception>
        /// <returns>sequence number of the highest slot claimed</returns>
        public long TryNext(int n)
        {
            return sequencer.TryNext(n);
        }

        /// <summary>
        /// Resets the cursor to a specific value.  This can be applied at any time, but it is worth not
        /// that it is a racy thing to do and should only be used in controlled circumstances.  E.g. during
        /// initialisation. 
        /// </summary>
        /// <param name="sequence">The sequence to reset too.</param>
        /// <exception cref="IllegalStateException">If any gating sequences have already been specified.</exception>
        public void ResetTo(long sequence)
        {
            sequencer.Claim(sequence);
            sequencer.Publish(sequence);
        }

        /// <summary>
        /// Sets the cursor to a specific sequence and returns the preallocated entry that is stored there.  This
        /// is another deliberately racy call, that should only be done in controlled circumstances, e.g. initialisation. 
        /// </summary>
        /// <param name="sequence">The sequence to claim.</param>
        /// <returns>The preallocated event.</returns>
        public T ClaimAndGetPreallocated(long sequence)
        {
            sequencer.Claim(sequence);
            return this[sequence];
        }

        /// <summary>
        ///  Determines if a particular entry has been published.
        /// </summary>
        /// <param name="sequence">The sequence to identify the entry.</param>
        /// <returns>If the value has been published or not.</returns>
        public bool IsPublished(long sequence)
        {
            return sequencer.IsAvailable(sequence);
        }

        /// <summary>        
        /// Add the specified gating sequences to this instance of the Disruptor. 
        /// They will safely and atomically added to the list of gating sequences.        
        /// </summary>
        /// <param name="gatingSequences"> The sequences to add.</param>
        public void AddGatingSequences(params Sequence[] gatingSequences)
        {
            sequencer.AddGatingSequences(gatingSequences);
        }

        /// <summary>
        ///  Get the minimum sequence value from all of the gating sequences
        ///  added to this ringBuffer.
        /// </summary>
        ///<returns>The minimum gating sequence or the cursor sequence if
        ///no sequences have been added.
        /// </returns>
        public long GetMinimumGatingSequence()
        {
            return sequencer.GetMinimumSequence();
        }

        /// <summary>
        /// Remove the specified sequence from this ringBuffer.
        /// </summary>
        /// <param name="sequence">to be removed.</param>
        /// <returns><tt>true</tt> if this sequence was found, <tt>false</tt> otherwise.</returns>
        public bool RemoveGatingSequence(Sequence sequence)
        {
            return sequencer.RemoveGatingSequence(sequence);
        }

        /// <summary>
        /// Create a  new <see cref="ISequenceBarrier"/> to be used by an EventProcessor to track which messages
        /// are available to be read from the ring buffer given a list of sequences to track.        
        /// </summary>
        /// <param name="sequencesToTrack">the additional sequences to track</param>
        /// <returns>A sequence barrier that will track the specified sequences.</returns>
        public ISequenceBarrier NewBarrier(params Sequence[] sequencesToTrack)
        {
            return sequencer.NewBarrier(sequencesToTrack);
        }

        /// <summary>
        /// Creates an event poller for this ring buffer gated on the supplied sequences.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="gatingSequences">gatingSequences</param>
        /// <returns>A poller that will gate on this ring buffer and the supplied sequences.</returns>
        public EventPoller<T> NewPoller(params Sequence[] gatingSequences)
        {
            return sequencer.NewPoller(this, gatingSequences);
        }

        /// <summary>
        /// Get the current cursor value for the ring buffer.  The cursor value is
        /// the last value that was published, or the highest available sequence
        /// that can be consumed. 
        /// </summary>
        public long GetCursor
        {
            get { return sequencer.GetCursor; }
        }

        /// <summary>
        /// The size of the buffer.
        /// </summary>
        public int GetBufferSize
        {
            get { return bufferSize; }
        }

        /// <summary>
        /// Given specified <tt>requiredCapacity</tt> determines if that amount of space
        /// is available.  Note, you can not assume that if this method returns <tt>true</tt>
        /// that a call to {@link RingBuffer#next()} will not block.  Especially true if this
        /// ring buffer is set up to handle multiple producers. 
        /// </summary>
        /// <param name="requiredCapacity">The capacity to check for.</param>
        /// <returns> <tt>true</tt> If the specified <tt>requiredCapacity</tt> is available
        ///           <tt>false</tt> if now.
        ///</returns>
        public bool HasAvailableCapacity(int requiredCapacity)
        {
            return sequencer.HasAvailableCapacity(requiredCapacity);
        }

        /// <summary>
        /// Publishes an event to the ring buffer.  It handles
        /// claiming the next sequence, getting the current (uninitialised)
        /// event from the ring buffer and publishing the claimed sequence
        /// after translation. 
        /// </summary>
        /// <param name="translator">The user specified translation for the event</param>
        public void PublishEvent(IEventTranslator<T> translator)
        {
            var sequence = sequencer.Next();
            TranslateAndPublish(translator, sequence);
        }

        /// <summary>
        /// Attempts to publish an event to the ring buffer.  It handles
        /// claiming the next sequence, getting the current (uninitialised)
        /// event from the ring buffer and publishing the claimed sequence
        /// after translation.  Will return false if specified capacity
        /// was not available. 
        /// </summary>
        /// <param name="translator">The user specified translation for the event</param>
        /// <returns>true if the value was published, false if there was insufficient capacity.</returns>
        public bool TryPublishEvent(IEventTranslator<T> translator)
        {
            try
            {
                long sequence = sequencer.TryNext();
                TranslateAndPublish(translator, sequence);
                return true;
            }
            catch (InsufficientCapacityException e)
            {
                return false;
            }
        }

        /// <summary>
        ///  Allows one user supplied argument. <see cref="publishEvent(EventTranslator)"/>
        /// </summary>
        /// <typeparam name="A"></typeparam>
        /// <param name="translator">The user specified translation for the event</param>
        /// <param name="arg0"> A user supplied argument.</param>
        public void PublishEvent<A>(IEventTranslatorOneArg<T, A> translator, A arg0)
        {
            long sequence = sequencer.Next();
            TranslateAndPublish(translator, sequence, arg0);
        }

        //net 风格
        //2013.8.15 add lg

        public void PublishEvent(Action<T, long> translatorAction)
        {
            var sequence = sequencer.Next();
            TranslateAndPublish(translatorAction, sequence);
        }

        public void PublishEventOneArg<A>(Action<T, long, A> action, A arg0)
        {
            var sequence = sequencer.Next();
            InternalPublish(action, sequence, arg0);
        }

        public void PublishEventTwoArg<A, B>(Action<T, long, A, B> action, A arg0, B arg1)
        {
            var sequence = sequencer.Next();
            InternalPublish(action, sequence, arg0, arg1);
        }

        public void PublishEventThreeArg<A, B, C>(Action<T, long, A, B, C> action, A arg0, B arg1, C arg2)
        {
            var sequence = sequencer.Next();
            InternalPublish(action, sequence, arg0, arg1, arg2);
        }

        public void PublishEventMultiArg<A>(EventTranslatorVarargTranslateTo<T, A> action, params A[] arg)
        {
            var sequence = sequencer.Next();
            InternalPublish(action, sequence, arg);
        }

        // end 
        //______________________________________________________

        /// <summary>
        ///  Allows one user supplied argument.
        /// </summary>
        /// <typeparam name="A"></typeparam>
        /// <param name="translator">The user specified translation for the event</param>
        /// <param name="arg0">A user supplied argument.</param>
        /// <returns>true if the value was published, false if there was insufficient capacity.</returns>
        public bool TryPublishEvent<A>(IEventTranslatorOneArg<T, A> translator, A arg0)
        {
            try
            {
                long sequence = sequencer.TryNext();
                TranslateAndPublish(translator, sequence, arg0);
                return true;
            }
            catch (InsufficientCapacityException e)
            {
                return false;
            }
        }

        /// <summary>
        /// Allows two user supplied arguments.<see cref="publishEvent(EventTranslator)"/>
        /// </summary>
        /// <typeparam name="A"></typeparam>
        /// <typeparam name="B"></typeparam>
        /// <param name="translator">The user specified translation for the event</param>
        /// <param name="arg0"> A user supplied argument.</param>
        /// <param name="arg1"> A user supplied argument.</param>
        public void PublishEvent<A, B>(IEventTranslatorTwoArg<T, A, B> translator, A arg0, B arg1)
        {
            long sequence = sequencer.Next();
            TranslateAndPublish(translator, sequence, arg0, arg1);
        }

        /// <summary>
        /// Allows two user supplied arguments.<see cref="tryPublishEvent(EventTranslator)"/>
        /// </summary>
        /// <typeparam name="A"></typeparam>
        /// <typeparam name="B"></typeparam>
        /// <param name="translator">The user specified translation for the event</param>
        /// <param name="arg0">A user supplied argument.</param>
        /// <param name="arg1">A user supplied argument.</param>
        /// <returns>true if the value was published, false if there was insufficient capacity.</returns>
        public bool TryPublishEvent<A, B>(IEventTranslatorTwoArg<T, A, B> translator, A arg0, B arg1)
        {
            try
            {
                long sequence = sequencer.TryNext();
                TranslateAndPublish(translator, sequence, arg0, arg1);
                return true;
            }
            catch (InsufficientCapacityException e)
            {
                return false;
            }
        }

        /// <summary>
        /// Allows three user supplied arguments. <see cref="publishEvent(EventTranslator)"/>
        /// </summary>
        /// <typeparam name="A"></typeparam>
        /// <typeparam name="B"></typeparam>
        /// <typeparam name="C"></typeparam>
        /// <param name="translator">The user specified translation for the event</param>
        /// <param name="arg0">A user supplied argument.</param>
        /// <param name="arg1">A user supplied argument.</param>
        /// <param name="arg2">A user supplied argument.</param>
        public void PublishEvent<A, B, C>(IEventTranslatorThreeArg<T, A, B, C> translator, A arg0, B arg1, C arg2)
        {
            long sequence = sequencer.Next();
            TranslateAndPublish(translator, sequence, arg0, arg1, arg2);
        }

        /// <summary>
        /// Allows three user supplied arguments
        /// </summary>
        /// <typeparam name="A"></typeparam>
        /// <typeparam name="B"></typeparam>
        /// <typeparam name="C"></typeparam>
        /// <param name="translator">The user specified translation for the event</param>
        /// <param name="arg0">A user supplied argument.</param>
        /// <param name="arg1">A user supplied argument.</param>
        /// <param name="arg2">A user supplied argument.</param>
        /// <returns>true if the value was published, false if there was insufficient capacity.</returns>
        public bool TryPublishEvent<A, B, C>(IEventTranslatorThreeArg<T, A, B, C> translator, A arg0, B arg1, C arg2)
        {
            try
            {
                long sequence = sequencer.TryNext();
                TranslateAndPublish(translator, sequence, arg0, arg1, arg2);
                return true;
            }
            catch (InsufficientCapacityException e)
            {
                return false;
            }
        }

        /// <summary>
        ///  Allows a variable number of user supplied arguments。
        /// </summary>       
        /// <param name="translator">The user specified translation for the event</param>
        /// <param name="args">args User supplied arguments.</param>       
        public void PublishEvent(IEventTranslatorVararg<T> translator, params Object[][] args)
        {
            long sequence = sequencer.Next();
            TranslateAndPublish(translator, sequence, args);
        }

        /// <summary>
        /// Allows a variable number of user supplied arguments。
        /// </summary>
        /// <typeparam name="A"></typeparam>
        /// <param name="translator">The user specified translation for the event.</param>
        /// <param name="args">args User supplied arguments.</param>
        /// <returns>true if the value was published, false if there was insufficient  capacity.</returns>       
        public bool TryPublishEvent(IEventTranslatorVararg<T> translator, params Object[][] args)
        {
            try
            {
                long sequence = sequencer.TryNext();
                TranslateAndPublish(translator, sequence, args);
                return true;
            }
            catch (InsufficientCapacityException e)
            {
                return false;
            }
        }

        /// <summary>
        /// Publishes multiple events to the ring buffer.  It handles
        /// claiming the next sequence, getting the current (uninitialised)
        /// event from the ring buffer and publishing the claimed sequence
        /// after translation. 
        /// </summary>
        /// <param name="translators">The user specified translation for each event</param>
        public void PublishEvents(IEventTranslator<T>[] translators)
        {
            PublishEvents(translators, 0, translators.Length);
        }

        /// <summary>
        /// Publishes multiple events to the ring buffer.  It handles
        /// claiming the next sequence, getting the current (uninitialised)
        /// event from the ring buffer and publishing the claimed sequence
        /// after translation. 
        /// </summary>
        /// <param name="translators">The user specified translation for each event.</param>
        /// <param name="batchStartsAt">The first element of the array which is within the batch.</param>
        /// <param name="batchSize">The actual size of the batch.</param>
        public void PublishEvents(IEventTranslator<T>[] translators, int batchStartsAt, int batchSize)
        {
            CheckBounds(translators, batchStartsAt, batchSize);
            var finalSequence = sequencer.Next(batchSize);
            TranslateAndPublishBatch(translators, batchStartsAt, batchSize, finalSequence);
        }

        /// <summary>
        /// Attempts to publish multiple events to the ring buffer.  It handles
        /// claiming the next sequence, getting the current (uninitialised)
        /// event from the ring buffer and publishing the claimed sequence
        /// after translation.  Will return false if specified capacity
        /// was not available. 
        /// </summary>
        /// <param name="translators">The user specified translation for the event</param>
        /// <returns>true if the value was published, false if there was insufficient capacity.</returns>
        public bool TryPublishEvents(IEventTranslator<T>[] translators)
        {
            return TryPublishEvents(translators, 0, translators.Length);
        }

        /// <summary>
        /// Attempts to publish multiple events to the ring buffer.  It handles
        /// claiming the next sequence, getting the current (uninitialised)
        /// event from the ring buffer and publishing the claimed sequence
        /// after translation.  Will return false if specified capacity
        /// was not available.
        /// </summary>
        /// <param name="translators">The user specified translation for the event</param>
        /// <param name="batchStartsAt">The first element of the array which is within the batch.</param>
        /// <param name="batchSize">The actual size of the batch</param>
        /// <returns>true if all the values were published, false if there was insufficient capacity.</returns>
        public bool TryPublishEvents(IEventTranslator<T>[] translators, int batchStartsAt, int batchSize)
        {
            CheckBounds(translators, batchStartsAt, batchSize);
            try
            {
                long finalSequence = sequencer.TryNext(batchSize);
                TranslateAndPublishBatch(translators, batchStartsAt, batchSize, finalSequence);
                return true;
            }
            catch (InsufficientCapacityException e)
            {
                return false;
            }
        }

        /// <summary>
        /// Allows one user supplied argument per event.<see cref="PublishEvents(EventTranslator[])"/>
        /// </summary>
        /// <typeparam name="A"></typeparam>
        /// <param name="translator">The user specified translation for the event</param>
        /// <param name="arg0"> A user supplied argument.</param>
        public void PublishEvents<A>(IEventTranslatorOneArg<T, A> translator, A[] arg0)
        {
            PublishEvents(translator, 0, arg0.Length, arg0);
        }

        /// <summary>
        /// Allows one user supplied argument per event.<seealso cref="PublishEvents(EventTranslator[])"/>
        /// </summary>
        /// <typeparam name="A"></typeparam>
        /// <param name="translator">The user specified translation for each event</param>
        /// <param name="batchStartsAt">The first element of the array which is within the batch.</param>
        /// <param name="batchSize"> The actual size of the batch</param>
        /// <param name="arg0">An array of user supplied arguments, one element per event.</param>
        public void PublishEvents<A>(IEventTranslatorOneArg<T, A> translator, int batchStartsAt, int batchSize, A[] arg0)
        {
            CheckBounds(arg0, batchStartsAt, batchSize);
            long finalSequence = sequencer.Next(batchSize);
            TranslateAndPublishBatch(translator, arg0, batchStartsAt, batchSize, finalSequence);
        }

        /// <summary>
        ///  Allows one user supplied argument.<seealso cref="TryPublishEvents(EventTranslator[])"/>
        /// </summary>
        /// <typeparam name="A"></typeparam>
        /// <param name="translator">The user specified translation for each event</param>
        /// <param name="arg0"> An array of user supplied arguments, one element per event.</param>
        /// <returns>true if the value was published, false if there was insufficient capacity.</returns>
        public bool TryPublishEvents<A>(IEventTranslatorOneArg<T, A> translator, A[] arg0)
        {
            return TryPublishEvents(translator, 0, arg0.Length, arg0);
        }

        /// <summary>
        /// Allows one user supplied argument.  <see cref="TryPublishEvents(EventTranslator[])"/>
        /// </summary>
        /// <typeparam name="A"></typeparam>
        /// <param name="translator">The user specified translation for each event</param>
        /// <param name="batchStartsAt">The first element of the array which is within the batch.</param>
        /// <param name="batchSize">The actual size of the batch</param>
        /// <param name="arg0">An array of user supplied arguments, one element per event.</param>
        /// <returns>true if the value was published, false if there was insufficient capacity.</returns>
        public bool TryPublishEvents<A>(IEventTranslatorOneArg<T, A> translator, int batchStartsAt, int batchSize, A[] arg0)
        {
            CheckBounds(arg0, batchStartsAt, batchSize);
            try
            {
                long finalSequence = sequencer.TryNext(batchSize);
                TranslateAndPublishBatch(translator, arg0, batchStartsAt, batchSize, finalSequence);
                return true;
            }
            catch (InsufficientCapacityException e)
            {
                return false;
            }
        }

        /// <summary>
        /// Allows two user supplied arguments per event.<seealso cref="publishEvents(EventTranslator[])"/>
        /// </summary>
        /// <typeparam name="A"></typeparam>
        /// <typeparam name="B"></typeparam>
        /// <param name="translator">The user specified translation for the event</param>
        /// <param name="arg0">An array of user supplied arguments, one element per event.</param>
        /// <param name="arg1"> An array of user supplied arguments, one element per event.</param>
        public void PublishEvents<A, B>(IEventTranslatorTwoArg<T, A, B> translator, A[] arg0, B[] arg1)
        {
            PublishEvents(translator, 0, arg0.Length, arg0, arg1);
        }

        /// <summary>
        /// Allows two user supplied arguments per event. <see cref="publishEvents(EventTranslator[])"/>
        /// </summary>
        /// <typeparam name="A"></typeparam>
        /// <typeparam name="B"></typeparam>
        /// <param name="translator">The user specified translation for the event</param>
        /// <param name="batchStartsAt">The first element of the array which is within the batch.</param>
        /// <param name="batchSize"> The actual size of the batch.</param>
        /// <param name="arg0">An array of user supplied arguments, one element per event.</param>
        /// <param name="arg1">An array of user supplied arguments, one element per event.</param>
        public void PublishEvents<A, B>(IEventTranslatorTwoArg<T, A, B> translator, int batchStartsAt, int batchSize, A[] arg0, B[] arg1)
        {
            CheckBounds(arg0, arg1, batchStartsAt, batchSize);
            long finalSequence = sequencer.Next(batchSize);
            TranslateAndPublishBatch(translator, arg0, arg1, batchStartsAt, batchSize, finalSequence);
        }

        /// <summary>
        /// Allows two user supplied arguments per event. <see cref="tryPublishEvents(EventTranslator[])"/>
        /// </summary>
        /// <typeparam name="A"></typeparam>
        /// <typeparam name="B"></typeparam>
        /// <param name="translator">The user specified translation for the event</param>
        /// <param name="arg0">An array of user supplied arguments, one element per event.</param>
        /// <param name="arg1">An array of user supplied arguments, one element per event.</param>
        /// <returns>true if the value was published, false if there was insufficient capacity.</returns>
        public bool TryPublishEvents<A, B>(IEventTranslatorTwoArg<T, A, B> translator, A[] arg0, B[] arg1)
        {
            return TryPublishEvents(translator, 0, arg0.Length, arg0, arg1);
        }

        /// <summary>
        /// Allows two user supplied arguments per event.<see cref="tryPublishEvents(EventTranslator[])"/>
        /// </summary>
        /// <typeparam name="A"></typeparam>
        /// <typeparam name="B"></typeparam>
        /// <param name="translator"> The user specified translation for the event</param>
        /// <param name="batchStartsAt">The first element of the array which is within the batch.</param>
        /// <param name="batchSize">The actual size of the batch.</param>
        /// <param name="arg0"> An array of user supplied arguments, one element per event.</param>
        /// <param name="arg1"> An array of user supplied arguments, one element per event.</param>
        /// <returns>true if the value was published, false if there was insufficient capacity.</returns>
        public bool TryPublishEvents<A, B>(IEventTranslatorTwoArg<T, A, B> translator, int batchStartsAt, int batchSize, A[] arg0, B[] arg1)
        {
            CheckBounds(arg0, arg1, batchStartsAt, batchSize);
            try
            {
                long finalSequence = sequencer.TryNext(batchSize);
                TranslateAndPublishBatch(translator, arg0, arg1, batchStartsAt, batchSize, finalSequence);
                return true;
            }
            catch (InsufficientCapacityException e)
            {
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="A"></typeparam>
        /// <typeparam name="B"></typeparam>
        /// <typeparam name="C"></typeparam>
        /// <param name="translator"></param>
        /// <param name="arg0"></param>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        public void PublishEvents<A, B, C>(IEventTranslatorThreeArg<T, A, B, C> translator, A[] arg0, B[] arg1, C[] arg2)
        {
            PublishEvents(translator, 0, arg0.Length, arg0, arg1, arg2);
        }

        public void PublishEvents<A, B, C>(IEventTranslatorThreeArg<T, A, B, C> translator, int batchStartsAt, int batchSize, A[] arg0, B[] arg1, C[] arg2)
        {
            CheckBounds(arg0, arg1, arg2, batchStartsAt, batchSize);
            long finalSequence = sequencer.Next(batchSize);
            TranslateAndPublishBatch(translator, arg0, arg1, arg2, batchStartsAt, batchSize, finalSequence);
        }

        /// <summary>
        ///  Allows three user supplied arguments per event. <see cref="publishEvents(EventTranslator[])"/>
        /// </summary>
        /// <typeparam name="A"></typeparam>
        /// <typeparam name="B"></typeparam>
        /// <typeparam name="C"></typeparam>
        /// <param name="translator">The user specified translation for the event</param>
        /// <param name="arg0"> An array of user supplied arguments, one element per event.</param>
        /// <param name="arg1"> An array of user supplied arguments, one element per event.</param>
        /// <param name="arg2"> An array of user supplied arguments, one element per event.</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <returns></returns>
        public bool TryPublishEvents<A, B, C>(IEventTranslatorThreeArg<T, A, B, C> translator, A[] arg0, B[] arg1, C[] arg2)
        {
            return TryPublishEvents(translator, 0, arg0.Length, arg0, arg1, arg2);
        }

        /// <summary>
        /// Allows three user supplied arguments per event. <see cref="PublishEvents(EventTranslator[])"/>
        /// </summary>
        /// <typeparam name="A"></typeparam>
        /// <typeparam name="B"></typeparam>
        /// <typeparam name="C"></typeparam>
        /// <param name="translator">The user specified translation for the event</param>
        /// <param name="batchStartsAt">he first element of the array which is within the batch.</param>
        /// <param name="batchSize">The number of elements in the batch.</param>
        /// <param name="arg0">An array of user supplied arguments, one element per event.</param>
        /// <param name="arg1">An array of user supplied arguments, one element per event.</param>
        /// <param name="arg2">An array of user supplied arguments, one element per event.</param>
        /// <returns>true if the value was published, false if there was insufficient capacity.</returns>
        public bool TryPublishEvents<A, B, C>(IEventTranslatorThreeArg<T, A, B, C> translator, int batchStartsAt, int batchSize, A[] arg0, B[] arg1, C[] arg2)
        {
            CheckBounds(arg0, arg1, arg2, batchStartsAt, batchSize);
            try
            {
                long finalSequence = sequencer.TryNext(batchSize);
                TranslateAndPublishBatch(translator, arg0, arg1, arg2, batchStartsAt, batchSize, finalSequence);
                return true;
            }
            catch (InsufficientCapacityException e)
            {
                return false;
            }
        }

        /// <summary>
        /// Allows a variable number of user supplied arguments per event. <see cref="publishEvents(EventTranslator[])"/>
        /// </summary>
        /// <param name="translator">The user specified translation for the event</param>
        /// <param name="args">User supplied arguments, one Object[] per event.</param>
        public void PublishEvents(IEventTranslatorVararg<T> translator, params Object[][] args)
        {
            PublishEvents(translator, 0, args.Length, args);
        }

        /// <summary>
        /// Allows a variable number of user supplied arguments per event. <see cref="publishEvents(EventTranslator[])"/>
        /// </summary>
        /// <param name="translator">The user specified translation for the event</param>
        /// <param name="batchStartsAt">The first element of the array which is within the batch.</param>
        /// <param name="batchSize">The actual size of the batch</param>
        /// <param name="args"> User supplied arguments, one Object[] per event.</param>
        public void PublishEvents(IEventTranslatorVararg<T> translator, int batchStartsAt, int batchSize, params Object[][] args)
        {
            CheckBounds(batchStartsAt, batchSize, args);
            long finalSequence = sequencer.Next(batchSize);
            TranslateAndPublishBatch(translator, batchStartsAt, batchSize, finalSequence, args);
        }

        /// <summary>
        ///  Allows a variable number of user supplied arguments per event.  <see cref="publishEvents(EventTranslator[])"/>
        /// </summary>
        /// <param name="translator">The user specified translation for the event</param>
        /// <param name="args"> User supplied arguments, one Object[] per event.</param>
        /// <returns>true if the value was published, false if there was insufficient capacity.</returns>
        public bool TryPublishEvents(IEventTranslatorVararg<T> translator, params Object[][] args)
        {
            return TryPublishEvents(translator, 0, args.Length, args);
        }

        /// <summary>
        ///  Allows a variable number of user supplied arguments per event. <see cref="publishEvents(EventTranslator[])"/>
        /// </summary>
        /// <param name="translator"> The user specified translation for the event</param>
        /// <param name="batchStartsAt">The first element of the array which is within the batch.</param>
        /// <param name="batchSize"> The actual size of the batch.</param>
        /// <param name="args">User supplied arguments, one Object[] per event.</param>
        /// <returns>true if the value was published, false if there was insufficient capacity.</returns>
        public bool TryPublishEvents(IEventTranslatorVararg<T> translator, int batchStartsAt, int batchSize, params Object[][] args)
        {
            CheckBounds(args, batchStartsAt, batchSize);
            try
            {
                long finalSequence = sequencer.TryNext(batchSize);
                TranslateAndPublishBatch(translator, batchStartsAt, batchSize, finalSequence, args);
                return true;
            }
            catch (InsufficientCapacityException e)
            {
                return false;
            }
        }

        /// <summary>
        /// Publish the specified sequence.  This action marks this particular
        /// message as being available to be read.
        /// </summary>
        /// <param name="sequence">the sequence to publish.</param>
        public void Publish(long sequence)
        {
            sequencer.Publish(sequence);
        }

        /// <summary>
        /// Publish the specified sequences.  This action marks these particular
        /// messages as being available to be read.
        /// </summary>
        /// <param name="lo">lo the lowest sequence number to be published</param>
        /// <param name="hi">hi the highest sequence number to be published</param>
        public void Publish(long lo, long hi)
        {
            sequencer.Publish(lo, hi);
        }

        /// <summary>
        /// Get the remaining capacity for this ringBuffer.
        /// </summary>
        /// <returns>The number of slots remaining.</returns>
        public long RemainingCapacity()
        {
            return sequencer.RemainingCapacity();
        }

        private void CheckBounds(IEventTranslator<T>[] translators, int batchStartsAt, int batchSize)
        {
            CheckBatchSizing(batchStartsAt, batchSize);
            BatchOverRuns(translators, batchStartsAt, batchSize);
        }

        private void CheckBatchSizing(int batchStartsAt, int batchSize)
        {
            if (batchStartsAt < 0 || batchSize < 0)
            {
                throw new ArgumentOutOfRangeException("Both batchStartsAt and batchSize must be positive but got: batchStartsAt " + batchStartsAt + " and batchSize " + batchSize);
            }
            else if (batchSize > bufferSize)
            {
                throw new ArgumentOutOfRangeException("The ring buffer cannot accommodate " + batchSize + " it only has space for " + bufferSize + " entities.");
            }
        }

        private void CheckBounds<A>(A[] arg0, int batchStartsAt, int batchSize)
        {
            CheckBatchSizing(batchStartsAt, batchSize);
            BatchOverRuns(arg0, batchStartsAt, batchSize);
        }

        private void CheckBounds<A, B>(A[] arg0, B[] arg1, int batchStartsAt, int batchSize)
        {
            CheckBatchSizing(batchStartsAt, batchSize);
            BatchOverRuns(arg0, batchStartsAt, batchSize);
            BatchOverRuns(arg1, batchStartsAt, batchSize);
        }

        private void CheckBounds<A, B, C>(A[] arg0, B[] arg1, C[] arg2, int batchStartsAt, int batchSize)
        {
            CheckBatchSizing(batchStartsAt, batchSize);
            BatchOverRuns(arg0, batchStartsAt, batchSize);
            BatchOverRuns(arg1, batchStartsAt, batchSize);
            BatchOverRuns(arg2, batchStartsAt, batchSize);
        }

        private void CheckBounds(int batchStartsAt, int batchSize, Object[] args)
        {
            CheckBatchSizing(batchStartsAt, batchSize);
            BatchOverRuns(args, batchStartsAt, batchSize);
        }

        private void BatchOverRuns<A>(A[] arg0, int batchStartsAt, int batchSize)
        {
            if (batchStartsAt + batchSize > arg0.Length)
            {
                throw new ArgumentOutOfRangeException("A batchSize of: " + batchSize +
                                                   " with batchStatsAt of: " + batchStartsAt +
                                                   " will overrun the available number of arguments: " + (arg0.Length - batchStartsAt));
            }
        }

        private void TranslateAndPublish(IEventTranslator<T> translator, long sequence)
        {
            try
            {
                translator.TranslateTo(this[sequence], sequence);
            }
            finally
            {
                sequencer.Publish(sequence);
            }
        }

        private void TranslateAndPublish<A>(IEventTranslatorOneArg<T, A> translator, long sequence, A arg0)
        {
            try
            {
                translator.TranslateTo(this[sequence], sequence, arg0);
            }
            finally
            {
                sequencer.Publish(sequence);
            }
        }

        private void TranslateAndPublish<A, B>(IEventTranslatorTwoArg<T, A, B> translator, long sequence, A arg0, B arg1)
        {
            try
            {
                translator.TranslateTo(this[sequence], sequence, arg0, arg1);
            }
            finally
            {
                sequencer.Publish(sequence);
            }
        }

        private void TranslateAndPublish<A, B, C>(IEventTranslatorThreeArg<T, A, B, C> translator, long sequence,
                                                   A arg0, B arg1, C arg2)
        {
            try
            {
                translator.TranslateTo(this[sequence], sequence, arg0, arg1, arg2);
            }
            finally
            {
                sequencer.Publish(sequence);
            }
        }

        private void TranslateAndPublish<A>(IEventTranslatorVararg<T> translator, long sequence, params A[] args)
        {
            try
            {
                translator.TranslateTo(this[sequence], sequence, args);
            }
            finally
            {
                sequencer.Publish(sequence);
            }
        }

        private void TranslateAndPublishBatch(IEventTranslator<T>[] translators, int batchStartsAt,
                                               int batchSize, long finalSequence)
        {
            long initialSequence = finalSequence - (batchSize - 1);
            try
            {
                long sequence = initialSequence;
                int batchEndsAt = batchStartsAt + batchSize;
                for (int i = batchStartsAt; i < batchEndsAt; i++)
                {
                    IEventTranslator<T> translator = translators[i];
                    translator.TranslateTo(this[sequence], sequence++);
                }
            }
            finally
            {
                sequencer.Publish(initialSequence, finalSequence);
            }
        }

        private void TranslateAndPublishBatch<A>(IEventTranslatorOneArg<T, A> translator, A[] arg0,
                                                  int batchStartsAt, int batchSize, long finalSequence)
        {
            long initialSequence = finalSequence - (batchSize - 1);
            try
            {
                long sequence = initialSequence;
                int batchEndsAt = batchStartsAt + batchSize;
                for (int i = batchStartsAt; i < batchEndsAt; i++)
                {
                    translator.TranslateTo(this[sequence], sequence++, arg0[i]);
                }
            }
            finally
            {
                sequencer.Publish(initialSequence, finalSequence);
            }
        }

        private void TranslateAndPublishBatch<A, B>(IEventTranslatorTwoArg<T, A, B> translator, A[] arg0,
                                                      B[] arg1, int batchStartsAt, int batchSize,
                                                      long finalSequence)
        {
            long initialSequence = finalSequence - (batchSize - 1);
            try
            {
                long sequence = initialSequence;
                int batchEndsAt = batchStartsAt + batchSize;
                for (int i = batchStartsAt; i < batchEndsAt; i++)
                {
                    translator.TranslateTo(this[sequence], sequence++, arg0[i], arg1[i]);
                }
            }
            finally
            {
                sequencer.Publish(initialSequence, finalSequence);
            }
        }

        private void TranslateAndPublishBatch<A, B, C>(IEventTranslatorThreeArg<T, A, B, C> translator,
                                                         A[] arg0, B[] arg1, C[] arg2, int batchStartsAt,
                                                         int batchSize, long finalSequence)
        {
            long initialSequence = finalSequence - (batchSize - 1);
            try
            {
                long sequence = initialSequence;
                int batchEndsAt = batchStartsAt + batchSize;
                for (int i = batchStartsAt; i < batchEndsAt; i++)
                {
                    translator.TranslateTo(this[sequence], sequence++, arg0[i], arg1[i], arg2[i]);
                }
            }
            finally
            {
                sequencer.Publish(initialSequence, finalSequence);
            }
        }

        private void TranslateAndPublishBatch(IEventTranslatorVararg<T> translator, int batchStartsAt,
                                               int batchSize, long finalSequence, Object[][] args)
        {
            long initialSequence = finalSequence - (batchSize - 1);
            try
            {
                long sequence = initialSequence;
                int batchEndsAt = batchStartsAt + batchSize;
                for (int i = batchStartsAt; i < batchEndsAt; i++)
                {
                    translator.TranslateTo(this[sequence], sequence++, args[i]);
                }
            }
            finally
            {
                sequencer.Publish(initialSequence, finalSequence);
            }
        }

        private void InternalPublish<A>(Action<T, long, A> action, long sequence, A arg0)
        {
            try
            {
                action(this[sequence], sequence, arg0);
            }
            finally
            {
                sequencer.Publish(sequence);
            }
        }

        private void InternalPublish<A, B>(Action<T, long, A, B> action, long sequence, A arg0, B arg1)
        {
            try
            {
                action(this[sequence], sequence, arg0, arg1);
            }
            finally
            {
                sequencer.Publish(sequence);
            }
        }

        private void InternalPublish<A, B, C>(Action<T, long, A, B, C> action, long sequence, A arg0, B arg1, C arg2)
        {
            try
            {
                action(this[sequence], sequence, arg0, arg1, arg2);
            }
            finally
            {
                sequencer.Publish(sequence);
            }
        }

        private void InternalPublish<A>(EventTranslatorVarargTranslateTo<T, A> action, long sequence, params A[] arg)
        {
            try
            {
                action(this[sequence], sequence, arg);
            }
            finally
            {
                sequencer.Publish(sequence);
            }
        }

        private void TranslateAndPublish(Action<T, long> translatorAction, long sequence)
        {
            try
            {
                translatorAction(this[sequence], sequence);
            }
            finally
            {
                sequencer.Publish(sequence);
            }
        }

        private void VerifyBufferSize()
        {
            if (bufferSize < 1)
            {
                throw new ArgumentException("bufferSize must not be less than 1");
            }
            if (!bufferSize.IsPowerOf2())
            {
                throw new ArgumentException("bufferSize must be a power of 2");
            }
        }

        private void FillInstance(Func<T> eventFactory)
        {
            for (int i = 0; i < bufferSize; i++)
            {
                this.entries[/*BUFFER_PAD+*/i] = eventFactory();
            }
        }
    }
}
