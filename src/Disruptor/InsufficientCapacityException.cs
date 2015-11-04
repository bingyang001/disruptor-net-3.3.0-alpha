using System;
namespace Disruptor
{
    /// <summary>
    /// <p>Exception thrown when the it is not possible to insert a value into
    /// the ring buffer without it wrapping the consuming sequenes.  Used
    /// specifically when claiming with the {@link RingBuffer#tryNext()} call.
    ///
    /// <p>For efficiency this exception will not have a stack trace.
    /// </summary>
    public sealed class InsufficientCapacityException : Exception
    {
        public static readonly InsufficientCapacityException INSTANCE = new InsufficientCapacityException();

        private InsufficientCapacityException()
        {
            // Singleton
        }


        public Exception fillInStackTrace()
        {
            return this;
        }
    }
}
