using System;
using System.Threading;
using System.Threading.Tasks;

namespace Disruptor
{
    /// <summary>
    /// WorkerPool contains a pool of {@link WorkProcessor}s that will consume sequences so jobs can be farmed out across a pool of workers.
    /// Each of the {@link WorkProcessor}s manage and calls a {@link WorkHandler} to process the events.
    /// </summary>
    /// <typeparam name="T">to be processed by a pool of workers</typeparam>
    public sealed class WorkerPool<T> where T : class
    {
        private _Volatile.Boolean started = new _Volatile.Boolean(false);
        private readonly Sequence workSequence = new Sequence(InitialCursorValue.INITIAL_CURSOR_VALUE);
        private readonly RingBuffer<T> ringBuffer;
        // WorkProcessors are created to wrap each of the provided WorkHandlers
        private readonly WorkProcessor<T>[] workProcessors;

        /// <summary>
        /// Create a worker pool to enable an array of {@link WorkHandler}s to consume published sequences.
        /// This option requires a pre-configured {@link RingBuffer} which must have {@link RingBuffer#AddGatingSequences(Sequence...)}
        /// called before the work pool is started.
        /// </summary>
        /// <param name="ringBuffer">of events to be consumed.</param>
        /// <param name="sequenceBarrier">on which the workers will depend.</param>
        /// <param name="exceptionHandler">to callback when an error occurs which is not handled by the {@link WorkHandler}s.</param>
        /// <param name="workHandlers">to distribute the work load across.</param>
        public WorkerPool(RingBuffer<T> ringBuffer,
                       ISequenceBarrier sequenceBarrier,
                       IExceptionHandler exceptionHandler,
                       params IWorkHandler<T>[] workHandlers)
        {
            this.ringBuffer = ringBuffer;
            int numWorkers = workHandlers.Length;
            workProcessors = new WorkProcessor<T>[numWorkers];

            for (int i = 0; i < numWorkers; i++)
            {
                workProcessors[i] = new WorkProcessor<T>(ringBuffer,
                                                         sequenceBarrier,
                                                         workHandlers[i],
                                                         exceptionHandler,
                                                         workSequence);
            }
        }

        /// <summary>
        /// Construct a work pool with an internal {@link RingBuffer} for convenience.
        /// This option does not require {@link RingBuffer#AddGatingSequences(Sequence...)} to be called before the work pool is started.
        /// </summary>
        /// <param name="eventFactory">for filling the {@link RingBuffer}</param>
        /// <param name="exceptionHandler">to callback when an error occurs which is not handled by the {@link WorkHandler}s.</param>
        /// <param name="workHandlers">to distribute the work load across.</param>
        public WorkerPool(Func<T> eventFactory,
                       IExceptionHandler exceptionHandler,
                       params IWorkHandler<T>[] workHandlers)
        {
            ringBuffer = RingBuffer<T>.CreateMultiProducer(eventFactory, 1024, new BlockingWaitStrategy());
            ISequenceBarrier barrier = ringBuffer.NewBarrier();
            var numWorkers = workHandlers.Length;
            workProcessors = new WorkProcessor<T>[numWorkers];

            for (var i = 0; i < numWorkers; i++)
            {
                workProcessors[i] = new WorkProcessor<T>(ringBuffer,
                                                         barrier,
                                                         workHandlers[i],
                                                         exceptionHandler,
                                                         workSequence);
            }

            ringBuffer.AddGatingSequences(getWorkerSequences());
        }

        /// <summary>
        /// Get an array of <see cref="Sequence"/>s representing the progress of the workers.
        /// </summary>
        /// <returns>an array of <see cref="Sequence"/>s representing the progress of the workers.</returns>
        public Sequence[] getWorkerSequences()
        {
            var sequences = new Sequence[workProcessors.Length + 1];
            for (int i = 0, size = workProcessors.Length; i < size; i++)
            {
                sequences[i] = workProcessors[i].Sequence;
            }
            sequences[sequences.Length - 1] = workSequence;

            return sequences;
        }

        /// <summary>
        /// Start the worker pool processing events in sequence.
        /// </summary>
        /// <param name="taskScheduler"> the <see cref="TaskScheduler"/> used to start <see cref="IEventProcessor"/>s.</param>
        /// <returns>the <see cref="RingBuffer"/> used for the work queue.</returns>
        public RingBuffer<T> start(TaskScheduler taskScheduler)
        {
            if (!started.AtomicCompareExchange(true, false))
            {
                throw new InvalidOperationException("WorkerPool has already been started and cannot be restarted until halted.");
            }

            var cursor = ringBuffer.GetCursor;
            workSequence.Value = cursor;

            foreach (var processor in workProcessors)
            {
                processor.Sequence.Value = cursor;
                Task.Factory.StartNew(processor.Run, CancellationToken.None, TaskCreationOptions.None, taskScheduler);
            }

            return ringBuffer;
        }

        /// <summary>
        /// Wait for the <see cref="RingBuffer"/> to drain of published events then halt the workers.
        /// </summary>
        public void drainAndHalt()
        {
            var workerSequences = getWorkerSequences();
            while (ringBuffer.GetCursor > Util.GetMinimumSequence(workerSequences))
            {
                Thread.Sleep(0);
            }

            foreach (WorkProcessor<T> processor in workProcessors)
            {
                processor.Halt();
            }

            started.WriteFullFence(false);
        }

        /// <summary>
        ///  Halt all workers immediately at the end of their current cycle.
        /// </summary>
        public void halt()
        {
            foreach (var processor in workProcessors)
            {
                processor.Halt();
            }

            started.WriteFullFence(false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool IsRunning()
        {
            return started.ReadFullFence();
        }
    }
}
