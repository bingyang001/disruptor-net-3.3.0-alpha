using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Disruptor
{
    public sealed class TimeoutException : Exception
    {
        public static readonly TimeoutException INSTANCE = new TimeoutException();

        private TimeoutException()
        {
            // Singleton
        }
        public Exception fillInStackTrace()
        {
            return this;
        }
    }
}
