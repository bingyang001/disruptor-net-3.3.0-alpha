using NUnit.Framework;
using Moq;
using Disruptor;
using Disruptor.Dsl;
using DisruptorGeneralTest.Dsl.stubs;

namespace DisruptorGeneralTest.Dsl
{
    [TestFixture]
    public class ConsumerRepositoryTest
    {
        private Mock<IEventProcessor> eventProcessorMock1;
        private Mock<IEventProcessor> eventProcessorMock2;
        private ConsumerRepository<TestEvent> consumerRepository;
        private SleepingEventHandler handler1;
        private SleepingEventHandler handler2;
        private Mock<ISequenceBarrier> barrier1;
        private Mock<ISequenceBarrier> barrier2;

        [SetUp]
        public void SetUp()
        {
            consumerRepository = new ConsumerRepository<TestEvent>();

            eventProcessorMock1 = new Mock<IEventProcessor>();
            eventProcessorMock2 = new Mock<IEventProcessor>();

            //eventProcessorMock1.Verify(e => e.IsRunning(), "IEventProcessor not Running ");
            //eventProcessorMock2.Verify(e => e.IsRunning(), "IEventProcessor not Running ");

            var sequence1 = new Sequence();
            var sequence2 = new Sequence();

            handler1 = new SleepingEventHandler();
            handler2 = new SleepingEventHandler();

            eventProcessorMock1.SetupGet(e => e.Sequence).Returns(sequence1);
            eventProcessorMock2.SetupGet(e => e.Sequence).Returns(sequence2);

            barrier1 = new Mock<ISequenceBarrier>();
            barrier2 = new Mock<ISequenceBarrier>();
          
        }

        [Test]
        public void ShouldGetBarrierByHandler()
        {
            consumerRepository.Add(eventProcessorMock1.Object, handler1, barrier1.Object);
            Assert.AreEqual(consumerRepository.GetBarrierFor(handler1), barrier1.Object);
        }
    }
}
