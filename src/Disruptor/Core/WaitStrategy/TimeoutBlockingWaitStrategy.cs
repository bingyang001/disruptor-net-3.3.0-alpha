using System;
using System.Threading;

namespace Disruptor
{
    public sealed class TimeoutBlockingWaitStrategy : IWaitStrategy
    {
        private readonly object gate = new object();
        private /*readonly*/ TimeSpan timeOut;

        #region
        /// <summary>
        /// 
        /// </summary>
        //2014.9.13 增加无参构造
        public TimeoutBlockingWaitStrategy() 
        {
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="timeOut"></param>
        public TimeoutBlockingWaitStrategy(TimeSpan timeOut)
        {
            this.timeOut = timeOut;
        }
        #endregion

        //注意：java 版本中使用 TimeoutBlockingWaitStrategy(TimeSpan timeOut) 带参构造函数。这里重构了下
        //增加 SetTimeOut(TimeSpan timeOut) 使所有实现IWaitStrategy接口的策略保持一致的风格
        public TimeoutBlockingWaitStrategy SetTimeOut(TimeSpan timeOut)
        {
            this.timeOut = timeOut;
            return this;
        }

        public long WaitFor(long sequence, Sequence cursor, Sequence dependentSequence, ISequenceBarrier barrier)
        {
            VerifyTimeOut();
            long availableSequence;
            if ((availableSequence = cursor.Value) < sequence)
            {
                Monitor.Enter(gate);
                try
                {
                    while ((availableSequence = cursor.Value) < sequence)
                    {
                        barrier.CheckAlert();
                        var wait = Monitor.Wait(gate, timeOut);
                        if (!wait)
                        {
                            throw  TimeoutException.INSTANCE;
                        }
                    }
                }
                finally
                {
                    Monitor.Exit(gate);
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
            lock (gate)
                Monitor.PulseAll(gate);
        }

        private void VerifyTimeOut()
        {
            if (timeOut == default(TimeSpan))
                throw new ArgumentException("timeOut Invalid ???");
        }
    }
}
