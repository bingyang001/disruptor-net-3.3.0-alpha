namespace Disruptor
{
    /// <summary>
    /// Implementations translate another data representations into events claimed from the {@link RingBuffer}
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="A"></typeparam>
    /// <typeparam name="B"></typeparam>
    public interface IEventTranslatorTwoArg<T, A, B>
    {
        /// <summary>
        /// Translate a data representation into fields set in given event
        /// </summary>
        /// <param name="event"></param>
        /// <param name="sequence"></param>
        /// <param name="arg0"></param>
        /// <param name="arg1"></param>
        void TranslateTo(T @event, long sequence, A arg0, B arg1);
    }
}
