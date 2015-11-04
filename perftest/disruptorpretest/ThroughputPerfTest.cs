using System;
using disruptorpretest;

namespace disruptorpretest
{
    public abstract class ThroughputPerfTest : PerfTest
    {
        protected ThroughputPerfTest(int iterations) : base(iterations)
        {
        }

        public abstract long RunPass();

        protected override void RunAsUnitTest()
        {
            var operationsPerSecond = RunPass();
            Console.WriteLine("{0}£¨‘À–– {1:###,###,###,###} ¥Œ,,{2:###,###,###,###}op/sec", GetType().Name, Iterations, operationsPerSecond);
        }

        public override TestRun CreateTestRun(int pass, int availableCores)
        {
            return new ThroughputTestRun(this, pass, availableCores);
        }
    }
}