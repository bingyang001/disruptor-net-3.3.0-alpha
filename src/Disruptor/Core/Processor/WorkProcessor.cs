using System;
using System.Threading;

namespace Disruptor
{
    /// <summary>
    /// <p>A <see cref="WorkProcessor"/> wraps a single <see cref="IWorkHandler"/>, effectively consuming the sequence
    /// and ensuring appropriate barriers.</p>
    ///
    ///<p>Generally, this will be used as part of a <see cref="WorkerPool"/>.</p>
    /// </summary>
    /// <typeparam name="T">implementation storing the details for the work to processed.</typeparam>
    public class WorkProcessor<T> : IEventProcessor where T : class
    {
        private _Volatile.Boolean running = new _Volatile.Boolean(false);
        private readonly Sequence sequence = new Sequence(InitialCursorValue.INITIAL_CURSOR_VALUE);
        private readonly RingBuffer<T> ringBuffer;
        private readonly ISequenceBarrier sequenceBarrier;
        private readonly IWorkHandler<T> workHandler;
        private readonly IExceptionHandler exceptionHandler;
        private readonly Sequence workSequence;
        private readonly EventReleaser eventReleaser;

        /// <summary>
        ///  Construct a <see cref="WorkProcessor"/>
        /// </summary>
        /// <param name="ringBuffer">to which events are published.</param>
        /// <param name="sequenceBarrier">on which it is waiting</param>
        /// <param name="workHandler">is the delegate to which events are dispatched.</param>
        /// <param name="exceptionHandler">to be called back when an error occurs</param>
        /// <param name="workSequence">from which to claim the next event to be worked on.  It should always be initialised as <see cref="Sequencer.INITIAL_CURSOR_VALUE"/></param>
        public WorkProcessor(RingBuffer<T> ringBuffer,
                          ISequenceBarrier sequenceBarrier,
                          IWorkHandler<T> workHandler,
                          IExceptionHandler exceptionHandler,
                          Sequence workSequence)
        {
            this.ringBuffer = ringBuffer;
            this.sequenceBarrier = sequenceBarrier;
            this.workHandler = workHandler;
            this.exceptionHandler = exceptionHandler;
            this.workSequence = workSequence;
            this.eventReleaser = new EventReleaser(this.sequence);

            if (this.workHandler is IEventReleaseAware)
            {
                (this.workHandler as IEventReleaseAware).SetEventReleaser(eventReleaser);  
            }
        }

        public Sequence Sequence
        {
            get { return sequence; }
        }

        public void Halt()
        {
            running.WriteFullFence(false);
            sequenceBarrier.Alert();
        }

        /// <summary>
        /// It is ok to have another thread re-run this method after a halt().
        /// </summary>
        public void Run()
        {
            if (!running.AtomicCompareExchange(true, false))
            {
                throw new InvalidOperationException("Thread is already running");
            }
            sequenceBarrier.ClearAlert();

            NotifyStart();

            bool processedSequence = true;
            long cachedAvailableSequence = long.MinValue;
            long nextSequence = sequence.Value;
            T @event = default(T);
            while (true)
            {
                try
                {
                    // if previous sequence was processed - fetch the next sequence and set
                    // that we have successfully processed the previous sequence
                    // typically, this will be true
                    // this prevents the sequence getting too far forward if an exception
                    // is thrown from the WorkHandler                   
                    if (processedSequence)
                    {
                        processedSequence = false;
                        do
                        {
                            nextSequence = workSequence.Value + 1L;
                            sequence.Value = nextSequence - 1L;                            
                        }
                        while (!workSequence.CompareAndSet(nextSequence-1, nextSequence));
                    }                    
                    if (cachedAvailableSequence >= nextSequence)
                    {
                        @event = ringBuffer[nextSequence];
                        workHandler.OnEvent(@event);
                        processedSequence = true;
                    }
                    else
                    {
                        cachedAvailableSequence = sequenceBarrier.WaitFor(nextSequence);
                    }
                }
                catch (AlertException ex)
                {
                    if (!running.ReadFullFence())
                    {
                        break;
                    }
                }
                catch (Exception ex)
                {
                    // handle, mark as processed, unless the exception handler threw an exception
                    exceptionHandler.HandleEventException(ex, nextSequence, @event);
                    processedSequence = true;
                }
            }

            NotifyShutdown();

            running.WriteFullFence(false);
        }

        public bool IsRunning()
        {
            return running.ReadFullFence();
        }
        private void NotifyStart()
        {
            if (workHandler is ILifecycleAware)
            {
                try
                {
                    (workHandler as ILifecycleAware).OnStart();
                }
                catch (Exception ex)
                {
                    exceptionHandler.HandleOnStartException(ex);
                }
            }
        }
        private void NotifyShutdown()
        {
            if (workHandler is ILifecycleAware)
            {
                try
                {
                    (workHandler as ILifecycleAware).OnShutdown();
                }
                catch (Exception ex)
                {
                    exceptionHandler.HandleOnShutdownException(ex);
                }
            }
        }
        public class EventReleaser : IEventReleaser
        {
            private readonly Sequence sequence;

            public EventReleaser(Sequence sequence)
            {
                this.sequence = sequence;
            }

            public void Release()
            {
                //this.sequence.LazySet(long.MaxValue);
                this.sequence.Value = long.MaxValue;
            }
        }
    }
}
