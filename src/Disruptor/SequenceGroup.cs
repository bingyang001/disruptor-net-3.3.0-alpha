using System;
using System.Threading;

namespace Disruptor
{
    /// <summary>
    /// A <see cref="Sequence"/>  group that can dynamically have <see cref="Sequence"/> s added and removed while being
    /// thread safe.
    /// <p>
    ///The <see cref="SequenceGroup.Value"/> methods are lock free and can be
    /// concurrently be called with the <see cref="SequenceGroup.Add(Sequence)"/> and <see cref="SequenceGroup.Remove(Sequence)"/>.</p>
    /// </summary>
    public class SequenceGroup : Sequence
    {
        private  _Volatile.AtomicReference<Sequence[]> sequencesRef;
        private volatile Sequence[] sequences = new Sequence[0];
            
        public SequenceGroup()
            : base(-1)
        {
            sequencesRef = new _Volatile.AtomicReference<Sequence[]>(sequences); 
        }

        /// <summary>
        /// Current sequence number
        /// </summary>
        public override long Value
        {
            get { return Util.GetMinimumSequence(sequencesRef.ReadFullFence()); }
            set
            {
                var sequences = sequencesRef.ReadFullFence();
                for (int i = 0; i < sequences.Length; i++)
                {
                    sequences[i].Value = value;
                }
            }
        }

        /// <summary>
        /// Add a <see cref="Sequence"/> into this aggregate.  This should only be used during
        /// initialisation.  Use <see cref="SequenceGroup.AddWhileRunning(Cursored, Sequence)"/> 
        /// </summary>
        /// <param name="sequence"></param>
        public void Add(Sequence sequence)
        {
            Sequence[] oldSequences;
            Sequence[] newSequences;
            do
            {
                oldSequences = sequencesRef.ReadFullFence();
                var oldSize = oldSequences.Length;
                newSequences = new Sequence[oldSize + 1];
                Array.Copy(oldSequences, newSequences, oldSize);
                newSequences[oldSize] = sequence;
            }
            while (!sequencesRef.AtomicCompareExchange(newSequences, oldSequences));
        }

        /// <summary>
        /// Remove the first occurrence of the <see cref="Sequence"/> from this aggregate.
        /// </summary>
        /// <param name="sequence"> to be removed from this aggregate.</param>
        /// <returns>true if the sequence was removed otherwise false.</returns>
        public bool remove(Sequence sequence)
        {
            return SequenceGroupManaging.RemoveSequence(this, sequencesRef/*,ref sequences*/, sequence);
        }

        /// <summary>
        /// Get the size of the group.
        /// </summary>
        public int Size
        {
            get { return sequencesRef.ReadFullFence().Length; }
        }

        /// <summary>
        /// Adds a sequence to the sequence group after threads have started to Publish to
        /// the Disruptor.  It will set the sequences to cursor value of the ringBuffer
        /// just after adding them.  This should prevent any nasty rewind/wrapping effects.
        /// </summary>
        /// <param name="cursored">The data structure that the owner of this sequence group will be pulling it's events from.</param>
        /// <param name="sequence">The sequence to add.</param>
        public void AddWhileRunning(ICursored cursored, Sequence sequence)
        {
            SequenceGroupManaging.AddSequences(this, sequencesRef, cursored, sequence);           
        }
    }
}
