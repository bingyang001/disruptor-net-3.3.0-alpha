using System;
using System.Threading;

namespace Disruptor
{
   public abstract class SingleProducerSequencerPad : AbstractSequencer
    {
        protected long p1, p2, p3, p4, p5, p6, p7;
        public SingleProducerSequencerPad(int bufferSize, IWaitStrategy waitStrategy)
            : base(bufferSize, waitStrategy)
        {

        }
    }

   public abstract class SingleProducerSequencerFields : SingleProducerSequencerPad
    {
        public SingleProducerSequencerFields(int bufferSize, IWaitStrategy waitStrategy)
            : base(bufferSize, waitStrategy)
        {

        }

        /** Set to -1 as sequence starting point */
        protected long nextValue = InitialCursorValue.INITIAL_CURSOR_VALUE;
        protected long cachedValue = InitialCursorValue.INITIAL_CURSOR_VALUE;
    }
    /// <summary>
    ///  <p>Coordinator for claiming sequences for access to a data structure while tracking dependent {@link Sequence}s.</p>   
    ///  <p>Generally not safe for use from multiple threads as it does not implement any barriers.</p>
    /// </summary>
    public sealed class SingleProducerSequencer : SingleProducerSequencerFields
    {        
        [Obsolete("unused")]
        private class Padding
        {
            public long
                nextValue = InitialCursorValue.INITIAL_CURSOR_VALUE
                , cachedValue = InitialCursorValue.INITIAL_CURSOR_VALUE
                , p2// = INITIAL_CURSOR_VALUE
                , p3// = INITIAL_CURSOR_VALUE
                , p4// = INITIAL_CURSOR_VALUE
                , p5// = INITIAL_CURSOR_VALUE
                , p6// = INITIAL_CURSOR_VALUE
                , p7;// = INITIAL_CURSOR_VALUE
        }
       
        /// <summary>
        /// 源代码为：和.net区别很大，java静态类可以被实例化
        /// 例如：
        /// <p>private final RingBuffer LongEvent ringBuffer =RingBuffer.CreateSingleProducer(LongEvent.FACTORY,BUFFER_SIZE);</p>
        /// <p><code>@SuppressWarnings("unused")
        ///private static class Padding
        ///{
        ///    public Padding(){
        ///        System.out.println("aaaa");
        ///    }
        ///   /** Set to -1 as sequence starting point */
        ///   public long nextValue = Sequence.INITIAL_VALUE, cachedValue = Sequence.INITIAL_VALUE, p2, p3, p4, p5, p6, p7;
        ///}</code>
        ///</p>
        ///<p><code>private  final  Padding pad = new Padding();</code></p>
        /// </summary>
        //private readonly Padding pad = new Padding();
        
        /// <summary>
        ///  Construct a Sequencer with the selected wait strategy and buffer size.
        /// </summary>
        /// <param name="bufferSize">bufferSize the size of the buffer that this will sequence over.</param>
        /// <param name="waitStrategy">waitStrategy for those waiting on sequences.</param>
        public SingleProducerSequencer(int bufferSize, IWaitStrategy waitStrategy)
            : base(bufferSize, waitStrategy)
        {

        }

        /// <summary>
        /// <see cref="Sequencer#HasAvailableCapacity(int)"/>
        /// </summary>
        /// <param name="requiredCapacity"></param>         
        /// <returns></returns>
        public override bool HasAvailableCapacity(int requiredCapacity)
        {
            long nextValue = this.nextValue;

            long wrapPoint = (nextValue + requiredCapacity) - bufferSize;
            long cachedGatingSequence = this.cachedValue;

            if (wrapPoint > cachedGatingSequence || cachedGatingSequence > nextValue)
            {
                long minSequence = Util.GetMinimumSequence(/*sequencesRef.ReadFullFence()*/sequencesRef.ReadCompilerOnlyFence(), nextValue);
                this.cachedValue = minSequence;
               
                if (wrapPoint > minSequence)
                {
                    return false;
                }
            }

            return true;
        }

        public override long Next()
        {
            return Next(1);
        }

        public override long Next(int n)
        {
            if (n < 1)
            {
                throw new ArgumentOutOfRangeException("n must be > 0");
            }
            SpinWait spinWait = new SpinWait();
            long nextValue = this.nextValue;

            long nextSequence = nextValue + n;
            long wrapPoint = nextSequence - bufferSize;
            long cachedGatingSequence = this.cachedValue;

            if (wrapPoint > cachedGatingSequence || cachedGatingSequence > nextValue)
            {
                long minSequence;
                while (wrapPoint > (minSequence = Util.GetMinimumSequence(/*sequencesRef.ReadFullFence()*/sequencesRef.ReadCompilerOnlyFence(), nextValue)))
                {
                    spinWait.SpinOnce();
                }
                this.cachedValue = minSequence;
            }
            this.nextValue = nextSequence;

            return nextSequence;
        }

        public override long TryNext()
        {
            return TryNext(1);
        }

        public override long TryNext(int n)
        {
            if (n < 1)
            {
                throw new ArgumentOutOfRangeException("n must be > 0");
            }
            if (!HasAvailableCapacity(n))
            {              
                throw InsufficientCapacityException.INSTANCE;
            }
            long nextSequence = this.nextValue += n;
           
            return nextSequence;
        }

        public override long RemainingCapacity()
        {
            long nextValue = this.nextValue;

            long consumed = Util.GetMinimumSequence(/*sequencesRef.ReadFullFence()*/sequencesRef.ReadCompilerOnlyFence(), nextValue);
            long produced = nextValue;
            return GetBufferSize - (produced - consumed);
        }

        public override void Claim(long sequence)
        {
            this.nextValue = sequence;
        }

        public override void Publish(long sequence)
        {
            cursor.Value = sequence;
            waitStrategy.SignalAllWhenBlocking();
        }

        public override void Publish(long lo, long hi)
        {
            Publish(hi);
        }

        public override bool IsAvailable(long sequence)
        {
            return sequence <= cursor.Value;
        }

        public override long GetHighestPublishedSequence(long lowerBound, long availableSequence)
        {
            return availableSequence;
        }
    }
}
