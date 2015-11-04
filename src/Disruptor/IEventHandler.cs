namespace Disruptor
{
    /// <summary>
    /// Callback interface to be implemented for processing events as they become available in the {@link RingBuffer}
    /// 
    /// BatchEventProcessor#SetExceptionHandler(ExceptionHandler) if you want to handle exceptions propagated out of the handler.
    /// </summary>
    /// <typeparam name="T">event implementation storing the data for sharing during exchange or parallel coordination of an event.</typeparam>
    public interface IEventHandler<T>
    {
        /// <summary>
        /// Called when a publisher has published an event to the {@link RingBuffer}
        /// </summary>
        /// <exception cref="Exception">if the EventHandler would like the exception handled further up the chain.</exception>
        /// <param name="event">published to the {@link RingBuffer}</param>
        /// <param name="sequence">of the event being processed</param>
        /// <param name="endOfBatch">flag to indicate if this is the last event in a batch from the {@link RingBuffer}</param>
        void OnEvent(T @event, long sequence, bool endOfBatch);
    }
}
