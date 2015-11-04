using System;
using NUnit.Framework;
using Disruptor;

namespace DisruptorGeneralTest
{
    [TestFixture]
    public class YieldingWaitStrategyTest
    {
        [Test]
        public void ShouldWaitForValue()
        {
            WaitStrategyTestUtil.AssertWaitForWithDelayOf(TimeSpan.FromMilliseconds(50), new YieldingWaitStrategy());
        }
    }
}
