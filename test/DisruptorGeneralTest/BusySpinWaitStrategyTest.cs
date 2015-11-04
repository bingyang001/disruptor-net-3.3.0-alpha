using System;
using NUnit.Framework;
using Disruptor;

namespace DisruptorGeneralTest
{
    [TestFixture]
    public class BusySpinWaitStrategyTest
    {
        [Test]
        public void ShouldWaitForValue()
        {
           // Assert.AreEqual(50, new BusySpinWaitStrategy());
            WaitStrategyTestUtil.AssertWaitForWithDelayOf(TimeSpan.FromMilliseconds(50), new BusySpinWaitStrategy());
        }
    }
}
