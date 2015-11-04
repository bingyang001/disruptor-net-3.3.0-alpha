using System;
using System.Threading;

namespace Disruptor
{
    /// <summary>
    /// Convenience class for handling the batching semantics of consuming entries from a {@link RingBuffer}
    /// and delegating the available events to an {@link EventHandler}.
    ///
    /// If the {@link EventHandler} also implements {@link LifecycleAware} it will be notified just after the thread
    /// is started and just before the thread is shutdown.
    /// </summary>
    /// <typeparam name="T">event implementation storing the data for sharing during exchange or parallel coordination of an event.</typeparam>
    public sealed class BatchEventProcessor<T> : IEventProcessor where T : class
    {
        private _Volatile.Boolean running = new _Volatile.Boolean(false);
        private IExceptionHandler exceptionHandler = new FatalExceptionHandler();
        private readonly IDataProvider<T> dataProvider;
        private readonly ISequenceBarrier sequenceBarrier;
        private readonly IEventHandler<T> eventHandler;
        private readonly Sequence sequence = new Sequence(InitialCursorValue.INITIAL_CURSOR_VALUE);
        private readonly ITimeoutHandler timeoutHandler;

        /// <summary>
        ///  Construct a {@link EventProcessor} that will automatically track the progress by updating its sequence when
        ///  the {@link EventHandler#OnEvent(Object, long, boolean)} method returns.
        /// </summary>
        /// <param name="dataProvider">to which events are published.</param>
        /// <param name="sequenceBarrier"> on which it is waiting.</param>
        /// <param name="eventHandler">is the delegate to which events are dispatched.</param>
        public BatchEventProcessor(IDataProvider<T> dataProvider,
                               ISequenceBarrier sequenceBarrier,
                               IEventHandler<T> eventHandler)
        {
            this.dataProvider = dataProvider;
            this.sequenceBarrier = sequenceBarrier;
            this.eventHandler = eventHandler;

            if (eventHandler is ISequenceReportingEventHandler<T>)
            {
                ((ISequenceReportingEventHandler<T>)eventHandler).SetSequenceCallback(sequence);
            }

            timeoutHandler = (eventHandler is ITimeoutHandler) ? (ITimeoutHandler)eventHandler : null;
        }

        /// <summary>
        /// Set a new {@link ExceptionHandler} for handling exceptions propagated out of the {@link BatchEventProcessor}
        /// </summary>
        /// <param name="exceptionHandler">to replace the existing exceptionHandler.</param>
        /// <exception cref="ArgumentNullException">exceptionHandler is null</exception>
        public void SetExceptionHandler(IExceptionHandler exceptionHandler)
        {
            if (null == exceptionHandler)
            {
                throw new ArgumentNullException("exceptionHandler is null");
            }

            this.exceptionHandler = exceptionHandler;
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
        /// It is ok to have another thread rerun this method after a halt().
        /// </summary>
        public void Run()
        {
            if (!running.AtomicCompareExchange(true, false))
            {
                throw new InvalidOperationException("Thread is already running");
            }
            sequenceBarrier.ClearAlert();

            NotifyStart();

            T @event = null;
            var nextSequence = sequence.Value + 1L;
            try
            {
                while (true)
                {
                    try
                    {
                        var availableSequence = sequenceBarrier.WaitFor(nextSequence);
                        ///availableSequence=-1 ??????????
                        while (nextSequence <= availableSequence)
                        {
                            @event = dataProvider.Get(nextSequence);
                            eventHandler.OnEvent(@event, nextSequence, nextSequence == availableSequence);
                            nextSequence++;
                        }
                        sequence.LazySet(availableSequence);
                        //sequence.Value = availableSequence;
                    }
                    catch (TimeoutException e)
                    {
                        NotifyTimeout(sequence.Value);
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
                        exceptionHandler.HandleEventException(ex, nextSequence, @event);
                        sequence.LazySet(nextSequence);
                        //sequence.Value = nextSequence;
                        nextSequence++;
                    }
                }
            }
            finally
            {
                NotifyShutdown();
                running.WriteFullFence(false);
            }
        }

        public bool IsRunning()
        {
            //return running.ReadFullFence();
            return running.ReadCompilerOnlyFence();
        }

        private void NotifyTimeout(long availableSequence)
        {
            try
            {
                if (timeoutHandler != null)
                {
                    timeoutHandler.OnTimeout(availableSequence);
                }
            }
            catch (Exception e)
            {
                exceptionHandler.HandleEventException(e, availableSequence, null);
            }
        }

        /// <summary>
        /// Notifies the EventHandler when this processor is starting up
        /// </summary>
        private void NotifyStart()
        {
            if (eventHandler is ILifecycleAware)
            {
                try
                {
                    ((ILifecycleAware)eventHandler).OnStart();
                }
                catch (Exception ex)
                {
                    exceptionHandler.HandleOnStartException(ex);
                }
            }
        }

        /// <summary>
        ///  Notifies the EventHandler immediately prior to this processor shutting down
        /// </summary>
        private void NotifyShutdown()
        {
            if (eventHandler is ILifecycleAware)
            {
                try
                {
                    ((ILifecycleAware)eventHandler).OnShutdown();
                }
                catch (Exception ex)
                {
                    exceptionHandler.HandleOnShutdownException(ex);
                }
            }
        }
    }
}
