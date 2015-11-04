using System;
using System.Threading;

namespace Disruptor
{

    /// <summary>
    /// Blocking strategy that uses a lock and condition variable for <see cref="IEventProcessor"/>s waiting on a barrier.
    /// 
    /// This strategy should be used when performance and low-latency are not as important as CPU resource.
    /// </summary>
    public sealed class BlockingWaitStrategy : IWaitStrategy
    {
        private readonly object gate = new object();
        //不要把spinlock定义为readonly 因为在操作锁的时候，他的内部状态必须改变
        private SpinLock spinLock = new SpinLock(false);
        private readonly ManualResetEventSlim mres = new ManualResetEventSlim(false);

        /// <summary>
        /// Wait for the given sequence to be available
        /// </summary>
        /// <param name="sequence">sequence to be waited on.</param>
        /// <param name="cursor">Ring buffer cursor on which to wait.</param>
        /// <param name="dependents">dependents further back the chain that must advance first</param>
        /// <param name="barrier">barrier the <see cref="IEventProcessor"/> is waiting on.</param>
        /// <returns>the sequence that is available which may be greater than the requested sequence.</returns>
        public long WaitFor(long sequence, Sequence cursor, Sequence dependentSequence, ISequenceBarrier barrier)
        {           
            var availableSequence = cursor.Value; // volatile read
            if (availableSequence < sequence)
            {
                //SpinLock 与 Monitor相比是个轻量级的同步原语
                //实际测试下来使用 SpinLock 性能优于Monitor
                //Monitor.Enter(gate);
                var lockToken = false; 
                spinLock.Enter(ref lockToken);
                try
                {
                    while ((availableSequence = cursor.Value) < sequence) // volatile read
                    {
                        barrier.CheckAlert();
                        //Monitor.Wait(gate);
                        mres.Wait();
                    }
                }
                finally
                {
                    //Monitor.Exit(gate);
                    if (lockToken)
                        spinLock.Exit();
                }
            }
            while ((availableSequence = dependentSequence.Value) < sequence)
            {
                barrier.CheckAlert();
            }
            return availableSequence;
        }

        /// <summary>
        /// Signal those <see cref="IEventProcessor"/> waiting that the cursor has advanced.
        /// </summary>
        public void SignalAllWhenBlocking()
        {
            lock (gate)
                //Monitor.PulseAll(gate);
                mres.Set();
        }
    }
}
