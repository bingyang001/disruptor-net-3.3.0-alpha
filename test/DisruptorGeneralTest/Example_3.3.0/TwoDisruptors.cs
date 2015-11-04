using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Disruptor;
using Disruptor.Dsl;

namespace DisruptorGeneralTest.Example_3._3._0
{
    public class TwoDisruptors
    {
        private class ValueEvent<T>
        {
            private T t;

            public T get()
            {
                return t;
            }

            public void set(T t)
            {
                this.t = t;
            }
        }

        private class Translator<T> : IEventTranslatorOneArg<ValueEvent<T>, ValueEvent<T>>
        {

            public void TranslateTo(ValueEvent<T> @event, long sequence, ValueEvent<T> arg0)
            {
                @event.set(arg0.get());
            }
        }

        private class ValueEventHandler<T> : IEventHandler<ValueEvent<T>>
        {
            private RingBuffer<ValueEvent<T>> ringBuffer;
            private Translator<T> translator = new Translator<T>();

            public ValueEventHandler(RingBuffer<ValueEvent<T>> ringBuffer)
            {
                this.ringBuffer = ringBuffer;
            }


            public void OnEvent(ValueEvent<T> @event, long sequence, bool endOfBatch)
            {
                ringBuffer.PublishEvent(translator, @event);
            }


        }


        public static void Main(String[] args)
        {
            ThreadPool.SetMaxThreads(2, 256);
            //Executor executor = Executors.newFixedThreadPool(2);
            //EventFactory<ValueEvent<String>> factory = ValueEventHandler.factory();

            Disruptor<ValueEvent<String>> disruptorA =
                   new Disruptor<ValueEvent<String>>(
                           () => new ValueEvent<string>(),
                           1024,
                           TaskScheduler.Default,
                           ProducerType.MULTI,
                           new BlockingWaitStrategy());

            Disruptor<ValueEvent<String>> disruptorB =
                   new Disruptor<ValueEvent<String>>(
                            () => new ValueEvent<string>(),
                           1024,
                            TaskScheduler.Default,
                           ProducerType.SINGLE,
                           new BlockingWaitStrategy());

            ValueEventHandler<String> handlerA = new ValueEventHandler<String>(
            disruptorB.GetRingBuffer);
            disruptorA.HandleEventsWith(handlerA);

            ValueEventHandler<String> handlerB = new ValueEventHandler<String>(
            disruptorA.GetRingBuffer);
            disruptorB.HandleEventsWith(handlerB);

            disruptorA.Start();
            disruptorB.Start();

            Console.Read ();
        }
    }
}
