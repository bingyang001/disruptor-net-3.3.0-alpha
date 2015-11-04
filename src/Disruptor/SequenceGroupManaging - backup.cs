using System;
using System.Collections.Generic;
using System.Threading;

namespace Disruptor
{
    /// <summary>
    /// Provides static methods for managing a <see cref="SequenceGroup"/> object.    
    /// <remarks> Note： in java project map SequenceGroups.java file</remarks>
    /// </summary>
    internal class SequenceGroupManaging
    {
        internal static void AddSequences<T>(
                                      T holder
                                      , _Volatile.Reference<Sequence[]> sequenceUpdater
                                      , ICursored cursor
                                      , params Sequence[] sequencesToAdd)
                                       //where T : ISequencer
        {
            long cursorSequence;
            Sequence[] currentSequences;
            Sequence[] updatedSequences;
            var casSequence = new T[1] { holder } as Sequence[];
            do
            {
                //currentSequences = sequenceUpdater.ReadUnfenced();
                currentSequences = sequenceUpdater.ReadCompilerOnlyFence();
                updatedSequences = Util.CopyToNewArray(currentSequences
                                                      , currentSequences.Length + sequencesToAdd.Length);
                cursorSequence = cursor.GetCursor;

                int index = currentSequences.Length;
                foreach (Sequence sequence in sequencesToAdd)
                {
                    sequence.LazySet(cursorSequence);
                    updatedSequences[index++] = sequence;
                }
            } 
            while(!sequenceUpdater.AtomicCompareExchange(updatedSequences,currentSequences));
            cursorSequence = cursor.GetCursor;
            foreach (Sequence sequence in sequencesToAdd)
            {
                sequence.LazySet(cursorSequence);
            }
        }
        [Obsolete("use AddSequences<T>")]
        internal static void AddSequences(
                                    AtomicReference<Sequence[]> sequenceUpdater
                                    , ICursored cursor
                                    , ref Sequence[] sharedSequence
                                    , params Sequence[] sequencesToAdd)
        {
            long cursorSequence;
            Sequence[] _updatedSequences = new Sequence[sequencesToAdd.Length];
            Sequence[] currentSequences;
            var isUp = false;
            do
            {
                currentSequences = sequenceUpdater.ReadUnfenced();
                //_updatedSequences = currentSequences;
                cursorSequence = cursor.GetCursor;
                //var index = currentSequences.Length;
                foreach (var sequence in sequencesToAdd)
                {
                    sequence.Value = cursorSequence;
                    //_updatedSequences[index++] = sequence;
                }
                _updatedSequences = sequencesToAdd;
            }
            // while (!(Interlocked.CompareExchange(ref sharedSequence, _updatedSequences, currentSequences) == currentSequences));

            //if (isUp) sharedSequence = _updatedSequences;
            while (!(isUp = sequenceUpdater.AtomicCompareExchange(_updatedSequences, currentSequences)));
            if (isUp)
                sharedSequence = sequenceUpdater.ReadUnfenced();
            cursorSequence = cursor.GetCursor;
            foreach (Sequence sequence in sequencesToAdd)
            {
                sequence.Value = cursorSequence;
            }
        }

        internal static bool RemoveSequence<T>(
                                      T holder
                                      , _Volatile.Reference<Sequence[]> sequenceUpdater
                                      , Sequence sequence)
                                      //where T : ISequencer
        {
            int numToRemove;
            Sequence[] oldSequences;
            Sequence[] newSequences;
            var casSequence = new T[1] { holder } as Sequence[];
            do
            {
                oldSequences = sequenceUpdater.ReadUnfenced();
                numToRemove = CountMatching(oldSequences, sequence);
                if (0 == numToRemove)
                {
                    break;
                }
                int oldSize = oldSequences.Length;
                newSequences = new Sequence[oldSize - numToRemove];
                for (int i = 0, pos = 0; i < oldSize; i++)
                {
                    Sequence testSequence = oldSequences[i];
                    if (sequence != testSequence)
                    {
                        newSequences[pos++] = testSequence;
                    }
                }
            } 
            while (!sequenceUpdater.AtomicCompareExchange(newSequences,oldSequences))   ;

            return numToRemove != 0;
        }

        [Obsolete("use RemoveSequence<T>")]
        internal static bool RemoveSequence(
                                        AtomicReference<Sequence[]> sequenceUpdater,
                                        ref Sequence[] sharedSequence, Sequence sequence)
        {
            int numToRemove;
            Sequence[] oldSequences;
            Sequence[] newSequences;
            var isUp = false;
            do
            {
                oldSequences = sequenceUpdater.ReadUnfenced();//.ReadCompilerOnlyFence();

                numToRemove = CountMatching(oldSequences, sequence);
                if (0 == numToRemove)
                {
                    break;
                }

                int oldSize = oldSequences.Length;
                newSequences = new Sequence[oldSize - numToRemove];

                for (int i = 0, pos = 0; i < oldSize; i++)
                {
                    Sequence testSequence = oldSequences[i];
                    if (sequence != testSequence)
                    {
                        newSequences[pos++] = testSequence;
                    }
                }

            }
            // while (!(isUp = (Interlocked.CompareExchange(ref sharedSequence, newSequences, oldSequences) == oldSequences)));

            while (!(isUp = sequenceUpdater.AtomicCompareExchange(newSequences, oldSequences)));
            if (isUp)
                sharedSequence = sequenceUpdater.ReadUnfenced();
            return numToRemove != 0;
        }

        private static int CountMatching<T>(T[] values, T toMatch)
        {
            int numToRemove = 0;
            foreach (T value in values)
            {
                if (value.Equals(toMatch)) // Specifically uses identity
                {
                    numToRemove++;
                }
            }
            return numToRemove;
        }
    }
}
