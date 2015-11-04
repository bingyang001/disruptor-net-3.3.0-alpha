namespace Disruptor
{
    /// <summary>
    ///* <p>Implementations translate (write) data representations into events claimed from the {@link RingBuffer}.</p>
    ///*
    ///* <p>When publishing to the RingBuffer, provide an EventTranslator. The RingBuffer will select the next available
    ///* event by sequence and provide it to the EventTranslator (which should update the event), before publishing
    ///* the sequence update.</p>
    /// </summary>
    /// <typeparam name="T">event implementation storing the data for sharing during exchange or parallel coordination of an event.</typeparam>
    public interface IEventTranslator<T>
    {
        /// <summary>
        /// Translate a data representation into fields set in given event
        /// </summary>
        /// <param name="event">into which the data should be translated.</param>
        /// <param name="sequence">that is assigned to event.</param>
        void TranslateTo(T @event, long sequence);
    }
}
