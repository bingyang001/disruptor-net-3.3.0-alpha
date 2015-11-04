namespace Disruptor
{
    /// <summary>
    /// Implementations translate another data representations into events claimed from the {@link RingBuffer}
    /// <p>
    /// Translate a data representation into fields set in given event
    /// </p>
    /// </summary>
    /// <typeparam name="T">event implementation storing the data for sharing during exchange or parallel coordination of an event.</typeparam>
    /// <param name="event">into which the data should be translated.</param>
    /// <param name="sequence">that is assigned to event.</param>
    /// <param name="args">The array of user arguments.</param>
    public delegate void EventTranslatorVarargTranslateTo<T,A>(T @event, long sequence, params A[] args);
}
