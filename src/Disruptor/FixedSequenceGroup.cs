using System;

namespace Disruptor
{
    /// <summary>
    /// Hides a group of Sequences behind a single Sequence
    /// </summary>
    public sealed class FixedSequenceGroup : Sequence
    {
        private readonly Sequence[] sequences;
        public FixedSequenceGroup(Sequence[] sequences)
        {
            this.sequences = sequences;
            //Arrays.copyOf(sequences, sequences.length);
        }
        
        /// <summary>
        /// Get the minimum sequence value for the group.
        /// </summary>
        /// <remarks> the minimum sequence value for the group.</remarks>
        public override long Value
        {
            get { return Util.GetMinimumSequence(sequences); }
            set { throw new NotImplementedException(); }
        }
       
        public override string ToString()
        {
            return base.ToString();
        }

        public override bool CompareAndSet(long expectedValue, long newValue)
        {
            throw new NotImplementedException();
        }
        
        public override long IncrementAndGet()
        {
            throw new NotImplementedException();
        }
        
        public override long AddAndGet(long increment)
        {
            throw new NotImplementedException();
        }
    }
}
