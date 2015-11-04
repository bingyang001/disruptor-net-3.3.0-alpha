using System;

namespace Disruptor
{
    /// <summary>  
    ///    Callback handler for uncaught exceptions in the event processing cycle of the {@link BatchEventProcessor}    
    /// </summary>
    public interface IExceptionHandler
    {
        /// <summary>
        /// <p>Strategy for handling uncaught exceptions when processing an event.</p>
        ///
        /// <p>If the strategy wishes to terminate further processing by the {@link BatchEventProcessor}
        /// then it should throw a {@link RuntimeException}.</p>
        /// </summary>
        /// <param name="ex">the exception that propagated from the {@link EventHandler}.</param>
        /// <param name="sequence">sequence of the event which cause the exception.</param>
        /// <param name="event">event being processed when the exception occurred.  This can be null.</param>
        void HandleEventException(Exception ex, long sequence, Object @event);
      
        /// <summary>
        /// Callback to notify of an exception during {@link LifecycleAware#OnStart()}
        /// </summary>
        /// <param name="ex">throw during the starting process.</param>
        void HandleOnStartException(Exception ex);

        /// <summary>
        /// Callback to notify of an exception during {@link LifecycleAware#OnShutdown()}
        /// </summary>
        /// <param name="ex">throw during the shutdown process.</param>
        void HandleOnShutdownException(Exception ex);
    }
}
