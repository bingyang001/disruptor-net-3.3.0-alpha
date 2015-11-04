using System;
using NUnit.Framework;
using Disruptor;

namespace DisruptorGeneralTest
{
    [TestFixture]
    public class SleepingWaitStrategyTest
    {
        [Test]
        public void ShouldWaitForValue()
        {
            WaitStrategyTestUtil.AssertWaitForWithDelayOf(TimeSpan.FromMilliseconds(50), new SleepingWaitStrategy());
        }
    }
}
