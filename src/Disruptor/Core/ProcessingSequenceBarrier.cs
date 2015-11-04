namespace Disruptor
{
    /// <summary>
    ///  http://ifeve.com/disruptor-writing-ringbuffer/
    ///  生产者序列 。
    ///  写入 Ring Buffer 的过程涉及到两阶段提交 (two-phase commit)。
    ///  首先，你的生产者需要申请 buffer 里的下一个节点。
    ///  然后，当生产者向节点写完数据，它将会调用 ProducerBarrier 的 commit 方法。
    ///  <see cref="ISequenceBarrier"/>handed out for gating <see cref="EventProcessor"/>s on a cursor sequence and optional dependent {@link EventProcessor}(s),
    ///  using the given WaitStrategy.
    /// </summary>
    public class ProcessingSequenceBarrier : ISequenceBarrier
    {
        private readonly IWaitStrategy waitStrategy;
        private readonly Sequence dependentSequence;
        private volatile bool alerted = false;
        private readonly Sequence cursorSequence;
        private readonly ISequencer sequencer;

        public ProcessingSequenceBarrier(
                                     ISequencer sequencer,
                                     IWaitStrategy waitStrategy,
                                     Sequence cursorSequence,
                                     Sequence[] dependentSequences)
        {
            this.sequencer = sequencer;
            this.waitStrategy = waitStrategy;
            this.cursorSequence = cursorSequence;
            if (dependentSequences.Length == 0)
                this.dependentSequence = cursorSequence;
            else
                this.dependentSequence = new FixedSequenceGroup(dependentSequences);
        }

        public long WaitFor(long sequence)
        {
            CheckAlert();

            var availableSequence = waitStrategy.WaitFor(sequence, cursorSequence, dependentSequence, this);

            if (availableSequence < sequence)
            {
                return availableSequence;
            }

            return sequencer.GetHighestPublishedSequence(sequence, availableSequence);
        }

        public long GetCursor
        {
            get { return cursorSequence.Value; }
        }

        public bool IsAlerted
        {
            get { return alerted; }
        }

        public void Alert()
        {
            alerted = true;
            waitStrategy.SignalAllWhenBlocking();
        }


        public void ClearAlert()
        {
            alerted = false;
        }
       
        public void CheckAlert()
        {
            if (alerted)
            {
                throw AlertException.instance;
            }
        }
    }
}
