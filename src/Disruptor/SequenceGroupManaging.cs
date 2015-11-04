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
                                      ,  _Volatile.AtomicReference<Sequence[]> sequenceUpdater
                                      , ICursored cursor
                                      , params Sequence[] sequencesToAdd)                                      
        {
            long cursorSequence;
            Sequence[] currentSequences;
            Sequence[] updatedSequences;
            //var casSequence = new T[1] { holder } as Sequence[];
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
      
        internal static bool RemoveSequence<T>(
                                      T holder
                                      , _Volatile.AtomicReference<Sequence[]> sequenceUpdater
                                      , Sequence sequence)                                     
        {
            int numToRemove;
            Sequence[] oldSequences;
            Sequence[] newSequences;
            //var casSequence = new T[1] { holder } as Sequence[];
            do
            {
                 //oldSequences = sequenceUpdater.ReadUnfenced();
                oldSequences = sequenceUpdater.ReadCompilerOnlyFence ();
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
