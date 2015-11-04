using System;
using System.Threading;

namespace Disruptor.Utils
{
    /// <summary>
    /// Access to a ThreadFactory instance. All threads are created with setDaemon(true).
    /// </summary>
    public class DaemonThreadFactory
    {
        public Thread NewThread(Action action, bool start = false)
        {
            var th = new Thread(() => action()) { IsBackground = true };
            if (start)
            {
                th.Start();
                return th;
            }
            else
            {
                return th;
            }
        }
    }
}
