using System;
using System.Threading;

namespace Disruptor
{
    /// <summary>
    /// Base class for the various sequencer types (single/multi).  Provides
    /// common functionality like the management of gating sequences (add/remove) and
    /// ownership of the current cursor.
    /// </summary>
    public abstract class AbstractSequencer : ISequencer
    {
        protected _Volatile.AtomicReference<Sequence[]> sequencesRef = 
        new _Volatile.AtomicReference<Sequence[]>(new Sequence[0]);
        protected readonly int bufferSize;
        protected readonly IWaitStrategy waitStrategy;
        protected readonly Sequence cursor = new Sequence(InitialCursorValue.INITIAL_CURSOR_VALUE);
        //protected volatile Sequence[] gatingSequences = new Sequence[0];

        /// <summary>
        /// Create with the specified buffer size and wait strategy.
        /// </summary>
        /// <param name="bufferSize">The total number of entries, must be a positive power of 2.</param>
        /// <param name="waitStrategy">waitStrategy</param>
        public AbstractSequencer(int bufferSize, IWaitStrategy waitStrategy)
        {
            Verify(bufferSize);
            //this.sequencesRef = new _Volatile.AtomicReference<Sequence[]>(gatingSequences);
            this.bufferSize = bufferSize;
            this.waitStrategy = waitStrategy;
        }

        public int GetBufferSize
        {
            get { return bufferSize; }
        }

        public void AddGatingSequences(params Sequence[] gatingSequencess)
        {
            SequenceGroupManaging.AddSequences(this, sequencesRef, this/*,ref gatingSequences*/, gatingSequencess);
            //SequenceGroupManaging.AddSequences(sequencesRef,this,ref gatingSequences, gatingSequencess);          
        }

        public bool RemoveGatingSequence(Sequence sequence)
        {
            return SequenceGroupManaging.RemoveSequence(this, sequencesRef, sequence);
            //return SequenceGroupManaging.RemoveSequence(sequencesRef,ref gatingSequences, sequence);
        }

        public ISequenceBarrier NewBarrier(params Sequence[] sequencesToTrack)
        {
            return new ProcessingSequenceBarrier(this, waitStrategy, cursor, sequencesToTrack);
        }

        public long GetMinimumSequence()
        {
            return Util.GetMinimumSequence(sequencesRef.ReadFullFence(), cursor.Value);
        }

        public long GetCursor
        {
            get { return cursor.Value; }
        }

        protected void Verify(int bufferSize)
        {
            if (bufferSize < 1)
            {
                throw new ArgumentOutOfRangeException("bufferSize must not be less than 1");
            }
            if (!bufferSize.IsPowerOf2())
            {
                throw new ArgumentOutOfRangeException("bufferSize must be a power of 2");
            }
        }

        public abstract bool HasAvailableCapacity(int requiredCapacity);

        public abstract long Next();

        public abstract long Next(int n);

        public abstract long TryNext();

        public abstract long TryNext(int n);

        public abstract long RemainingCapacity();

        public abstract void Claim(long sequence);

        public abstract void Publish(long sequence);

        public abstract void Publish(long lo, long hi);

        public abstract bool IsAvailable(long sequence);

        public abstract long GetHighestPublishedSequence(long sequence, long availableSequence);

        public virtual EventPoller<T> NewPoller<T>(IDataProvider<T> dataProvider, params Sequence[] gatingSequences)
        {
            return EventPoller<T>.NewInstance(dataProvider, this, new Sequence(), cursor, gatingSequences);
        }
    }
}
