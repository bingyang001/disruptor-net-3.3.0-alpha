using System;
using System.Threading.Tasks;

using NUnit.Framework;
using Disruptor;
using Disruptor.Dsl;

namespace DisruptorGeneralTest
{
    [TestFixture]
    public class ShutdownOnFatalExceptionTest
    {
        private Random random;
        private FailingEventHandler eventHandler;
        private Disruptor<byte[]> disruptor;

        [SetUp]
        public void SetUp()
        {
            random = new Random(Guid.NewGuid().GetHashCode());
            eventHandler = new FailingEventHandler();
            disruptor = new Disruptor<byte[]>(() => new byte[256], 1024, TaskScheduler.Default, ProducerType.SINGLE, new BlockingWaitStrategy());
            disruptor.HandleEventsWith(eventHandler);
            disruptor.HandleExceptionsWith(new FatalExceptionHandler());
        }

        [TestFixtureTearDown]
        public void TearDown()
        {
            disruptor.Shutdown();
        }

        [Test]
        [Timeout(1000)]
        [ExpectedException(typeof(ApplicationException))]
        public void ShouldShutdownGracefulEvenWithFatalExceptionHandler()
        {
            disruptor.Start();

            byte[] bytes;
            for (int i = 1; i < 10; i++)
            {
                bytes = new byte[32];
                random.NextBytes(bytes);
                disruptor.PublishEvent(new ByteArrayTranslator(bytes));
            }
        }


    }

    class ByteArrayTranslator : IEventTranslator<byte[]>
    {
        private byte[] bytes;

        public ByteArrayTranslator(byte[] bytes)
        {
            this.bytes = bytes;
        }
        #region IEventTranslator<byte[]> 成员

        public void TranslateTo(byte[] @event, long sequence)
        {
            bytes = @event;
        }

        #endregion
    }

    class FailingEventHandler : IEventHandler<byte[]>
    {
        private int count = 0;

        #region IEventHandler<byte[]> 成员

        public void OnEvent(byte[] @event, long sequence, bool endOfBatch)
        {
            count++;
            if (count == 3)
            {                
                throw new ArgumentOutOfRangeException();
            }
        }

        #endregion
    }
}
