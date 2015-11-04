using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace Disruptor.Dsl
{
    /// <summary>
    /// A DSL-style API for setting up the disruptor pattern around a ring buffer (aka the Builder pattern).
    /// <p>A simple example of setting up the disruptor with two event handlers that must process events in order:</p>
    ///  <pre><code> Disruptor&lt;MyEvent&gt; disruptor = new Disruptor&lt;MyEvent&gt;(MyEvent.FACTORY, 32, Executors.newCachedThreadPool());
    /// EventHandler&lt;MyEvent&gt; handler1 = new EventHandler&lt;MyEvent&gt;() { ... };
    /// EventHandler&lt;MyEvent&gt; handler2 = new EventHandler&lt;MyEvent&gt;() { ... };
    /// disruptor.HandleEventsWith(handler1);
    /// disruptor.after(handler1).HandleEventsWith(handler2);
    /// RingBuffer ringBuffer = disruptor.Start();</code></pre>
    /// </summary>
    /// <typeparam name="T"> the type of event used.</typeparam>
    public class Disruptor<T> where T : class
    {
        private readonly RingBuffer<T> ringBuffer;    
        private readonly TaskScheduler taskScheduler;
        private readonly ConsumerRepository<T> consumerRepository = new ConsumerRepository<T>();
        private _Volatile.Boolean started = new _Volatile.Boolean(false);
        private IExceptionHandler exceptionHandler;

        /// <summary>
        /// Create a new Disruptor.
        /// </summary>
        /// <param name="eventFactory">the factory to Create events in the ring buffer.</param>
        /// <param name="ringBufferSize">the size of the ring buffer.</param>
        /// <param name="taskScheduler">an <see cref="TaskScheduler"/> to execute event processors.</param>
        public Disruptor(Func<T> eventFactory, int ringBufferSize, TaskScheduler taskScheduler)
            : this(RingBuffer<T>.CreateMultiProducer(eventFactory, ringBufferSize), taskScheduler)
        {

        }

        /// <summary>
        ///  Create a new Disruptor.
        /// </summary>
        /// <param name="eventFactory">the factory to Create events in the ring buffer.</param>
        /// <param name="ringBufferSize">ringBufferSize</param>
        /// <param name="taskScheduler">an <see cref="TaskScheduler"/> to execute event processors.</param>
        /// <param name="producerType">the claim strategy to use for the ring buffer.</param>
        /// <param name="waitStrategy">the wait strategy to use for the ring buffer.</param>
        public Disruptor(Func<T> eventFactory,
                     int ringBufferSize,
                     TaskScheduler taskScheduler,
                     ProducerType producerType,
                     IWaitStrategy waitStrategy)
            : this(RingBuffer<T>.Create(producerType, eventFactory, ringBufferSize, waitStrategy),
                 taskScheduler)
        {

        }

        /// <summary>
        /// Private constructor helper
        /// </summary>
        /// <param name="ringBuffer"></param>
        /// <param name="taskScheduler"></param>
        private Disruptor(RingBuffer<T> ringBuffer, TaskScheduler taskScheduler)
        {
            this.ringBuffer = ringBuffer;
            this.taskScheduler = taskScheduler;
        }

        /// <summary>
        /// <p>Set up event handlers to handle events from the ring buffer. These handlers will process events
        /// as soon as they become available, in parallel.</p>
        ///
        /// <p>This method can be used as the start of a chain. For example if the handler <code>A</code> must
        /// process events before handler <code>B</code>:</p>
        /// <pre><code>dw.HandleEventsWith(A).then(B);</code></pre>
        /// </summary>
        /// <param name="handlers">the event handlers that will process events.</param>
        /// <returns> a <see cref="EventHandlerGroup"/> that can be used to chain dependencies.</returns>
        public EventHandlerGroup<T> HandleEventsWith(params IEventHandler<T>[] handlers)
        {
            return CreateEventProcessors(new Sequence[0], handlers);
        }

        /// <summary>
        /// <p>Set up custom event processors to handle events from the ring buffer. The Disruptor will
        /// automatically start these processors when {@link #start()} is called.</p>
        ///
        /// <p>This method can be used as the start of a chain. For example if the handler <code>A</code> must
        /// process events before handler <code>B</code>:</p>
        /// <pre><code>dw.handleEventsWith(A).then(B);</code></pre>
        ///
        /// <p>Since this is the start of the chain, the processor factories will always be passed an empty <code>Sequence</code>
        /// array, so the factory isn't necessary in this case. This method is provided for consistency with
        /// {@link EventHandlerGroup#handleEventsWith(EventProcessorFactory...)} and {@link EventHandlerGroup#then(EventProcessorFactory...)}
        /// which do have barrier sequences to provide.</p> 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="eventProcessorFactories">eventProcessorFactories the event processor factories to use to create the event processors that will process events.</param>
        /// <returns>a {@link EventHandlerGroup} that can be used to chain dependencies.</returns>
        public EventHandlerGroup<T> HandleEventsWith(params IEventProcessorFactory<T>[] eventProcessorFactories)
        {
            Sequence[] barrierSequences = new Sequence[0];
            return CreateEventProcessors(barrierSequences, eventProcessorFactories);
        }

        /// <summary>
        /// <p>Set up custom event processors to handle events from the ring buffer. The Disruptor will
        /// automatically start this processors when {@link #start()} is called.</p>
        ///
        /// <p>This method can be used as the start of a chain. For example if the processor <code>A</code> must
        /// process events before handler <code>B</code>:</p>
        /// <pre><code>dw.handleEventsWith(A).then(B);</code></pre>
        /// </summary>
        /// <param name="processors">processors the event processors that will process events.</param>
        /// <returns> a <see cref="EventHandlerGroup"/>that can be used to chain dependencies.</returns>
        public EventHandlerGroup<T> HandleEventsWith(params  IEventProcessor[] processors)
        {
            foreach (var processor in processors)
            {
                consumerRepository.Add(processor);
            }
            return new EventHandlerGroup<T>(this, consumerRepository, Util.GetSequencesFor(processors));
        }

        /// <summary>
        ///  Set up a <see cref="WorkerPool"/>to distribute an event to one of a pool of work handler threads.
        /// Each event will only be processed by one of the work handlers.
        /// The Disruptor will automatically start this processors when {@link #Start()} is called.
        /// </summary>
        /// <param name="workHandlers">the work handlers that will process events.</param>
        /// <returns>a <see cref="EventHandlerGroup"/> that can be used to chain dependencies.</returns>
        public EventHandlerGroup<T> HandleEventsWithWorkerPool(params IWorkHandler<T>[] workHandlers)
        {
            return CreateWorkerPool(new Sequence[0], workHandlers);
        }

        /// <summary>
        /// <p>Specify an exception handler to be used for any future event handlers.</p>     
        /// <p>Note that only event handlers set up after calling this method will use the exception handler.</p>
        /// </summary>
        /// <param name="exceptionHandler">the exception handler to use for any future <see cref="EventProcessor"/>.</param>
        public void HandleExceptionsWith(IExceptionHandler exceptionHandler)
        {
            this.exceptionHandler = exceptionHandler;
        }

        /// <summary>
        /// Override the default exception handler for a specific handler.
        /// <pre>disruptorWizard.handleExceptionsIn(eventHandler).with(exceptionHandler);</pre>
        /// </summary>
        /// <param name="eventHandler">the event handler to set a different exception handler for.</param>
        /// <returns>an ExceptionHandlerSetting dsl object - intended to be used by chaining the with method call.</returns>
        public ExceptionHandlerSetting<T> HandleExceptionsFor(IEventHandler<T> eventHandler)
        {
            return new ExceptionHandlerSetting<T>(eventHandler, consumerRepository);
        }

        /// <summary>
        ///  p>Create a group of event handlers to be used as a dependency.
        /// For example if the handler <code>A</code> must process events before handler <code>B</code>:</p>
        ///
        /// <pre><code>dw.after(A).HandleEventsWith(B);</code></pre>
        /// </summary>
        /// <param name="handlers">the event handlers, previously set up with {@link #HandleEventsWith(com.lmax.disruptor.EventHandler[])},that will form the barrier for subsequent handlers or processors.</param>
        /// <returns>an <see cref="EventHandlerGroup"/>that can be used to setup a dependency barrier over the specified event handlers.</returns>
        public EventHandlerGroup<T> After(params IEventHandler<T>[] handlers)
        {
            var sequences = new Sequence[handlers.Length];
            for (int i = 0, handlersLength = handlers.Length; i < handlersLength; i++)
            {
                sequences[i] = consumerRepository.GetSequenceFor(handlers[i]);
            }

            return new EventHandlerGroup<T>(this, consumerRepository, sequences);
        }

        /// <summary>
        /// Create a group of event processors to be used as a dependency.
        /// </summary>
        /// <param name="processors">the event processors, previously set up with {@link #HandleEventsWith(com.lmax.disruptor.EventProcessor...)},that will form the barrier for subsequent handlers or processors.</param>
        /// <returns>an {@link EventHandlerGroup} that can be used to setup a {@link SequenceBarrier} over the specified event processors.</returns>
        public EventHandlerGroup<T> After(params IEventProcessor[] processors)
        {
            foreach (var processor in processors)
            {
                consumerRepository.Add(processor);
            }

            return new EventHandlerGroup<T>(this, consumerRepository, Util.GetSequencesFor(processors));
        }

        /// <summary>
        /// Publish an event to the ring buffer.
        /// </summary>
        /// <param name="eventTranslator">eventTranslator the translator that will load data into the event.</param>
        public void PublishEvent(IEventTranslator<T> eventTranslator)
        {
            ringBuffer.PublishEvent(eventTranslator);
        }

        /// <summary>
        /// Publish an event to the ring buffer.
        /// </summary>
        /// <typeparam name="A"></typeparam>
        /// <param name="eventTranslator"> the translator that will load data into the event.</param>
        /// <param name="arg"> A single argument to load into the event</param>
        public void PublishEvent<A>(IEventTranslatorOneArg<T, A> eventTranslator, A arg)
        {
            ringBuffer.PublishEvent(eventTranslator, arg);
        }

        /// <summary>
        /// Publish a batch of events to the ring buffer.
        /// </summary>
        /// <typeparam name="A"></typeparam>
        /// <param name="eventTranslator">the translator that will load data into the event.</param>
        /// <param name="arg">arg An array single arguments to load into the events. One Per event</param>
        public void PublishEvent<A>(IEventTranslatorOneArg<T, A> eventTranslator, A[] arg)
        {
            ringBuffer.PublishEvents(eventTranslator, arg);
        }

        /// <summary>
        /// <p>Starts the event processors and returns the fully configured ring buffer.</p>
        ///
        /// <p>The ring buffer is set up to prevent overwriting any entry that is yet to
        /// be processed by the slowest event processor.</p>
        ///
        /// <p>This method must only be called once after all event processors have been added.</p>
        /// </summary>
        /// <returns>the configured ring buffer.</returns>
        public RingBuffer<T> Start()
        {
            var gatingSequences = consumerRepository.GetLastSequenceInChain(true);
            ringBuffer.AddGatingSequences(gatingSequences);

            CheckOnlyStartedOnce();
            foreach (var consumerInfo in consumerRepository.AllConsumerInfos)
            {
                consumerInfo.Start(taskScheduler);
            }

            return ringBuffer;
        }

        /// <summary>
        /// Calls <see cref="com.lmax.disruptor.EventProcessor#halt()"/>on all of the event processors created via this disruptor.
        /// </summary>
        public void Halt()
        {
            foreach (var consumerInfo in consumerRepository.AllConsumerInfos)
            {
                consumerInfo.Halt();
            }
        }

        /// <summary>
        /// Waits until all events currently in the disruptor have been processed by all event processors
        /// and then halts the processors.  It is critical that publishing to the ring buffer has stopped
        /// before calling this method, otherwise it may never return.
        ///
        /// <p>This method will not shutdown the executor, nor will it await the final termination of the
        /// processor threads.</p>
        /// </summary>
        public void Shutdown()
        {          
            try
            {
                Shutdown(TimeSpan.FromSeconds(3),true) ;
            }
            catch (TimeoutException e)
            {
                exceptionHandler.HandleOnShutdownException(e);
            }
        }

        /// <summary>
        /// <p>Waits until all events currently in the disruptor have been processed by all event processors
        /// and then halts the processors.</p>
        ///
        /// <p>This method will not shutdown the executor, nor will it await the final termination of the
        /// processor threads.</p>
        /// </summary>
        /// <param name="timeOut">the amount of time to wait for all events to be processed. <code>-1</code> will give an infinite timeout</param>
        /// <param name="timeOutThrowOut">timeOutThrowOut true <para>TimeoutException</para></param>
        public void Shutdown(TimeSpan timeOut = default(TimeSpan), bool timeOutThrowOut = false)
        {
            if (timeOut == default(TimeSpan))
            {
                System.Threading.SpinWait.SpinUntil(() => !HasBacklog());
            }
            else
            {
                if (!System.Threading.SpinWait.SpinUntil(() => !HasBacklog(), timeOut) && timeOutThrowOut)
                {
                    throw TimeoutException.INSTANCE;
                }
            }
            Halt();
        }

        #region
        //public void shutdown(long timeout, TimeUnit timeUnit)
        //{
        //    long timeOutAt = System.currentTimeMillis() + timeUnit.toMillis(timeout);
        //    while (hasBacklog())
        //    {
        //        if (timeout >= 0 && System.currentTimeMillis() > timeOutAt)
        //        {
        //            throw TimeoutException.INSTANCE;
        //        }
        //        // Busy spin
        //    }
        //    halt();
        //}
        #endregion
        
        /// <summary>
        ///  Confirms if all messages have been consumed by all event processors
        /// </summary>
        /// <returns></returns>
        private bool HasBacklog()
        {
            var cursor = ringBuffer.GetCursor;            
            foreach (Sequence consumer in consumerRepository.GetLastSequenceInChain(false))
            {
                if (cursor > consumer.Value)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// The <see cref="RingBuffer"/>RingBuffer} used by this Disruptor.  This is useful for creating custom
        /// event processors if the behaviour of {@link BatchEventProcessor} is not suitable.
        /// </summary>
        /// <returns></returns>
        public RingBuffer<T> GetRingBuffer
        {
            get { return ringBuffer; }
        }

        /// <summary>
        /// Get the value of the cursor indicating the published sequence.
        /// </summary>
        /// <returns>of the cursor for events that have been published.</returns>
        public long GetCursor
        {
            get { return ringBuffer.GetCursor; }
        }

        /// <summary>
        /// The capacity of the data structure to hold entries.
        /// </summary>
        /// <returns>the size of the RingBuffer.</returns>
        public long GetBufferSize
        {
            get { return ringBuffer.GetBufferSize; }
        }

        /// <summary>
        ///  Get the event for a given sequence in the RingBuffer.
        /// </summary>
        /// <param name="sequence">sequence for the event.</param>
        /// <returns>event for the sequence.<see cref="RingBuffer[long]"/></returns>
        public T this[long sequence]
        {
            get { return ringBuffer[sequence]; }
        }

        /// <summary>
        /// Get the <see cref="SequenceBarrier"/> used by a specific handler. Note that the {@link SequenceBarrier}
        /// may be shared by multiple event handlers.
        /// </summary>
        /// <param name="handler">the handler to get the barrier for.</param>
        /// <returns>the SequenceBarrier used by <i>handler</i>.</returns>
        public ISequenceBarrier GetBarrierFor(IEventHandler<T> handler)
        {
            return consumerRepository.GetBarrierFor(handler);
        }


        public EventHandlerGroup<T> CreateEventProcessors(Sequence[] barrierSequences,
                                               IEventProcessorFactory<T>[] processorFactories)
        {
            IEventProcessor[] eventProcessors = new IEventProcessor[processorFactories.Length];
            for (int i = 0; i < processorFactories.Length; i++)
            {
                eventProcessors[i] = processorFactories[i].CreateEventProcessor(ringBuffer, barrierSequences);
            }
            return HandleEventsWith(eventProcessors);
            
        }

        public EventHandlerGroup<T> CreateEventProcessors(Sequence[] barrierSequences,
                                            IEventHandler<T>[] eventHandlers)
        {

            CheckNotStarted();

            var processorSequences = new Sequence[eventHandlers.Length];
            var barrier = ringBuffer.NewBarrier(barrierSequences);

            for (int i = 0, eventHandlersLength = eventHandlers.Length; i < eventHandlersLength; i++)
            {
                IEventHandler<T> eventHandler = eventHandlers[i];

                BatchEventProcessor<T> batchEventProcessor = new BatchEventProcessor<T>(ringBuffer, barrier, eventHandler);

                if (exceptionHandler != null)
                {
                    batchEventProcessor.SetExceptionHandler(exceptionHandler);
                }

                consumerRepository.Add(batchEventProcessor, eventHandler, barrier);
                processorSequences[i] = batchEventProcessor.Sequence;
            }

            if (processorSequences.Length > 0)
            {
                consumerRepository.UnMarkEventProcessorsAsEndOfChain(barrierSequences);
            }

            return new EventHandlerGroup<T>(this, consumerRepository, processorSequences);
        }

        public EventHandlerGroup<T> CreateWorkerPool(Sequence[] barrierSequences, IWorkHandler<T>[] workHandlers)
        {
            var sequenceBarrier = ringBuffer.NewBarrier(barrierSequences);
            var workerPool = new WorkerPool<T>(ringBuffer, sequenceBarrier, exceptionHandler, workHandlers);
            consumerRepository.Add(workerPool, sequenceBarrier);
            return new EventHandlerGroup<T>(this, consumerRepository, workerPool.getWorkerSequences());
        }       

        private void CheckNotStarted()
        {
            if (started.ReadFullFence())
            {
                throw new InvalidOperationException("All event handlers must be added before calling starts.");
            }
        }

        private void CheckOnlyStartedOnce()
        {
            if (!started.AtomicCompareExchange(true, false))
            {
                throw new InvalidOperationException("Disruptor.Start() must only be called once.");
            }
        }
    }
}
