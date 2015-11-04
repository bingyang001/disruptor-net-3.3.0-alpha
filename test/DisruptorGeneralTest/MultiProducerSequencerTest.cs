using NUnit.Framework;
using Disruptor;

namespace DisruptorGeneralTest
{
    [TestFixture]
    public class MultiProducerSequencerTest
    {
        [Test]
        public void ShouldOnlyAllowMessagesToBeAvailableIfSpecificallyPublished()
        {
            var publisher = new MultiProducerSequencer(1024, new BlockingWaitStrategy());
            publisher.Publish(3);
            publisher.Publish(5);
            publisher.Publish(6);
            publisher.Publish(7);

            Assert.AreEqual(publisher.IsAvailable(0), false);
            Assert.AreEqual(publisher.IsAvailable(1), false);
            Assert.AreEqual(publisher.IsAvailable(2), false);
            Assert.AreEqual(publisher.IsAvailable(3), true);
            Assert.AreEqual(publisher.IsAvailable(4), false);
            Assert.AreEqual(publisher.IsAvailable(5), true);
            Assert.AreEqual(publisher.IsAvailable(6), true);
            Assert.AreEqual(publisher.IsAvailable(6), true);
        }
    }
}
