namespace Disruptor
{
    /// <summary>
    /// Implementations translate another data representations into events claimed from the {@link RingBuffer}
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="A"></typeparam>
    /// <typeparam name="B"></typeparam>
    /// <typeparam name="C"></typeparam>
    public interface IEventTranslatorThreeArg<T, A, B, C>
    {
        /// <summary>
        /// Translate a data representation into fields set in given event
        /// </summary>
        /// <param name="event">into which the data should be translated.</param>
        /// <param name="sequence">that is assigned to event.</param>
        /// <param name="arg0">The first user specified argument to the translator</param>
        /// <param name="arg1">The second user specified argument to the translator</param>
        /// <param name="arg2">The third user specified argument to the translator</param>
        void TranslateTo(T @event, long sequence, A arg0, B arg1, C arg2);
    }
}
