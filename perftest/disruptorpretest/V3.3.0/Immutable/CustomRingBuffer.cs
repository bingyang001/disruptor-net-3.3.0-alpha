using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Disruptor;

namespace disruptorpretest.V3._3._0.Immutable
{
    public class CustomRingBuffer<T> : IDataProvider<IEventAccessor<T>>, IEventAccessor<T>
    {
        private class AccessorEventHandler<E> : IEventHandler<IEventAccessor<E>>, ILifecycleAware
        {
            private readonly IEventHandler<E> handler;
            private readonly ILifecycleAware lifecycle;

            public AccessorEventHandler(IEventHandler<E> handler)
            {
                this.handler = handler;
                lifecycle = handler is ILifecycleAware ? (ILifecycleAware)handler : null;
            }


            public void OnEvent(IEventAccessor<E> accessor, long sequence, bool endOfBatch)
            {
                this.handler.OnEvent(accessor.take(sequence), sequence, endOfBatch);
            }

            public void onShutdown()
            {
                if (null != lifecycle)
                {
                    lifecycle.OnShutdown();
                }
            }


            public void onStart()
            {
                if (null != lifecycle)
                {
                    lifecycle.OnStart();
                }
            }

            #region ILifecycleAware 成员

            public void OnStart()
            {
                if (null != lifecycle)
                {
                    lifecycle.OnStart();
                }
            }

            public void OnShutdown()
            {
                if (null != lifecycle)
                {
                    lifecycle.OnShutdown();
                }
            }

            #endregion
        }
        private readonly ISequencer sequencer;
        private readonly Object[] buffer;
        private readonly int mask;

        public CustomRingBuffer(ISequencer sequencer)
        {
            this.sequencer = sequencer;
            buffer = new Object[sequencer.GetBufferSize];
            mask = sequencer.GetBufferSize - 1;
        }
        private int index(long sequence)
        {
            return (int)sequence & mask;
        }

        public void put(T e)
        {
            long next = sequencer.Next();
            buffer[index(next)] = e;
            sequencer.Publish(next);
        }
        #region IDataProvider<IEventAccessor<T>> 成员

        public IEventAccessor<T> this[long sequence]
        {
            get { return this; }
        }

        public IEventAccessor<T> Get(long sequence)
        {
            return this;
        }

        public T take(long sequence)
        {
            int _index = index(sequence);

            T t = (T)buffer[_index];
            buffer[_index] = null;

            return t;
        }

        #endregion
        public BatchEventProcessor<IEventAccessor<T>> createHandler(IEventHandler<T> handler)
        {
            BatchEventProcessor<IEventAccessor<T>> processor =
                    new BatchEventProcessor<IEventAccessor<T>>(this,
                            sequencer.NewBarrier(),
                            new AccessorEventHandler<T>(handler));
            sequencer.AddGatingSequences(processor.Sequence);

            return processor;
        }
    }
}
