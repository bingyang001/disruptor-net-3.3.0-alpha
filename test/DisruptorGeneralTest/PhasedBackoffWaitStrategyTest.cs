using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Disruptor;

namespace DisruptorGeneralTest
{
    [TestFixture]
    public class PhasedBackoffWaitStrategyTest
    {
        [Test]
        public void ShouldHandleImmediateSequenceChange()
        {
            WaitStrategyTestUtil.AssertWaitForWithDelayOf(new TimeSpan(0), PhasedBackoffWaitStrategy.WithLock(TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1)));
            WaitStrategyTestUtil.AssertWaitForWithDelayOf(new TimeSpan(0), PhasedBackoffWaitStrategy.WithLock(TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1)));
        }

        [Test]
        public void ShouldHandleSequenceChangeWithOneMillisecondDelay()
        {
            WaitStrategyTestUtil.AssertWaitForWithDelayOf(new TimeSpan(1), PhasedBackoffWaitStrategy.WithLock(TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1)));
            WaitStrategyTestUtil.AssertWaitForWithDelayOf(new TimeSpan(1), PhasedBackoffWaitStrategy.WithLock(TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1)));
        }

        [Test]
        public void ShouldHandleSequenceChangeWithTwoMillisecondDelay()
        {
            WaitStrategyTestUtil.AssertWaitForWithDelayOf(new TimeSpan(2), PhasedBackoffWaitStrategy.WithLock(TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1)));
            WaitStrategyTestUtil.AssertWaitForWithDelayOf(new TimeSpan(2), PhasedBackoffWaitStrategy.WithLock(TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1)));
        }

        [Test]
        public void ShouldHandleSequenceChangeWithTenMillisecondDelay()
        {
            WaitStrategyTestUtil.AssertWaitForWithDelayOf(new TimeSpan(10), PhasedBackoffWaitStrategy.WithLock(TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1)));
            WaitStrategyTestUtil.AssertWaitForWithDelayOf(new TimeSpan(10), PhasedBackoffWaitStrategy.WithLock(TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1)));
        }
    }
}
