using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Disruptor;
//using NUnit.Framework;
namespace DisruptorGeneralTest
{
    public class WaitStrategyTestUtil
    {
        public static void assertWaitForWithDelayOf(int sleepTimeMillis, IWaitStrategy waitStrategy)
        {
            var sequenceUpdater = new SequenceUpdater(sleepTimeMillis, waitStrategy);
            Task.Factory.StartNew(() => sequenceUpdater.run(), CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
            sequenceUpdater.WaitForStartup();
            var cursor = new Sequence(0);
            var sequence = waitStrategy.WaitFor(0, cursor,sequenceUpdater.sequence , new DummySequenceBarrier());
           
            //Assert.AreEqual(sequence, 0L);
            
        }
    }
}
