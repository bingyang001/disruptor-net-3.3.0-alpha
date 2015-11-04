using System.Threading;
using System.Threading.Tasks;

namespace Disruptor.Dsl
{
    /// <summary>
    /// <p>Wrapper class to tie together a particular event processing stage</p>
    ///* <p>Tracks the event processor instance, the event handler instance, and sequence barrier which the stage is attached to.</p>
    /// </summary>
    /// <typeparam name="T">the type of the configured {@link EventHandler}</typeparam>
    internal class EventProcessorInfo<T> : IConsumerInfo
    {
        private readonly IEventProcessor eventprocessor;
        private readonly IEventHandler<T> handler;
        private readonly ISequenceBarrier barrier;
        private bool endOfChain = true;

        public EventProcessorInfo(IEventProcessor eventprocessor, IEventHandler<T> handler, ISequenceBarrier barrier)
        {
            this.eventprocessor = eventprocessor;
            this.handler = handler;
            this.barrier = barrier;
        }
        
        public IEventProcessor GetEventProcessor
        {
            get { return eventprocessor; }
        }

        public Sequence[] GetSequences
        {
            get { return new Sequence[] { eventprocessor.Sequence }; }
        }
        
        public IEventHandler<T> GetHandler
        {
            get { return handler; }
        }
        
        public ISequenceBarrier GetBarrier
        {
            get { return barrier; }
        }
        
        public bool IsEndOfChain
        {
            get { return endOfChain; }
        }
        
        public void Start(TaskScheduler taskScheduler)
        {
            Task.Factory.StartNew(eventprocessor.Run, CancellationToken.None, TaskCreationOptions.None, taskScheduler);
        }
        
        public void Halt()
        {
            eventprocessor.Halt();
        }
       
        public void MarkAsUsedInBarrier()
        {
            endOfChain = false;
        }
       
        public bool IsRunning
        {
            get { return eventprocessor.IsRunning(); }
        }
    }
}
