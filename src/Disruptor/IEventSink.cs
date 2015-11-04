using System;

namespace Disruptor
{
    public interface IEventSink<T>
    {

        /// <summary>
        ///  Publishes an event to the ring buffer.  It handles claiming the next sequence, getting the current (uninitialised)
        ///  event from the ring buffer and publishing the claimed sequence   after translation.
        /// </summary>
        /// <param name="translator">translator The user specified translation for the event</param>
        void PublishEvent(IEventTranslator<T> translator);

        /// <summary>
        ///  Attempts to publish an event to the ring buffer.  It handles  claiming the next sequence, getting the current (uninitialised)
        ///  event from the ring buffer and publishing the claimed sequence
        ///  after translation.  Will return false if specified capacity was not available.
        /// </summary>
        /// <param name="translator">translator The user specified translation for the event</param>
        /// <returns>true if the value was published, false if there was insufficient capacity.</returns>
        bool TryPublishEvent(IEventTranslator<T> translator);

        /// <summary>      
        /// Allows one user supplied argument.
        /// @see #publishEvent(EventTranslator)  
        /// </summary>
        /// <typeparam name="A"></typeparam>
        /// <param name="translator">translator The user specified translation for the event</param>
        /// <param name="arg0">arg0 A user supplied argument.  </param>
        void PublishEvent<A>(IEventTranslatorOneArg<T, A> translator, A arg0);

        /// <summary>
        /// 
        /// Allows one user supplied argument.
        ///
        /// @see #tryPublishEvent(EventTranslator) 
        /// </summary>
        /// <typeparam name="A"></typeparam>
        /// <param name="translator"> translator The user specified translation for the event</param>
        /// <param name="arg0">A user supplied argument.</param>
        /// <returns>true if the value was published, false if there was insufficient capacity.   </returns>
        bool TryPublishEvent<A>(IEventTranslatorOneArg<T, A> translator, A arg0);

        /// <summary>
        /// Allows two user supplied arguments.@see #publishEvent(EventTranslator)
        /// </summary>
        /// <typeparam name="A"></typeparam>
        /// <typeparam name="B"></typeparam>
        /// <param name="translator"> translator The user specified translation for the event</param>
        /// <param name="arg0">A user supplied argument.</param>
        /// <param name="arg1">A user supplied argument.</param>
        void PublishEvent<A, B>(IEventTranslatorTwoArg<T, A, B> translator, A arg0, B arg1);

        /// <summary>
        ///  Allows two user supplied arguments.see #tryPublishEvent(EventTranslator)
        /// </summary>
        /// <typeparam name="A"></typeparam>
        /// <typeparam name="B"></typeparam>
        /// <param name="translator">translator The user specified translation for the event</param>
        /// <param name="arg0"> A user supplied argument.</param>
        /// <param name="arg1">A user supplied argument.</param>
        /// <returns>true if the value was published, false if there was insufficient capacity.</returns>
        bool TryPublishEvent<A, B>(IEventTranslatorTwoArg<T, A, B> translator, A arg0, B arg1);

        /// <summary>
        ///  Allows three user supplied arguments @see #publishEvent(EventTranslator)
        /// </summary>
        /// <typeparam name="A"></typeparam>
        /// <typeparam name="B"></typeparam>
        /// <typeparam name="C"></typeparam>
        /// <param name="translator">translator The user specified translation for the event</param>
        /// <param name="arg0">A user supplied argument.</param>
        /// <param name="arg1">A user supplied argument.</param>
        /// <param name="arg2">A user supplied argument.</param>
        void PublishEvent<A, B, C>(IEventTranslatorThreeArg<T, A, B, C> translator, A arg0, B arg1, C arg2);

        /// <summary>
        ///  Allows three user supplied arguments @see #publishEvent(EventTranslator)
        /// </summary>
        /// <typeparam name="A"></typeparam>
        /// <typeparam name="B"></typeparam>
        /// <typeparam name="C"></typeparam>
        /// <param name="translator">translator The user specified translation for the event</param>
        /// <param name="arg0">A user supplied argument.</param>
        /// <param name="arg1">A user supplied argument.</param>
        /// <param name="arg2">A user supplied argument.</param>
        /// <returns>true if the value was published, false if there was insufficient capacity.</returns>
        bool TryPublishEvent<A, B, C>(IEventTranslatorThreeArg<T, A, B, C> translator, A arg0, B arg1, C arg2);

        /// <summary>
        ///  Allows a variable number of user supplied arguments @see #publishEvent(EventTranslator)
        /// </summary>
        /// <param name="translator">translator The user specified translation for the event</param>
        /// <param name="args">User supplied arguments.</param>
        void PublishEvent(IEventTranslatorVararg<T> translator, params Object[][] args);

        /// <summary>
        ///  Allows a variable number of user supplied arguments #publishEvents(com.lmax.disruptor.EventTranslator[])
        /// </summary>
        /// <param name="translator">translator The user specified translation for the event</param>
        /// <param name="args">User supplied arguments.</param>
        bool TryPublishEvent(IEventTranslatorVararg<T> translator, params Object[][] args);

        /// <summary>
        /// Publishes multiple events to the ring buffer.  It handles claiming the next sequence, getting the current (uninitialised)
        /// event from the ring buffer and publishing the claimed sequence after translation.
        /// <p>With this call the data that is to be inserted into the ring
        /// buffer will be a field (either explicitly or captured anonymously),
        /// therefore this call will require an instance of the translator
        /// for each value that is to be inserted into the ring buffer.
        /// </summary>
        /// <param name="translators">The user specified translation for each event</param>
        void PublishEvents(IEventTranslator<T>[] translators);

        /// <summary>
        /// Publishes multiple events to the ring buffer.  It handles claiming the next sequence, getting the current (uninitialised)
        /// event from the ring buffer and publishing the claimed sequence  after translation.
        /// <p>
        /// With this call the data that is to be inserted into the ring
        /// buffer will be a field (either explicitly or captured anonymously),
        /// therefore this call will require an instance of the translator
        /// for each value that is to be inserted into the ring buffer.
        /// </p>
        /// </summary>
        /// <param name="translators">The user specified translation for each event</param>
        /// <param name="batchStartsAt">The first element of the array which is within the batch.</param>
        /// <param name="batchSize">The actual size of the batch</param>
        void PublishEvents(IEventTranslator<T>[] translators, int batchStartsAt, int batchSize);

        /// <summary>
        /// Attempts to publish multiple events to the ring buffer.  It handles
        ///   claiming the next sequence, getting the current (uninitialised)
        ///   event from the ring buffer and publishing the claimed sequence
        ///   after translation.  Will return false if specified capacity
        ///   was not available.
        /// </summary>
        /// <param name="translators"> The user specified translation for the event</param>
        /// <returns>true if the value was published, false if there was insufficient capacity.</returns>
        bool TryPublishEvents(IEventTranslator<T>[] translators);

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
        /// <returns> true if all the values were published, false if there was insufficient apacity.</returns>
        bool TryPublishEvents(IEventTranslator<T>[] translators, int batchStartsAt, int batchSize);

        /// <summary>
        /// Allows one user supplied argument per event. @see #publishEvents(com.lmax.disruptor.EventTranslator[])
        /// </summary>
        /// <typeparam name="A"></typeparam>
        /// <param name="translator">The user specified translation for the event</param>
        /// <param name="arg0">A user supplied argument.</param>
        void PublishEvents<A>(IEventTranslatorOneArg<T, A> translator, A[] arg0);

        /// <summary>
        ///  Allows one user supplied argument per event.@see #publishEvents(EventTranslator[])
        /// </summary>
        /// <typeparam name="A"></typeparam>
        /// <param name="translator">The user specified translation for each event</param>
        /// <param name="batchStartsAt">The first element of the array which is within the batch.</param>
        /// <param name="batchSize">The actual size of the batch</param>
        /// <param name="arg0"> An array of user supplied arguments, one element per event.</param>
        void PublishEvents<A>(IEventTranslatorOneArg<T, A> translator, int batchStartsAt, int batchSize, A[] arg0);

        /// <summary>
        /// Allows one user supplied argument. @see #tryPublishEvents(com.lmax.disruptor.EventTranslator[])
        /// </summary>
        /// <typeparam name="A"></typeparam>
        /// <param name="translator">The user specified translation for each event</param>
        /// <param name="arg0"> An array of user supplied arguments, one element per event.</param>
        /// <returns>true if the value was published, false if there was insufficient
        ///capacity.</returns>
        bool TryPublishEvents<A>(IEventTranslatorOneArg<T, A> translator, A[] arg0);

        /// <summary>
        /// Allows one user supplied argument.
        /// </summary>
        /// <typeparam name="A"></typeparam>
        /// <param name="translator"> The user specified translation for each event</param>
        /// <param name="batchStartsAt">The first element of the array which is within the batch.</param>
        /// <param name="batchSize">The actual size of the batch</param>
        /// <param name="arg0"> An array of user supplied arguments, one element per event.</param>
        /// <returns>true if the value was published, false if there was insufficient
        ///        capacity.</returns>
        bool TryPublishEvents<A>(IEventTranslatorOneArg<T, A> translator, int batchStartsAt, int batchSize, A[] arg0);

        /// <summary>
        /// Allows two user supplied arguments per event.@see #publishEvents(com.lmax.disruptor.EventTranslator[])
        /// </summary>
        /// <typeparam name="A"></typeparam>
        /// <typeparam name="B"></typeparam>
        /// <param name="translator">The user specified translation for the event</param>
        /// <param name="arg0">An array of user supplied arguments, one element per event.</param>
        /// <param name="arg1">An array of user supplied arguments, one element per event.</param>
        void PublishEvents<A, B>(IEventTranslatorTwoArg<T, A, B> translator, A[] arg0, B[] arg1);

        /// <summary>
        ///  Allows two user supplied arguments per event.@see #publishEvents(EventTranslator[])
        /// </summary>
        /// <typeparam name="A"></typeparam>
        /// <typeparam name="B"></typeparam>
        /// <param name="translator">The user specified translation for the event</param>
        /// <param name="batchStartsAt">The first element of the array which is within the batch.</param>
        /// <param name="batchSize">The actual size of the batch.</param>
        /// <param name="arg0"> An array of user supplied arguments, one element per event.</param>
        /// <param name="arg1">An array of user supplied arguments, one element per event.</param>
        void PublishEvents<A, B>(IEventTranslatorTwoArg<T, A, B> translator, int batchStartsAt, int batchSize, A[] arg0,
                                 B[] arg1);

        /// <summary>
        ///  Allows two user supplied arguments per event.@see #tryPublishEvents(com.lmax.disruptor.EventTranslator[])
        /// </summary>
        /// <typeparam name="A"></typeparam>
        /// <typeparam name="B"></typeparam>
        /// <param name="translator">The user specified translation for the event</param>
        /// <param name="arg0">An array of user supplied arguments, one element per event.</param>
        /// <param name="arg1">An array of user supplied arguments, one element per event.</param>
        /// <returns>true if the value was published, false if there was insufficient
        ///         capacity.</returns>
        bool TryPublishEvents<A, B>(IEventTranslatorTwoArg<T, A, B> translator, A[] arg0, B[] arg1);

        /// <summary>
        /// Allows two user supplied arguments per event. @see #tryPublishEvents(EventTranslator[])
        /// </summary>
        /// <typeparam name="A"></typeparam>
        /// <typeparam name="B"></typeparam>
        /// <param name="translator">The user specified translation for the event</param>
        /// <param name="batchStartsAt">The first element of the array which is within the batch.</param>
        /// <param name="batchSize">The actual size of the batch.</param>
        /// <param name="arg0">An array of user supplied arguments, one element per event.</param>
        /// <param name="arg1">An array of user supplied arguments, one element per event.</param>
        /// <returns>true if the value was published, false if there was insufficient
        ///         capacity.</returns>
        bool TryPublishEvents<A, B>(IEventTranslatorTwoArg<T, A, B> translator, int batchStartsAt, int batchSize,
                                       A[] arg0, B[] arg1);

        /**
         * Allows three user supplied arguments per event.
         *
         * @param translator The user specified translation for the event
         * @param arg0       An array of user supplied arguments, one element per event.
         * @param arg1       An array of user supplied arguments, one element per event.
         * @param arg2       An array of user supplied arguments, one element per event.
         * @see #publishEvents(com.lmax.disruptor.EventTranslator[])
         */
        void PublishEvents<A, B, C>(IEventTranslatorThreeArg<T, A, B, C> translator, A[] arg0, B[] arg1, C[] arg2);

        /**
         * Allows three user supplied arguments per event.
         *
         * @param translator    The user specified translation for the event
         * @param batchStartsAt The first element of the array which is within the batch.
         * @param batchSize     The number of elements in the batch.
         * @param arg0          An array of user supplied arguments, one element per event.
         * @param arg1          An array of user supplied arguments, one element per event.
         * @param arg2          An array of user supplied arguments, one element per event.
         * @see #publishEvents(EventTranslator[])
         */
        void PublishEvents<A, B, C>(IEventTranslatorThreeArg<T, A, B, C> translator, int batchStartsAt, int batchSize,
                                    A[] arg0, B[] arg1, C[] arg2);

        /// <summary>
        /// Allows three user supplied arguments per event.
        /// </summary>
        /// <typeparam name="A"></typeparam>
        /// <typeparam name="B"></typeparam>
        /// <typeparam name="C"></typeparam>
        /// <param name="translator">The user specified translation for the event</param>
        /// <param name="arg0">An array of user supplied arguments, one element per event.</param>
        /// <param name="arg1">An array of user supplied arguments, one element per event.</param>
        /// <param name="arg2">An array of user supplied arguments, one element per event.</param>
        /// <returns>true if the value was published, false if there was insufficient
        ///        capacity.</returns>
        bool TryPublishEvents<A, B, C>(IEventTranslatorThreeArg<T, A, B, C> translator, A[] arg0, B[] arg1, C[] arg2);

        /// <summary>
        /// Allows three user supplied arguments per event.
        /// </summary>
        /// <typeparam name="A"></typeparam>
        /// <typeparam name="B"></typeparam>
        /// <typeparam name="C"></typeparam>
        /// <param name="translator">The user specified translation for the event</param>
        /// <param name="batchStartsAt"> The first element of the array which is within the batch.</param>
        /// <param name="batchSize"> The actual size of the batch.</param>
        /// <param name="arg0"> An array of user supplied arguments, one element per event.</param>
        /// <param name="arg1"> An array of user supplied arguments, one element per event.</param>
        /// <param name="arg2"> An array of user supplied arguments, one element per event.</param>
        /// <returns>true if the value was published, false if there was insufficient
        ///         capacity.</returns>
        bool TryPublishEvents<A, B, C>(IEventTranslatorThreeArg<T, A, B, C> translator, int batchStartsAt,
                                          int batchSize, A[] arg0, B[] arg1, C[] arg2);

        /// <summary>
        /// Allows a variable number of user supplied arguments per event.
        /// </summary>
        /// <param name="translator"></param>
        /// <param name="args"></param>
        void PublishEvents(IEventTranslatorVararg<T> translator, params Object[][] args);

        /// <summary>
        /// Allows a variable number of user supplied arguments per event. @see #publishEvents(EventTranslator[])
        /// </summary>
        /// <param name="translator">The user specified translation for the event</param>
        /// <param name="batchStartsAt">The first element of the array which is within the batch.</param>
        /// <param name="batchSize">The actual size of the batch</param>
        /// <param name="args"> User supplied arguments, one Object[] per event.</param>
        void PublishEvents(IEventTranslatorVararg<T> translator, int batchStartsAt, int batchSize, params Object[][] args);

        /// <summary>
        /// Allows a variable number of user supplied arguments per event.  @see #publishEvents(com.lmax.disruptor.EventTranslator[])
        /// </summary>
        /// <param name="translator">The user specified translation for the event</param>
        /// <param name="args"> User supplied arguments, one Object[] per event.</param>
        /// <returns>true if the value was published, false if there was insufficient
        ///         capacity.</returns>
        bool TryPublishEvents(IEventTranslatorVararg<T> translator, params Object[][] args);

        /// <summary>
        /// Allows a variable number of user supplied arguments per event.
        /// </summary>
        /// <param name="translator">The user specified translation for the event</param>
        /// <param name="batchStartsAt">The first element of the array which is within the batch.</param>
        /// <param name="batchSize">The actual size of the batch.</param>
        /// <param name="args">User supplied arguments, one Object[] per event.</param>
        /// <returns>true if the value was published, false if there was insufficient
        ///         capacity.</returns>
        bool TryPublishEvents(IEventTranslatorVararg<T> translator, int batchStartsAt, int batchSize, params Object[][] args);

    }
}
