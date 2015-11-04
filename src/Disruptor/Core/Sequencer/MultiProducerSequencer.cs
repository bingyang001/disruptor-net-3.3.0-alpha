using System;
using System.Linq;
using System.Threading;

namespace Disruptor
{
    /// <summary>
    /// Coordinator for claiming sequences for access to a data structure while tracking dependent {@link Sequence}s
    /// Suitable for use for sequencing across multiple publisher threads.
    /// </summary>
    public class MultiProducerSequencer : AbstractSequencer
    {
        private const int BASE = 12;
        private const int SCALE = 4;
        private readonly Sequence gatingSequenceCache = new Sequence(InitialCursorValue.INITIAL_CURSOR_VALUE);
        private _Volatile.IntegerArray pendingPublication;
        private readonly int _pendingMask;
        // availableBuffer tracks the state of each ringbuffer slot
        // see below for more details on the approach
        //private readonly int[] availableBuffer;
        private readonly int indexMask;
        private readonly int indexShift;
        private _Volatile.Boolean isClaim = new _Volatile.Boolean(false);

        public MultiProducerSequencer(int bufferSize, IWaitStrategy waitStrategy)
            : base(bufferSize, waitStrategy)
        {
            indexMask = bufferSize - 1;
            indexShift = Util.Log2(bufferSize);
            pendingPublication = new _Volatile.IntegerArray(bufferSize);
            _pendingMask = bufferSize - 1;
            InitialiseAvailableBuffer();
        }

        private void InitialiseAvailableBuffer()
        {
            for (int i = pendingPublication.Length - 1; i != 0; i--)
            {
                SetAvailableBufferValue(i, -1);
            }

            SetAvailableBufferValue(0, -1);
        }

        public override bool HasAvailableCapacity(int requiredCapacity)
        {
            return HasAvailableCapacity(sequencesRef.ReadFullFence (), requiredCapacity, cursor.Value);
        }

        public override void Claim(long sequence)
        {
            isClaim.WriteReleaseFence(true);
            cursor.Value = sequence;
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

            //long current;
            //long next;
            var spinWait = default(SpinWait);
            do
            {
                var current = cursor.Value;
                var next = current + n;
                //var swp = true;
                var wrapPoint = next - GetBufferSize;
                var cachedGatingSequence = gatingSequenceCache.Value;

                if (wrapPoint > cachedGatingSequence || cachedGatingSequence > current)
                {
                    var gatingSequence = Util.GetMinimumSequence(sequencesRef.ReadFullFence(), current);

                    if (wrapPoint > gatingSequence)
                    {
                        // TODO, should we spin based on the wait strategy? 
                        // java version use  LockSupport.parkNanos(1);                  
                        spinWait.SpinOnce();
                        continue;
                    }

                    gatingSequenceCache.Value = gatingSequence;
                }
                else if (cursor.CompareAndSet(current, next))
                {                   
                    return next;
                }             
            }
            while (true);                     
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

            long current;
            long next;

            do
            {
                current = cursor.Value;
                next = current + n;

                if (!HasAvailableCapacity(this.sequencesRef.ReadFullFence(), n, current))
                {
                    throw InsufficientCapacityException.INSTANCE;
                }
            }
            while (!cursor.CompareAndSet(current, next));

            return next;
        }

        public override long RemainingCapacity()
        {
            var consumed = Util.GetMinimumSequence(sequencesRef.ReadFullFence(), cursor.Value);
            var produced = cursor.Value;
            return GetBufferSize - (produced - consumed);
        }

        public override void Publish(long sequence)
        {
            SetAvailable(sequence);
            waitStrategy.SignalAllWhenBlocking();
        }

        public override void Publish(long lo, long hi)
        {
            for (var l = lo; l <= hi; l++)
            {
                SetAvailable(l);
            }
            waitStrategy.SignalAllWhenBlocking();
        }

        public override bool IsAvailable(long sequence)
        {
            int index = calculateIndex(sequence);
            int flag = CalculateAvailabilityFlag(sequence);

            return pendingPublication.ReadFullFence(index) == flag;
        }

        public override long GetHighestPublishedSequence(long lowerBound, long availableSequence)
        {
            for (var sequence = lowerBound; sequence <= availableSequence; sequence++)
            {
                if (!IsAvailable(sequence))
                {
                    return sequence - 1;
                }
            }

            return availableSequence;
        }

        private bool HasAvailableCapacity(Sequence[] gatingSequences, int requiredCapacity, long cursorValue)
        {
            var wrapPoint = (cursorValue + requiredCapacity) - bufferSize;
            var cachedGatingSequence = gatingSequenceCache.Value;

            if (wrapPoint > cachedGatingSequence || cachedGatingSequence > cursorValue)
            {
                var minSequence = Util.GetMinimumSequence(sequencesRef.ReadFullFence(), cursorValue);
                gatingSequenceCache.Value = minSequence;

                if (wrapPoint > minSequence)
                {
                    return false;
                }
            }

            return true;
        }

        private void SetAvailable(long sequence)
        {
            SetAvailableBufferValue(calculateIndex(sequence), CalculateAvailabilityFlag(sequence));
        }

        private void SetAvailableBufferValue(int index, int flag)
        {
            pendingPublication.WriteFullFence(index, flag);
        }

        private int CalculateAvailabilityFlag(long sequence)
        {
            return (int)(sequence >>= indexShift);
        }

        private int calculateIndex(long sequence)
        {
            return ((int)sequence) & indexMask;
        }

        //private void MarkSequences(long next)
        //{
        //    if (!isClaim.ReadFullFence())
        //    {
        //        gatingSequences = new Sequence[] { new Sequence(next) };
        //    }
        //}

    }
}
