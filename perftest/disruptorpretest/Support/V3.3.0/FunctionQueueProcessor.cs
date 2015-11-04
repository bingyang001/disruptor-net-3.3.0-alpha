using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Text;

namespace disruptorpretest.Support.V3._3._0
{
    public class FunctionQueueProcessor
    {
        private readonly FunctionStep functionStep;
        private readonly ConcurrentQueue<long[]> stepOneQueue;
        private readonly ConcurrentQueue<long> stepTwoQueue;
        private readonly ConcurrentQueue<long> stepThreeQueue;
        private readonly long count;

        private volatile bool running;
        private long stepThreeCounter;
        private long sequence;
        private CountdownEvent latch;


        public FunctionQueueProcessor(FunctionStep functionStep,
                                   ConcurrentQueue<long[]> stepOneQueue,
                                   ConcurrentQueue<long> stepTwoQueue,
                                   ConcurrentQueue<long> stepThreeQueue,
                                   long count)
        {
            this.functionStep = functionStep;
            this.stepOneQueue = stepOneQueue;
            this.stepTwoQueue = stepTwoQueue;
            this.stepThreeQueue = stepThreeQueue;
            this.count = count;
        }

        public long getStepThreeCounter()
        {
            return stepThreeCounter;
        }

        public void reset(CountdownEvent latch)
        {
            stepThreeCounter = 0L;
            sequence = 0L;
            this.latch = latch;
        }

        public void halt()
        {
            running = false;
        }


        public void run()
        {
            running = true;
            while (true)
            {
                try
                {
                    switch (functionStep)
                    {
                        case FunctionStep.One:
                            {
                                long[] values;
                                stepOneQueue.TryDequeue(out values);
                                stepTwoQueue.Enqueue(values[0] + values[1]);
                                break;
                            }

                        case FunctionStep.Two:
                            {
                                long value;
                                stepTwoQueue.TryDequeue(out value);
                                stepThreeQueue.Enqueue(value + 3);
                                break;
                            }

                        case FunctionStep.Three:
                            {
                                long value;
                                stepThreeQueue.TryDequeue(out value);
                                long testValue = value;
                                if ((testValue & 4L) == 4L)
                                {
                                    ++stepThreeCounter;
                                }
                                break;
                            }
                    }

                    if (null != latch && sequence++ == count)
                    {
                        latch.Signal();
                    }
                }
                catch (Exception ex)
                {
                    if (!running)
                    {
                        break;
                    }
                }
            }
        }
    }
}
