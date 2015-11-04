using System;
using System.Threading;

namespace Disruptor
{   
    /// <summary>
    /// No operation version of a {@link EventProcessor} that simply tracks a {@link Sequence}.
    /// This is useful in tests or for pre-filling a {@link RingBuffer} from a publisher.
    /// </summary>
    public sealed class NoOpEventProcessor<T> : IEventProcessor where T : class
    {
        private readonly SequencerFollowingSequence sequence;
        private _Volatile.Boolean running = new _Volatile.Boolean(false);

        /// <summary>
        /// Construct a {@link EventProcessor} that simply tracks a {@link Sequence} object.
        /// </summary>
        /// <param name="sequencer">to track.</param>
        public NoOpEventProcessor(RingBuffer<T> sequencer)
        {
            sequence = new SequencerFollowingSequence(sequencer);
        }

        public Sequence Sequence
        {
            get { return sequence; }
        }

        public void Halt()
        {
            running.WriteFullFence(false);
        }

        public void Run()
        {
            if (!running.AtomicCompareExchange(true, false))
            {
                throw new Exception("Thread is already running");
            }
        }

        public bool IsRunning()
        {
            return running.ReadFullFence();
        }

        /// <summary>
        /// Sequence that follows (by wrapping) another sequence
        /// </summary>
        private sealed class SequencerFollowingSequence : Sequence
        {
            private readonly RingBuffer<T> sequencer;           
            public SequencerFollowingSequence(RingBuffer<T> sequencer)
                : base(InitialCursorValue.INITIAL_CURSOR_VALUE)
            {

                this.sequencer = sequencer;
            }


            public override long  Value
            {
                get { return sequencer.GetCursor; }
            }
        }
    }
}
