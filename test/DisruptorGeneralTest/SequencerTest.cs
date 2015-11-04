using System;
using System.Threading;
using NUnit.Framework;
using Disruptor;
using Disruptor.Dsl;
using Moq;

namespace DisruptorGeneralTest
{
    [TestFixture(ProducerType.MULTI)]
    [TestFixture(ProducerType.SINGLE)]
    
    public class SequencerTest
    {
        private static readonly int BUFFER_SIZE = 16;
        private ISequencer sequencer;
        private Sequence gatingSequence = new Sequence();
        private ProducerType producerType;
    
     
        public SequencerTest(ProducerType producerType/*, IWaitStrategy waitStrategy*/)
        {
            this.producerType = producerType;
                  
        }

        [SetUp]
        public void Setup()
        {
            this.sequencer = newProducer(producerType, BUFFER_SIZE, new BlockingWaitStrategy());     
        }

        [Test]
        public void ShouldStartWithInitialValue()
        {
            //this.sequencer = newProducer(producerType, BUFFER_SIZE, new BlockingWaitStrategy());
            Assert.AreEqual(0, sequencer.Next());
        }

        [Test]
        public void ShouldBatchClaim()
        {
            //this.sequencer = newProducer(producerType, BUFFER_SIZE, new BlockingWaitStrategy());
            Assert.AreEqual(3, sequencer.Next(4));
        }

        [Test]
        public void ShouldIndicateHasAvailableCapacity()
        {
            //this.sequencer = newProducer(producerType, BUFFER_SIZE, new BlockingWaitStrategy());
            sequencer.AddGatingSequences(gatingSequence);
            Assert.True(sequencer.HasAvailableCapacity(1));
            Assert.True(sequencer.HasAvailableCapacity(BUFFER_SIZE));
            Assert.False(sequencer.HasAvailableCapacity(BUFFER_SIZE + 1));

            sequencer.Publish(sequencer.Next());

            Assert.True(sequencer.HasAvailableCapacity(BUFFER_SIZE - 1));
            Assert.False(sequencer.HasAvailableCapacity(BUFFER_SIZE));
        }

        [Test]
        public void ShouldIndicateNoAvailableCapacity()
        {
            //this.sequencer = newProducer(producerType, BUFFER_SIZE, new BlockingWaitStrategy());
            sequencer.AddGatingSequences(gatingSequence);
            var sequence = sequencer.Next(BUFFER_SIZE);
            sequencer.Publish(sequence - (BUFFER_SIZE - 1), sequence);
            Assert.False(sequencer.HasAvailableCapacity(1));
        }

        [Test]
        public void ShouldHoldUpPublisherWhenBufferIsFull()
        {
            //this.sequencer = newProducer(producerType, BUFFER_SIZE, new BlockingWaitStrategy());
            sequencer.AddGatingSequences(gatingSequence);
            var sequence = sequencer.Next(BUFFER_SIZE);
            sequencer.Publish(sequence - (BUFFER_SIZE - 1), sequence);

            var waitingLatch = new System.Threading.ManualResetEvent(false);//.CountdownEvent(1);
            var doneLatch = new System.Threading.ManualResetEvent(false);//.CountdownEvent(1);

            var expectedFullSequence = InitialCursorValue.INITIAL_CURSOR_VALUE + sequencer.GetBufferSize;
            Assert.AreEqual(sequencer.GetCursor, expectedFullSequence);
            new Thread(
              () =>
              {
                  waitingLatch.Set();

                  sequencer.Publish(sequencer.Next());

                  doneLatch.Set();
              }).Start();

            waitingLatch.WaitOne();
            Assert.AreEqual(sequencer.GetCursor, expectedFullSequence);

            gatingSequence.Value = InitialCursorValue.INITIAL_CURSOR_VALUE + 1L;
            doneLatch.WaitOne();

            Assert.AreEqual(sequencer.GetCursor, expectedFullSequence + 1L);
        }

        [Test]
        [ExpectedException(typeof(InsufficientCapacityException))]//预期异常
        public void ShouldThrowInsufficientCapacityExceptionWhenSequencerIsFull()
        {
           // this.sequencer = newProducer(producerType, BUFFER_SIZE, new BlockingWaitStrategy());
            sequencer.AddGatingSequences(gatingSequence);
            for (int i = 0; i < BUFFER_SIZE; i++)
            {
                sequencer.Next();
            }
            sequencer.TryNext();
        }

        /// <summary>
        /// 计算剩余容量
        /// </summary>
        [Test]
        public void ShouldCalculateRemainingCapacity()
        {
            //this.sequencer = newProducer(producerType, BUFFER_SIZE, new BlockingWaitStrategy());
            sequencer.AddGatingSequences(gatingSequence);
            Assert.AreEqual(sequencer.RemainingCapacity(), (long)BUFFER_SIZE);
            for (int i = 1; i < BUFFER_SIZE; i++)
            {
                sequencer.Next();
                Assert.AreEqual(sequencer.RemainingCapacity(), (long)(BUFFER_SIZE - i));
            }
        }

        [Test]
        public void ShouldNotBeAvailableUntilPublished()
        {
            //this.sequencer = newProducer(producerType, BUFFER_SIZE, new BlockingWaitStrategy());
            var next = sequencer.Next(6);
            for (int i = 0; i <= 5; i++)
            {
                Assert.AreEqual(sequencer.IsAvailable(i), false);
            }
            sequencer.Publish(next - (6 - 1), next);
            for (int i = 0; i <= 5; i++)
            {
                Assert.AreEqual(sequencer.IsAvailable(i), true);
            }
            Assert.AreEqual(sequencer.IsAvailable(6), false);
        }

        [Test]
        public void ShouldNotifyWaitStrategyOnPublish()
        {
            //var waitStrategy = new Mock<IWaitStrategy>();
            //var mock = waitStrategy.Setup(e => new BlockingWaitStrategy());
            //var sequencer = newProducer(producerType, BUFFER_SIZE, waitStrategy.Object);
            //mock.Callback(() => waitStrategy.Object.signalAllWhenBlocking());

            //sequencer.Publish(sequencer.Next());

            //waitStrategy.Verify();
        }

        [Test]
        public void ShouldNotifyWaitStrategyOnPublishBatch()
        {
            //var waitStrategy = new Mock<IWaitStrategy>();
            //var mock = waitStrategy.Setup(e => new BlockingWaitStrategy());
            //var sequencer = newProducer(producerType, BUFFER_SIZE, waitStrategy.Object);
            //mock.Callback(() => waitStrategy.Object.signalAllWhenBlocking());

            //var next = sequencer.Next(4);
            //sequencer.Publish(next - (4 - 1), next);

            //waitStrategy.Verify();
        }

        [Test]
        public void ShouldWaitOnPublication()
        {
            //this.sequencer = newProducer(producerType, BUFFER_SIZE, new BlockingWaitStrategy());
            var barrier = sequencer.NewBarrier();
            var next = sequencer.Next(10);
            var lo = next - (10 - 1);
            var mid = next - 5;
            for (long l = lo; l < mid; l++)
            {
                sequencer.Publish(l);
            }
            Assert.AreEqual(barrier.WaitFor(-1), (long)(mid - 1));
            for (long l = mid; l <= next; l++)
            {
                sequencer.Publish(l);
            }
            Assert.AreEqual(barrier.WaitFor(-1), next);
        }

        [Test]
        public void ShouldTryNext()
        {
           // this.sequencer = newProducer(producerType, BUFFER_SIZE, new BlockingWaitStrategy());
            sequencer.AddGatingSequences(gatingSequence);

            for (int i = 0; i < BUFFER_SIZE; i++)
            {
                sequencer.Publish(sequencer.TryNext());
            }
            try
            {
                sequencer.TryNext();
                Assert.Fail("Should of thrown:");
            }
            catch
            {
            }
        }

        [Test]
        public void ShouldClaimSpecificSequence()
        {
           // this.sequencer = newProducer(producerType, BUFFER_SIZE, new BlockingWaitStrategy());
            var sequence = 14L;
            sequencer.Claim(sequence);
            sequencer.Publish(sequence);

            Assert.AreEqual(sequencer.Next(), sequence + 1);
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ShouldNotAllowBulkNextLessThanZero()
        {
           // this.sequencer = newProducer(producerType, BUFFER_SIZE, new BlockingWaitStrategy());
            sequencer.Next(0);
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ShouldNotAllowBulkTryNextLessThanZero()
        {
           // this.sequencer = newProducer(producerType, BUFFER_SIZE, new BlockingWaitStrategy());
            sequencer.TryNext(-1);
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ShouldNotAllowBulkTryNextOfZero()
        {
           // this.sequencer = newProducer(producerType, BUFFER_SIZE, new BlockingWaitStrategy());
            sequencer.TryNext(0);
        }

        private ISequencer newProducer(ProducerType producerType, int bufferSize, IWaitStrategy waitStrategy)
        {
            switch (producerType)
            {
                case ProducerType.SINGLE:
                    return new SingleProducerSequencer(bufferSize, waitStrategy);
                case ProducerType.MULTI:
                    return new MultiProducerSequencer(bufferSize, waitStrategy);
                default:
                    throw new ArgumentException(producerType.ToString());
            }
        }
    }
}
