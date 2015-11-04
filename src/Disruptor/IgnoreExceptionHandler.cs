using System;

namespace Disruptor
{
    /// <summary>
    /// Convenience implementation of an exception handler that using Console.WriteLine to log
    /// the exception
    /// </summary>
    public class IgnoreExceptionHandler : IExceptionHandler
    {
        public IgnoreExceptionHandler()
        {

        }
       
        public void HandleEventException(Exception ex, long sequence, Object @event)
        {
            var message = string.Format("Exception processing sequence {0} for event {1}: {2}", sequence, @event, ex);

            Console.WriteLine(message);
        }
       
        public void HandleOnStartException(Exception ex)
        {
            var message = string.Format("Exception during OnStart(): {0}", ex);

            Console.WriteLine(message);
        }
        
        public void HandleOnShutdownException(Exception ex)
        {
            var message = string.Format("Exception during OnShutdown(): {0}", ex);

            Console.WriteLine(message);
        }
    }
}
