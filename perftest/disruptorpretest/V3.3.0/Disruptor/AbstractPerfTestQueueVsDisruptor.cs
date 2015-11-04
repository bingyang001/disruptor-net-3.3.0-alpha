using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.Diagnostics;
using System.Threading;
using Disruptor;
using Disruptor.Scheduler;

namespace disruptorpretest
{
    [TestFixture]
    public abstract class AbstractPerfTestQueueVsDisruptor
    {
        protected const string Test_Queue = "Queue";
        protected const string Test_Disruptor = "Disruptor";
        protected const string Test_QueueAndDisruptor="ALL";
        private string testName;
        private long runCount;
        protected readonly int RUNS = 20;
        protected const string TestName = "Disruptor";
        protected const int BUFFER_SIZE = 1024 * 64;
        protected const long ITERATIONS = 1000L * 1000L * 100L; //1 亿次
        public AbstractPerfTestQueueVsDisruptor(string testName, long runCount,int testCount=20)
        {
            this.testName = testName;
            this.runCount = runCount;
            this.RUNS=testCount ;
        }

        [Test]
        public void TestImplementations()
        {
            var availableProcessors = Environment.ProcessorCount;
            if (GetRequiredProcessorCount > availableProcessors)
            {
                Console.WriteLine("*** Warning ***: your system has insufficient processors to execute the test efficiently. ");
                Console.WriteLine("Processors required = " + GetRequiredProcessorCount + " available = " + availableProcessors);
            }

            var queueOps = new long[RUNS];
            var disruptorOps = new long[RUNS];
            Console.WriteLine("start test {0:###,###,###,###},testcount {1}", this.runCount,RUNS);
            if (testName == "ALL")
            {
                Console.WriteLine("Starting Queue tests");
                for (int i = 0; i < RUNS; i++)
                {
                    GC.Collect();
                    var startTime = Stopwatch.StartNew();
                    queueOps[i] = RunQueuePass();
                    Console.WriteLine("Run {0}, BlockingQueue={1:###,###,###,###,###} ops/sec 单次执行耗时 {2:###,###,###,###,###} 毫秒", i, queueOps[i], startTime.ElapsedMilliseconds);
                }
                Console.WriteLine("Starting Disruptor tests");
                for (int i = 0; i < RUNS; i++)
                {
                    //GC.Collect();
                    var startTime = Stopwatch.StartNew();
                    disruptorOps[i] = RunDisruptorPass();
                    var endTIme = startTime.ElapsedMilliseconds;
                    Console.WriteLine("Run {0}, Disruptor={1:###,###,###,###,###} ops/sec,单次执行耗时 {2:###,###,###,###,###} 毫秒", i, disruptorOps[i], endTIme);
                }

                for (int i = 0; i < RUNS; i++)
                {
                    Assert.True(disruptorOps[i] > queueOps[i], "Performance degraded");
                }
            }
            else if (testName == Test_Queue)
            {
                Console.WriteLine("Starting Queue tests");
                for (int i = 0; i < RUNS; i++)
                {
                    //GC.Collect();
                    var startTime = Stopwatch.StartNew();
                    queueOps[i] = RunQueuePass();
                    Console.WriteLine("Run {0}, BlockingQueue={1:###,###,###,###,###} ops/sec 单次执行耗时 {2:###,###,###,###,###} 毫秒", i, queueOps[i], startTime.ElapsedMilliseconds);
                }
            }
            else if (testName == Test_Disruptor)
            {
                Console.WriteLine("Starting Disruptor tests");
                for (int i = 0; i < RUNS; i++)
                {
                    //GC.Collect();
                    var startTime = Stopwatch.StartNew();
                    disruptorOps[i] = RunDisruptorPass();
                    var endTIme = startTime.ElapsedMilliseconds;
                    Console.WriteLine("Run {0}, Disruptor={1:###,###,###,###,###} ops/sec,单次执行耗时 {2:###,###,###,###,###} 毫秒", i, disruptorOps[i], endTIme);
                }

            }
        }

        public  void PrintResults(String className, long[] disruptorOps, long[] queueOps)
        {
            for (int i = 0; i < RUNS; i++)
            {
                Console.WriteLine("{0} run {1} 次: BlockingQueue={2:###,###,###,###} , Disruptor={3:###,###,###,###} ops/sec",
                                   className, i, queueOps[i], disruptorOps[i]);
            }
        }

        protected RoundRobinThreadAffinedTaskScheduler TestTaskScheduler
        {
            get { return new RoundRobinThreadAffinedTaskScheduler(Environment.ProcessorCount); }
        }

        protected void WaitForEventProcessorSequence(long expectedCount, SpinWait spinwait, MyRunnable myRunnable)
        {
            while (myRunnable.GetSequence.Value != expectedCount)
            {
                //Thread.Sleep(1);
                spinwait.SpinOnce();
            }
        }
        protected void WaitForEventProcessorSequence(long expectedCount, SpinWait spinwait, _MyRunnable myRunnable)
        {
            while (myRunnable.GetSequence.Value != expectedCount)
            {
                //Thread.Sleep(1);
                spinwait.SpinOnce();
            }
        }
        protected virtual int GetRequiredProcessorCount
        {
            get { return Environment.ProcessorCount; }
        }

        protected abstract long RunQueuePass();

        protected abstract long RunDisruptorPass();

        protected abstract void ShouldCompareDisruptorVsQueues();
    }


    public class MyRunnable
    {
        private ManualResetEvent latch;
        private long expectedCount;
        private Sequence sequence = new Sequence(-1);
        private ISequenceBarrier barrier;

        public MyRunnable(ISequencer sequencer)
        {
            this.barrier = sequencer.NewBarrier();
        }

        public void Reset(ManualResetEvent latch, long expectedCount)
        {
            this.latch = latch;
            this.expectedCount = expectedCount;
        }
        public Sequence GetSequence { get { return sequence; } }
        public void Run()
        {
            long expected = expectedCount;
            long processed = -1;

            try
            {
                do
                {
                    processed = barrier.WaitFor(sequence.Value + 1);
                    sequence.Value = processed;
                }
                while (processed < expected);

                latch.Set();
                sequence.Value=processed;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }

    public class _MyRunnable
    {
        private CountdownEvent latch;
        private long expectedCount;
        private Sequence sequence = new Sequence(-1);
        private ISequenceBarrier barrier;

        public _MyRunnable(ISequencer sequencer)
        {
            this.barrier = sequencer.NewBarrier();
        }

        public void Reset(CountdownEvent latch, long expectedCount)
        {
            this.latch = latch;
            this.expectedCount = expectedCount;
        }
        public Sequence GetSequence { get { return sequence; } }
        public void Run()
        {
            long expected = expectedCount;
            long processed = -1;

            try
            {
                do
                {
                    processed = barrier.WaitFor(sequence.Value + 1);
                    sequence.Value = processed;
                }
                while (processed < expected);

                latch.Signal();
                sequence.Value=processed;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }

}
