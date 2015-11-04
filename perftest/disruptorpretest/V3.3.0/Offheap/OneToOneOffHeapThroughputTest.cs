using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Disruptor;

namespace disruptorpretest.V3._3._0.Offheap
{
    public class OneToOneOffHeapThroughputTest : AbstractPerfTestQueueVsDisruptor
    {
        private static readonly int BLOCK_SIZE = 256;
        private static readonly int BUFFER_SIZE = 1024 * 1024;
        private static readonly long ITERATIONS = 1000 * 1000 * 10L;
        private readonly IWaitStrategy waitStrategy = new YieldingWaitStrategy();
        public OneToOneOffHeapThroughputTest() : base(Test_Queue, ITERATIONS) { }
        protected override long RunQueuePass()
        {
            throw new NotImplementedException();
        }

        protected override long RunDisruptorPass()
        {
            throw new NotImplementedException();
        }

        protected override void ShouldCompareDisruptorVsQueues()
        {
            throw new NotImplementedException();
        }
    }

    public class OffHeapRingBuffer : IDataProvider<byte[]>
    {
        private readonly ISequencer sequencer;
        private readonly int entrySize;
        private readonly byte[] buffer;
        private readonly int mask;

        //private  ThreadLocal<ByteBuffer> perThreadBuffer = new ThreadLocal<ByteBuffer>()
        //{

        //    protected ByteBuffer initialValue()
        //    {
        //        return buffer.duplicate();
        //    }
        //};

        public OffHeapRingBuffer(ISequencer sequencer, int entrySize)
        {
            this.sequencer = sequencer;
            this.entrySize = entrySize;
            this.mask = sequencer.GetBufferSize - 1;
            buffer = new byte[sequencer.GetBufferSize * entrySize];
        }

        public void addGatingSequences(Sequence sequence)
        {
            sequencer.AddGatingSequences(sequence);
        }

        public ISequenceBarrier newBarrier()
        {
            return sequencer.NewBarrier();
        }


        public byte[] Get(long sequence)
        {
            int _index = index(sequence);
            int position = _index * entrySize;
            int limit = position + entrySize;

          
            //byteBuffer.position(position).limit(limit);

            return buffer;
        }

        public void put(byte[] data)
        {
            long next = sequencer.Next();
            try
            {
                byte[] b= Get(next);
               b =data;
            }
            finally
            {
                sequencer.Publish(next);
            }
        }

        private int index(long next)
        {
            return (int)(next & mask);
        }

        #region IDataProvider<byte[]> 成员

        public byte[] this[long sequence]
        {
            get { throw new NotImplementedException(); }
        }

        #endregion
    }
}
