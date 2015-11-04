using System;

namespace Disruptor
{
    /// <summary>
    /// Implementations translate another data representations into events claimed from the {@link RingBuffer}
    /// </summary>
    /// <typeparam name="T">implementation storing the data for sharing during exchange or parallel coordination of an event <see cref="EventTranslator"/> .</typeparam>   
    public interface IEventTranslatorVararg<T>
    {
        /// <summary>
        /// Translate a data representation into fields set in given event. 
        /// Note：java version , void TranslateTo(T @event, long sequence, params Object[] args)
        /// </summary>       
        /// <param name="event">into which the data should be translated.</param>
        /// <param name="sequence">that is assigned to event.</param>
        /// <param name="args">The array of user arguments.</param>
        /// <remarks>Note：java version , void TranslateTo(T @event, long sequence, params Object[] args)</remarks>
        void TranslateTo(T @event, long sequence, params Object[] args);
    }
}
