using System.Threading.Tasks;

namespace Disruptor.Dsl
{
    internal class WorkerPoolInfo<T> : IConsumerInfo where T : class
    {
        private readonly WorkerPool<T> workerPool;
        private readonly ISequenceBarrier sequenceBarrier;
        private bool endOfChain = true;
        
        public WorkerPoolInfo(WorkerPool<T> workerPool, ISequenceBarrier sequenceBarrier)
        {
            this.workerPool = workerPool;
            this.sequenceBarrier = sequenceBarrier;
        }


        public Sequence[] GetSequences
        {
            get { return workerPool.getWorkerSequences(); }
        }

        public ISequenceBarrier GetBarrier
        {
            get { return sequenceBarrier; }
        }

        public bool IsEndOfChain
        {
            get { return endOfChain; }
        }

        public void Start(TaskScheduler taskScheduler)
        {
            workerPool.start(taskScheduler);
        }

        public void Halt()
        {
            workerPool.halt();
        }

        public void MarkAsUsedInBarrier()
        {
            endOfChain = false;
        }

        public bool IsRunning
        {
            get { return workerPool.IsRunning(); }
        }
    }
}
