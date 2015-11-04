using System.Collections.Generic;
using System.Linq;

namespace Disruptor
{
    /// <summary>
    /// An aggregate collection of {@link EventHandler}s that get called in sequence for each event.
    /// </summary>
    /// <typeparam name="T">event implementation storing the data for sharing during exchange or parallel coordination of an event.</typeparam>
    public class AggregateEventHandler<T> : IEventHandler<T>, ILifecycleAware
    {
        private readonly IEventHandler<T>[] eventHandlers;

        /// <summary>
        /// Construct an aggregate collection of {@link EventHandler}s to be called in sequence.
        /// </summary>
        /// <param name="eventHandlers">to be called in sequence.</param>
        public AggregateEventHandler(params IEventHandler<T>[] eventHandlers)
        {
            this.eventHandlers = eventHandlers;
        }

        /// <summary>
        /// 
        /// </summary>
        public void OnStart()
        {
            this.eventHandlers.Where(e => e is ILifecycleAware).ToList().ForEach(e => (e as ILifecycleAware).OnStart());
        }

        /// <summary>
        /// 
        /// </summary>
        public void OnShutdown()
        {
            this.eventHandlers.Where(e => e is ILifecycleAware).ToList().ForEach(e => (e as ILifecycleAware).OnShutdown());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="event"></param>
        /// <param name="sequence"></param>
        /// <param name="endOfBatch"></param>
        public void OnEvent(T @event, long sequence, bool endOfBatch)
        {
            this.eventHandlers.ToList().ForEach(e => e.OnEvent(@event, sequence, endOfBatch));
        }
    }
}
