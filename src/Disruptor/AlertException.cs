using System;
namespace Disruptor
{
    /// <summary>
    /// Used to alert <see cref="IEventProcessor"/>s waiting at a <see cref="ISequenceBarrier"/> of status changes.
    /// </summary>
    public sealed class AlertException : Exception
    {
        /// <summary>
        /// Pre-allocated exception to avoid garbage generation
        /// </summary>
        public static readonly AlertException instance = new AlertException();

        /// <summary>
        /// Private constructor so only a single instance exists.
        /// </summary>
        private AlertException()
        {
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Exception FillInStackTrace()
        {
            return this;
        }
    }
}
