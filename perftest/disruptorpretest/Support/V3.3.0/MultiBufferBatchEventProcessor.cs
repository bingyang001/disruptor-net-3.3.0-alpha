using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Disruptor;

namespace disruptorpretest.Support.V3._3._0
{
    public class MultiBufferBatchEventProcessor<T> : IEventProcessor
    {
        private readonly _Volatile.Boolean isRunning = new _Volatile.Boolean(false);
        private readonly IDataProvider<T>[] providers;
        private readonly ISequenceBarrier[] barriers;
        private readonly IEventHandler<T> handler;
        private readonly Sequence[] sequences;
        private long count;
        public MultiBufferBatchEventProcessor(IDataProvider<T>[] providers,
                                         ISequenceBarrier[] barriers,
                                         IEventHandler<T> handler)
        {
            if (providers.Length != barriers.Length)
            {
                throw new ArgumentException();
            }

            this.providers = providers;
            this.barriers = barriers;
            this.handler = handler;

            this.sequences = new Sequence[providers.Length];
            for (int i = 0; i < sequences.Length; i++)
            {
                sequences[i] = new Sequence(-1);
            }
        }
        #region IEventProcessor 成员

        public Sequence Sequence
        {
            get { throw new NotImplementedException(); }
        }
        public Sequence[] getSequences()
        {
            return sequences;
        }
        public void Halt()
        {
            isRunning.WriteUnfenced(false);
            barriers[0].Alert();
        }
        public long getCount()
        {
            return count;
        }
        public void Run()
        {
            if (!isRunning.AtomicCompareExchange(true, false))
            {
                throw new Exception("Already running");
            }

            foreach (ISequenceBarrier barrier in barriers)
            {
                barrier.ClearAlert();
            }

            int barrierLength = barriers.Length;
            //?????????????????????
            //long[] lastConsumed = new long[barrierLength];
            //fill(lastConsumed, -1L);
            //Array.CreateInstance(typeof(long),barrierLength);
            while (true)
            {
                try
                {
                    for (int i = 0; i < barrierLength; i++)
                    {
                        long available = barriers[i].WaitFor(-1);
                        Sequence sequence = sequences[i];

                        long previous = sequence.Value;

                        for (long l = previous + 1; l <= available; l++)
                        {
                            handler.OnEvent(providers[i].Get(l), l, previous == available);
                        }

                        sequence.Value = available;

                        count += (available - previous);
                    }

                    Thread.Yield();
                }
                catch (AlertException e)
                {
                    if (!isRunning.ReadFullFence())
                    {
                        break;
                    }
                }
                catch (Disruptor.TimeoutException e)
                {
                    Console.WriteLine(e.ToString());
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                    break;
                }
            }
        }

        public bool IsRunning()
        {
            return isRunning.ReadUnfenced();
        }

        #endregion
    }
}
