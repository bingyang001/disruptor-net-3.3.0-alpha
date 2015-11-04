using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using NUnit.Framework;
using Disruptor;


namespace DisruptorGeneralTest.Dsl.stubs
{
    public class StubExecutor
    {
        private readonly _Volatile.Boolean ignoreExecution = new _Volatile.Boolean(false);
        private readonly _Volatile.Integer executionCount = new _Volatile.Integer(0);
        private readonly List<Thread> threads = new List<Thread>();

        public void Execute(Action command)
        {
            executionCount.AtomicIncrementAndGet();
            if (!ignoreExecution.ReadFullFence())
            {
                var th = new Thread(() => command());
                th.Name = command.ToString();
                th.IsBackground = true;
                threads.Add(th);
                th.Start();
            }
        }

        public void JoinAllThreads()
        {
            foreach (Thread thread in threads)
            {
                if (thread.IsAlive)
                {
                    try
                    {
                        thread.Abort();
                        thread.Join(5000);
                    }
                    catch (ThreadAbortException e)
                    {
                        Console.WriteLine(e.ToString());
                    }
                }

                Assert.False(thread.IsAlive,"Failed to stop thread: " + thread);
            }

            threads.Clear();
        }

        public void IgnoreExecutions()
        {
            ignoreExecution.WriteCompilerOnlyFence(true);
        }

        public int GetExecutionCount
        {
            get { return executionCount.ReadCompilerOnlyFence(); }
        }
    }
}
