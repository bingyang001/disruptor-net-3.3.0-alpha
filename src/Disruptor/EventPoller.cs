using System;
using System.Collections.Generic;

namespace Disruptor
{
    /// <summary>
    /// Experimental poll-based interface for the Disruptor.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class EventPoller<T>
    {
        private readonly IDataProvider<T> dataProvider;
        private readonly ISequencer sequencer;
        private readonly Sequence sequence;
        private readonly Sequence gatingSequence;

        public interface IHandler<T>
        {
            bool OnEvent(T @event, long sequence, bool endOfBatch);
        }

        public enum PollState
        {
            PROCESSING, GATING, IDLE
        }

        public EventPoller(IDataProvider<T> dataProvider,
                       ISequencer sequencer,
                       Sequence sequence,
                       Sequence gatingSequence)
        {
            this.dataProvider = dataProvider;
            this.sequencer = sequencer;
            this.sequence = sequence;
            this.gatingSequence = gatingSequence;
        }

        public PollState Poll(IHandler<T> eventHandler)
        {
            long currentSequence = sequence.Value;
            long nextSequence = currentSequence + 1;
            long availableSequence = sequencer.GetHighestPublishedSequence(nextSequence, gatingSequence.Value);

            if (nextSequence <= availableSequence)
            {
                bool processNextEvent;
                long processedSequence = currentSequence;

                try
                {
                    do
                    {
                        T @event = dataProvider.Get(nextSequence);
                        processNextEvent = eventHandler.OnEvent(@event, nextSequence, nextSequence == availableSequence);
                        processedSequence = nextSequence;
                        nextSequence++;

                    }
                    while (nextSequence <= availableSequence & processNextEvent);
                }
                finally
                {
                    sequence.Value = processedSequence;
                }

                return PollState.PROCESSING;
            }
            else if (sequencer.GetCursor >= nextSequence)
            {
                return PollState.GATING;
            }
            else
            {
                return PollState.IDLE;
            }
        }

        public static EventPoller<T> NewInstance(IDataProvider<T> dataProvider,
                                                  ISequencer sequencer,
                                                  Sequence sequence,
                                                  Sequence cursorSequence,
                                                  params Sequence[] gatingSequences)
        {
            Sequence gatingSequence;
            if (gatingSequences.Length == 0)
            {
                gatingSequence = cursorSequence;
            }
            else if (gatingSequences.Length == 1)
            {
                gatingSequence = gatingSequences[0];
            }
            else
            {
                gatingSequence = new FixedSequenceGroup(gatingSequences);
            }

            return new EventPoller<T>(dataProvider, sequencer, sequence, gatingSequence);
        }

        public Sequence GetSequence()
        {
            return sequence;
        }
    }
}
