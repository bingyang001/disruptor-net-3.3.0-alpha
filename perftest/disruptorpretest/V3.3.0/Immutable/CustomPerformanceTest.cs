using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Disruptor;

namespace disruptorpretest.V3._3._0.Immutable
{
    public class CustomPerformanceTest
    {
        private readonly CustomRingBuffer<SimpleEvent> ringBuffer;
        public CustomPerformanceTest()
        {
            ringBuffer = new CustomRingBuffer<SimpleEvent>(new SingleProducerSequencer(Constants.SIZE, new YieldingWaitStrategy()));

        }
        public void run()
        {
            try
            {
                doRun();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private void doRun()
        {
            var batchEventProcessor = ringBuffer.createHandler(new SimpleEventHandler());

            Thread t = new Thread(() =>
            {
                long iterations = Constants.ITERATIONS;
                for (long l = 0; l < iterations; l++)
                {
                    SimpleEvent e = new SimpleEvent(l, l, l, l);
                    ringBuffer.put(e);
                    while (batchEventProcessor.Sequence.Value != iterations - 1)
                    {
                        // LockSupport.parkNanos(1);
                    }
                }

            });
            t.Start();



            batchEventProcessor.Halt();
            t.Join();
        }

        public static void main(String[] args)
        {
            new CustomPerformanceTest().run();
        }

    }
}
