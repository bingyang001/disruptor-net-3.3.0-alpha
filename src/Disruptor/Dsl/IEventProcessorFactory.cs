using System;

namespace Disruptor.Dsl
{
    /// <summary>
    /// A factory interface to make it possible to include custom event processors in a chain:
    /// <pre><code>
    /// disruptor.handleEventsWith(handler1).then((ringBuffer, barrierSequences) -> new CustomEventProcessor(ringBuffer, barrierSequences));
    /// </code></pre>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IEventProcessorFactory<T> where T:class
    {
        /// <summary>
        /// Create a new event processor that gates on <code>barrierSequences</code>.
        /// </summary>
        /// <param name="ringBuffer">barrierSequences the sequences to gate on</param>
        /// <param name="barrierSequences"></param>
        /// <returns>a new EventProcessor that gates on <code>barrierSequences</code> before processing events</returns>
        IEventProcessor CreateEventProcessor(RingBuffer<T> ringBuffer, Sequence[] barrierSequences);
    }
}
