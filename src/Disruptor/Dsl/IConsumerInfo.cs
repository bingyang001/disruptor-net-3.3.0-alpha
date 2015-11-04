using System.Threading.Tasks;

namespace Disruptor.Dsl
{
    public interface IConsumerInfo
    {
        Sequence[] GetSequences { get; }

        ISequenceBarrier GetBarrier { get; }

        bool IsEndOfChain{get;}

        void Start(TaskScheduler taskScheduler);

        void Halt();

        void MarkAsUsedInBarrier();

        bool IsRunning { get; }
    }
}
