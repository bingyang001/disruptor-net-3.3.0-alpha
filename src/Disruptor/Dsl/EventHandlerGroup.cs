using System.Linq;

namespace Disruptor.Dsl
{
    /// <summary>
    /// A group of <see cref="EventProcessor"/>s used as part of the <see cref="Disruptor"/>.
    /// </summary>
    /// <typeparam name="T">the type of entry used by the event processors</typeparam>
    public class EventHandlerGroup<T> where T : class
    {
        private readonly Disruptor<T> disruptor;
        private readonly ConsumerRepository<T> consumerRepository;
        private readonly Sequence[] sequences;

        internal EventHandlerGroup(Disruptor<T> disruptor,
                       ConsumerRepository<T> consumerRepository,
                       Sequence[] sequences)
        {
            this.disruptor = disruptor;
            this.consumerRepository = consumerRepository;
            this.sequences = sequences;
        }

        /// <summary>
        /// Create a new event handler group that combines the consumers in this group with <tt>otherHandlerGroup</tt>.
        /// </summary>
        /// <param name="otherHandlerGroup">the event handler group to combine.</param>
        /// <returns>a new EventHandlerGroup combining the existing and new consumers into a single dependency group.</returns>
        public EventHandlerGroup<T> And(EventHandlerGroup<T> otherHandlerGroup)
        {
            var combinedSequences = otherHandlerGroup.sequences.Concat(this.sequences).ToArray();

            return new EventHandlerGroup<T>(disruptor, consumerRepository, combinedSequences);
        }

        /// <summary>
        /// Create a new event handler group that combines the handlers in this group with <tt>processors</tt>.
        /// </summary>
        /// <param name="processors">the processors to combine.</param>
        /// <returns>a new EventHandlerGroup combining the existing and new processors into a single dependency group.</returns>
        public EventHandlerGroup<T> And(params  IEventProcessor[] processors)
        {

            var combinedProcessors = processors.Select(e => e.Sequence).Concat(sequences).ToArray();

            foreach (var eventProcessor in processors)
            {
                consumerRepository.Add(eventProcessor);
            }

            return new EventHandlerGroup<T>(disruptor, consumerRepository, combinedProcessors);
        }

        /// <summary>
        /// Set up batch handlers to consume events from the ring buffer. These handlers will only process events
        /// after every {@link EventProcessor} in this group has processed the event.
        ///
        /// <p>This method is generally used as part of a chain. For example if the handler <code>A</code> must
        /// process events before handler <code>B</code>:</p>
        ///
        /// <pre><code>dw.HandleEventsWith(A).then(B);</code></pre>
        /// </summary>
        /// <param name="handlers">the batch handlers that will process events.</param>
        /// <returns>a <see cref="EventHandlerGroup"/> that can be used to set up a event processor barrier over the created event processors.</returns>
        public EventHandlerGroup<T> Then(params IEventHandler<T>[] handlers)
        {
            return HandleEventsWith(handlers);
        }

        /// <summary>
        /// <p>Set up custom event processors to handle events from the ring buffer. The Disruptor will
        /// automatically start these processors when {@link Disruptor#start()} is called.</p>
        ///
        /// <p>This method is generally used as part of a chain. For example if the handler <code>A</code> must
        /// process events before handler <code>B</code>:</p>
        /// </summary>
        /// <param name="eventProcessorFactories">eventProcessorFactories the event processor factories to use to create the event processors that will process events.</param>
        /// <returns>a {@link EventHandlerGroup} that can be used to chain dependencies.</returns>
        public EventHandlerGroup<T> Then(params IEventProcessorFactory<T>[] eventProcessorFactories)
        {
            return HandleEventsWith(eventProcessorFactories);
        }


        /// <summary>
        /// Set up a worker pool to handle events from the ring buffer. The worker pool will only process events
        /// after every {@link EventProcessor} in this group has processed the event. Each event will be processed
        /// by one of the work handler instances.
        ///
        /// <p>This method is generally used as part of a chain. For example if the handler <code>A</code> must
        /// process events before the worker pool with handlers <code>B, C</code>:</p>
        ///
        /// <pre><code>dw.HandleEventsWith(A).thenHandleEventsWithWorkerPool(B, C);</code></pre>
        /// </summary>
        /// <param name="handlers">the work handlers that will process events. Each work handler instance will provide an extra thread in the worker pool.</param>
        /// <returns>a {@link EventHandlerGroup} that can be used to set up a event processor barrier over the created event processors.</returns>
        public EventHandlerGroup<T> ThenHandleEventsWithWorkerPool(params IWorkHandler<T>[] handlers)
        {
            return HandleEventsWithWorkerPool(handlers);
        }

        /// <summary>
        /// Set up batch handlers to handle events from the ring buffer. These handlers will only process events
        /// after every {@link EventProcessor} in this group has processed the event.
        ///
        /// <p>This method is generally used as part of a chain. For example if the handler <code>A</code> must
        /// process events before handler <code>B</code>:</p>
        ///
        /// <pre><code>dw.after(A).HandleEventsWith(B);</code></pre>
        /// </summary>
        /// <param name="handlers">the batch handlers that will process events.</param>
        /// <returns>a {@link EventHandlerGroup} that can be used to set up a event processor barrier over the created event processors.</returns>
        public EventHandlerGroup<T> HandleEventsWith(params IEventHandler<T>[] handlers)
        {
            return disruptor.CreateEventProcessors(sequences, handlers);
        }

        /// <summary>
        /// <p>Set up custom event processors to handle events from the ring buffer. The Disruptor will
        /// automatically start these processors when {@link Disruptor#start()} is called.</p>
        /// 
        /// <p>This method is generally used as part of a chain. For example if <code>A</code> must
        /// process events before <code>B</code>:</p>
        ///
        /// <pre><code>dw.after(A).handleEventsWith(B);</code></pre>
        ///  a {@link EventHandlerGroup} that can be used to chain dependencies.
        /// </summary>
        /// <param name="eventProcessorFactories">eventProcessorFactories the event processor factories to use to create the event processors that will process events.</param>
        /// <returns></returns>
        public EventHandlerGroup<T> HandleEventsWith(params IEventProcessorFactory<T>[] eventProcessorFactories)
        {
            return disruptor.CreateEventProcessors(sequences, eventProcessorFactories);
        }

        /// <summary>
        /// Set up a worker pool to handle events from the ring buffer. The worker pool will only process events
        /// after every {@link EventProcessor} in this group has processed the event. Each event will be processed
        /// by one of the work handler instances.
        ///
        /// <p>This method is generally used as part of a chain. For example if the handler <code>A</code> must
        /// process events before the worker pool with handlers <code>B, C</code>:</p>
        ///
        /// <pre><code>dw.after(A).handleEventsWithWorkerPool(B, C);</code></pre>
        /// </summary>
        /// <param name="handlers">the work handlers that will process events. Each work handler instance will provide an extra thread in the worker pool.</param>
        /// <returns>a {@link EventHandlerGroup} that can be used to set up a event processor barrier over the created event processors.</returns>
        public EventHandlerGroup<T> HandleEventsWithWorkerPool(params IWorkHandler<T>[] handlers)
        {
            return disruptor.CreateWorkerPool(sequences, handlers);
        }

        /// <summary>
        /// Create a dependency barrier for the processors in this group.
        /// This allows custom event processors to have dependencies on
        /// {@link com.lmax.disruptor.BatchEventProcessor}s created by the disruptor.
        /// </summary>
        /// <returns>a {@link SequenceBarrier} including all the processors in this group.</returns>
        public ISequenceBarrier AsSequenceBarrier()
        {
            return disruptor.GetRingBuffer.NewBarrier(sequences);
        }
    }
}
