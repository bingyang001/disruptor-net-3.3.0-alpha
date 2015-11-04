using System;
using System.Threading;

namespace Disruptor.WaitStrategy
{
    public class LiteBlockingWaitStrategy : IWaitStrategy
    {
        private _Volatile.Boolean signalNeeded = new _Volatile.Boolean(false);
        private static readonly object obj = new object();

        public long WaitFor(long sequence, Sequence cursor, Sequence dependentSequence, ISequenceBarrier barrier)
        {
            long availableSequence;
            if ((availableSequence = dependentSequence.Value) < sequence)
            {
                bool lockToken = false;
                Monitor.Enter(obj, ref lockToken);
                try
                {
                    do
                    {
                        signalNeeded.WriteFullFence(true);

                        if ((availableSequence = dependentSequence.Value) >= sequence)
                        {
                            break;
                        }

                        barrier.CheckAlert();
                        Monitor.Wait(obj);
                    }
                    while ((availableSequence = dependentSequence.Value) < sequence);
                }
                finally
                {
                    if (lockToken)
                        Monitor.Exit(obj);
                }
            }

            while ((availableSequence = dependentSequence.Value) < sequence)
            {
                barrier.CheckAlert();
            }

            return availableSequence;
        }

        public void SignalAllWhenBlocking()
        {
            if (signalNeeded.AtomicCompareExchange(false, true))
            {
                bool lockToken = false;
                Monitor.Enter(obj, ref lockToken);
                try
                {
                    Monitor.Pulse(obj);                   
                }
                finally
                {
                    if (lockToken)
                        Monitor.Exit(obj);
                }
            }
        }
    }
}
