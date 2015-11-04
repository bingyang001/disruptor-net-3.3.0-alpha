using System;
using System.Collections.Generic;
//using System.Collections.Concurrent;
using System.Linq;

namespace Disruptor.Dsl
{
    /// <summary>
    /// Provides a repository mechanism to associate <see cref="EventHandler"/> s with <see cref="EventProcessor"/>s
    /// </summary>
    /// <typeparam name="T">the type of the <see cref="EventHandler"/></typeparam>
    public class ConsumerRepository<T> where T : class
    {
        private readonly IDictionary<IEventHandler<T>, EventProcessorInfo<T>> eventProcessorInfoByEventHandler = new Dictionary<IEventHandler<T>, EventProcessorInfo<T>>();
        private readonly IDictionary<Sequence, IConsumerInfo> eventProcessorInfoBySequence = new Dictionary<Sequence, IConsumerInfo>();
        private readonly IList<IConsumerInfo> consumerInfos = new List<IConsumerInfo>();

        public void Add(IEventProcessor eventprocessor,
                     IEventHandler<T> handler,
                     ISequenceBarrier barrier)
        {
            var consumerInfo = new EventProcessorInfo<T>(eventprocessor, handler, barrier);
            eventProcessorInfoByEventHandler[handler] = consumerInfo;
            eventProcessorInfoBySequence[eventprocessor.Sequence] = consumerInfo;
            consumerInfos.Add(consumerInfo);
        }

        public void Add(IEventProcessor processor)
        {
            var consumerInfo = new EventProcessorInfo<T>(processor, null, null);
            eventProcessorInfoBySequence[processor.Sequence] = consumerInfo;
            consumerInfos.Add(consumerInfo);
        }

        public void Add(WorkerPool<T> workerPool, ISequenceBarrier sequenceBarrier)
        {
            var workerPoolInfo = new WorkerPoolInfo<T>(workerPool, sequenceBarrier);
            consumerInfos.Add(workerPoolInfo);
            foreach (var sequence in workerPool.getWorkerSequences())
            {
                eventProcessorInfoBySequence[sequence] = workerPoolInfo;
            }
        }
             
        /// <summary>
        /// 获取序列链上的最后一个序列
        /// </summary>
        /// <param name="includeStopped"></param>
        /// <returns></returns>
        public Sequence[] GetLastSequenceInChain(bool includeStopped)
        {
            return
                consumerInfos
                .Where(consumerInfo => (includeStopped || consumerInfo.IsRunning) && consumerInfo.IsEndOfChain)
                .SelectMany(e => e.GetSequences)
                .ToArray();
        }

        /// <summary>
        /// 获取事件处理器
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public IEventProcessor GetEventProcessorFor(IEventHandler<T> handler)
        {
            var eventprocessorInfo = GetEventProcessorInfo(handler);
            if (eventprocessorInfo == null)
            {
                throw new ArgumentException(string.Format("The event handler {0}is not processing events.", handler));
            }

            return eventprocessorInfo.GetEventProcessor;
        }

        public Sequence GetSequenceFor(IEventHandler<T> handler)
        {
            return GetEventProcessorFor(handler).Sequence;
        }

        public void UnMarkEventProcessorsAsEndOfChain(params Sequence[] barrierEventProcessors)
        {
            foreach (var barrierEventProcessor in barrierEventProcessors)
            {
                GetEventProcessorInfo(barrierEventProcessor).MarkAsUsedInBarrier();
            }
        }

        public IEnumerable<IConsumerInfo> AllConsumerInfos
        {
            get { return consumerInfos; }
        }

        public ISequenceBarrier GetBarrierFor(IEventHandler<T> handler)
        {
            var consumerInfo = GetEventProcessorInfo(handler);
            return consumerInfo != null ? consumerInfo.GetBarrier : null;
        }


        private EventProcessorInfo<T> GetEventProcessorInfo(IEventHandler<T> handler)
        {
            return eventProcessorInfoByEventHandler[handler];
        }

        private IConsumerInfo GetEventProcessorInfo(Sequence barrierEventProcessor)
        {
            return eventProcessorInfoBySequence[barrierEventProcessor];
        }
    }
}
