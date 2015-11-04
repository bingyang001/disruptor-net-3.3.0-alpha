namespace Disruptor
{   
    /// <summary>
    /// Callback interface to be implemented for processing units of work as they become available in the {@link RingBuffer}.
    /// </summary>
    /// <typeparam name="T">event implementation storing the data for sharing during exchange or parallel coordination of an event.</typeparam>
    public interface IWorkHandler<T>
    {
        /// <summary>
        /// Callback to indicate a unit of work needs to be processed.
        /// </summary>
        /// <exception cref="Exception">Exception</exception>
        /// <param name="event">event published to the {@link RingBuffer}</param>
        void OnEvent(T @event);
    }
}
