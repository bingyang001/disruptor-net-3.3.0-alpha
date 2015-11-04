using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

using NUnit.Framework;
using Disruptor;

namespace DisruptorGeneralTest
{
    [TestFixture]
    public class RingBufferTest
    {
        private RingBuffer<StubEvent> ringBuffer;
        private ISequenceBarrier sequenceBarrier;



        [SetUp]
        public void Setup()
        {
            ringBuffer = RingBuffer<StubEvent>.CreateMultiProducer(() => new StubEvent(-1), 32);
            sequenceBarrier = ringBuffer.NewBarrier();
            ringBuffer.AddGatingSequences(new NoOpEventProcessor<StubEvent>(ringBuffer).Sequence);
        }

        [Test]
        public void ShouldClaimAndGet()
        {
            Assert.AreEqual(InitialCursorValue.INITIAL_CURSOR_VALUE, ringBuffer.GetCursor);
            var expectedEvent = new StubEvent(2701);
            ringBuffer.PublishEvent(new EventTranslatorTwoArg(), expectedEvent.Value, expectedEvent.TestString);
            var sequence = sequenceBarrier.WaitFor(0);
            Assert.AreEqual(0, sequence);
            var @event = ringBuffer[sequence];
            Assert.AreEqual(expectedEvent, @event);
            Assert.AreEqual(0, ringBuffer.GetCursor);
        }

        [Test]
        [Description("申请在单独的线程中获取")]
        public void ShouldClaimAndGetInSeparateThread()
        {
            var messages = GetEvents(0, 0);
            var expectedEvent = new StubEvent(2701);
            ringBuffer.PublishEvent(new EventTranslatorTwoArg(), expectedEvent.Value, expectedEvent.TestString);
            Assert.AreEqual(expectedEvent, messages.Result[0]);
        }

        [Test]
        [Description("获取多个消息")]
        public void ShouldClaimAndGetMultipleMessages()
        {
            var numMessages = ringBuffer.GetBufferSize;
            for (int i = 0; i < numMessages; i++)
            {
                ringBuffer.PublishEvent(StubEvent.TRANSLATOR, i, "");
            }
            var expectedSequence = numMessages - 1;
            var available = sequenceBarrier.WaitFor(expectedSequence);
            Assert.AreEqual(expectedSequence, available);
            for (var i = 0; i < numMessages; i++)
            {
                Assert.AreEqual(i, ringBuffer[i].Value);
            }
        }

        [Test]//有问题
        public void ShouldWrap()
        {
            //var ringBuffer = RingBuffer<StubEvent>.CreateSingleProducer(() => new StubEvent(-1), 32);
            var numMessages = ringBuffer.GetBufferSize;
            var offset = 1000;
            for (int i = 0; i < numMessages + offset; i++)
            {
                ringBuffer.PublishEvent(StubEvent.TRANSLATOR, i, "");
            }
            var expectedSequence = numMessages + offset - 1;
            var available = sequenceBarrier.WaitFor(expectedSequence);
            Assert.AreEqual(expectedSequence, available);
            for (int i = offset; i < numMessages + offset; i++)
            {
                Assert.AreEqual(i, ringBuffer[i].Value);
            }
        }

        [Test]
        public void ShouldPreventWrapping()
        {
            var sequence = new Sequence(InitialCursorValue.INITIAL_CURSOR_VALUE);
            var ringBuffer = RingBuffer<StubEvent>.CreateMultiProducer(() => new StubEvent(-1), 4);
            ringBuffer.AddGatingSequences(sequence);
            ringBuffer.PublishEvent(StubEvent.TRANSLATOR, 0, "0");
            ringBuffer.PublishEvent(StubEvent.TRANSLATOR, 1, "1");
            ringBuffer.PublishEvent(StubEvent.TRANSLATOR, 2, "2");
            ringBuffer.PublishEvent(StubEvent.TRANSLATOR, 3, "3");
            Assert.False(ringBuffer.TryPublishEvent(StubEvent.TRANSLATOR, 3, "3"));
        }

        [Test]
        public void ShouldThrowExceptionIfBufferIsFull()
        {
            ringBuffer.AddGatingSequences(new Sequence(ringBuffer.GetBufferSize));
            try
            {
                for (int i = 0; i < ringBuffer.GetBufferSize; i++)
                {
                    ringBuffer.Publish(ringBuffer.TryNext());
                }
            }
            catch (Exception e)
            {
                Assert.Fail("Should not of thrown exception");
            }

            try
            {
                ringBuffer.TryNext();
                Assert.Fail("Exception should have been thrown");
            }
            catch (InsufficientCapacityException e)
            {
            }
        }

        [Test]
        public void ShouldPreventPublishersOvertakingEventProcessorWrapPoint()
        {
            const int ringBufferSize = 4;
            var mre = new ManualResetEvent(false);
            var producerComplete = new _Volatile.Boolean(false);
            var ringBuffer = RingBuffer<StubEvent>.CreateMultiProducer(() => new StubEvent(-1), ringBufferSize);
            var processor = new TestEventProcessor(ringBuffer.NewBarrier());
            ringBuffer.AddGatingSequences(processor.Sequence);

            var thread = new Thread(
                () =>
                {
                    for (int i = 0; i <= ringBufferSize; i++) // produce 5 events
                    {
                        var sequence = ringBuffer.Next();
                        StubEvent evt = ringBuffer[sequence];
                        evt.Value = i;
                        ringBuffer.Publish(sequence);

                        if (i == 3) // unblock main thread after 4th event published
                        {
                            mre.Set();
                        }
                    }

                    producerComplete.WriteFullFence(true);
                });

            thread.Start();

            mre.WaitOne();
            Assert.AreEqual(ringBufferSize - 1, ringBuffer.GetCursor);
            Assert.IsFalse(producerComplete.ReadFullFence());

            processor.Run();
            thread.Join();

            Assert.IsTrue(producerComplete.ReadFullFence());
        }

        [Test]
        public void ShouldPublishEvent()
        {
            var ringBuffer = RingBuffer<Object[]>.CreateSingleProducer(() => new Object[1], 4);
            var translator = new NoArgEventTranslator();
            ringBuffer.PublishEvent(translator);
            ringBuffer.TryPublishEvent(translator);
            Assert.AreEqual(ringBuffer[0], new Object[] { 0L });
            Assert.AreEqual(ringBuffer[1], new Object[] { 1L });
        }

        [Test]
        public void ShouldPublishEventOneArg()
        {
            var ringBuffer = RingBuffer<Object[]>.CreateSingleProducer(() => new Object[1], 4);
            var translator = new OneArgEventTranslator();
            //ringBuffer.PublishEvent(translator, "Foo");
            ringBuffer.PublishEventOneArg((o, i, a) => o[0] = a + "-" + i, "Foo");
            ringBuffer.TryPublishEvent(translator, "Foo");
            Assert.AreEqual(ringBuffer[0], new Object[] { "Foo-0" });
            Assert.AreEqual(ringBuffer[1], new Object[] { "Foo-1" });
        }

        [Test]
        public void ShouldPublishEventTwoArg()
        {
            var ringBuffer = RingBuffer<Object[]>.CreateSingleProducer(() => new Object[1], 4);
            ringBuffer.PublishEventTwoArg((o, sequence, arg0, arg1) => o[0] = arg0 + arg1 + "-" + sequence, "Foo", "Bar");
            ringBuffer.PublishEventTwoArg((o, sequence, arg0, arg1) => o[0] = arg0 + arg1 + "-" + sequence, "Foo", "Bar");

            Assert.AreEqual(ringBuffer[0], new Object[] { "FooBar-0" });
            Assert.AreEqual(ringBuffer[1], new Object[] { "FooBar-1" });
        }

        [Test]
        public void ShouldPublishEventThreeArg()
        {
            var ringBuffer = RingBuffer<Object[]>.CreateSingleProducer(() => new Object[1], 4);

            ringBuffer.PublishEventThreeArg((o, sequence, arg0, arg1, arg2) => o[0] = arg0 + arg1 + arg2 + "-" + sequence, "Foo", "Bar", "Baz");
            ringBuffer.PublishEventThreeArg((o, sequence, arg0, arg1, arg2) => o[0] = arg0 + arg1 + arg2 + "-" + sequence, "Foo", "Bar", "Baz");

            Assert.AreEqual(ringBuffer[0], new Object[] { "FooBarBaz-0" });
            Assert.AreEqual(ringBuffer[1], new Object[] { "FooBarBaz-1" });
        }

        [Test]
        public void ShouldPublishEventVarArg()
        {
            var ringBuffer = RingBuffer<Object[]>.CreateSingleProducer(() => new Object[1], 4);
            ringBuffer.PublishEventMultiArg((o, i, arg) => o[0] = String.Join(" ", arg).Replace(" ", "") + "-" + i, "A", "B", "C", "D");
            ringBuffer.PublishEventMultiArg((o, i, arg) => o[0] = String.Join(" ", arg).Replace(" ", "") + "-" + i, "A", "B", "C", "D");

            Assert.AreEqual(ringBuffer[0], new Object[] { "ABCD-0" });
            Assert.AreEqual(ringBuffer[1], new Object[] { "ABCD-1" });
        }

        [Test]
        public void ShouldPublishEvents()
        {
            var ringBuffer = RingBuffer<Object[]>.CreateSingleProducer(() => new Object[1], 4);
            var eventTranslator = new NoArgEventTranslator();
            var translators = new IEventTranslator<Object[]>[] { eventTranslator, eventTranslator };
            ringBuffer.PublishEvents(translators);
            Assert.True(ringBuffer.TryPublishEvents(translators));
            Assert.AreEqual(ringBuffer[0], new Object[] { 0L });
            Assert.AreEqual(ringBuffer[1], new Object[] { 1L });
            Assert.AreEqual(ringBuffer[2], new Object[] { 2L });
            Assert.AreEqual(ringBuffer[3], new Object[] { 3L });
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ShouldNotPublishEventsIfBatchIsLargerThanRingBuffer()
        {
            var ringBuffer = RingBuffer<Object[]>.CreateSingleProducer(() => new Object[1], 4);
            var eventTranslator = new NoArgEventTranslator();
            var translators = new IEventTranslator<Object[]>[] { eventTranslator, eventTranslator, eventTranslator, eventTranslator, eventTranslator };
            try
            {
                ringBuffer.TryPublishEvents(translators);
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [Test]
        public void ShouldPublishEventsWithBatchSizeOfOne()
        {
            var ringBuffer = RingBuffer<Object[]>.CreateSingleProducer(() => new Object[1], 4);
            var eventTranslator = new NoArgEventTranslator();
            var translators = new IEventTranslator<Object[]>[] { eventTranslator, eventTranslator, eventTranslator };
            ringBuffer.PublishEvents(translators, 0, 1);
            Assert.True(ringBuffer.TryPublishEvents(translators, 0, 1));
            Assert.AreEqual(ringBuffer[0], new Object[] { 0L });
            Assert.AreEqual(ringBuffer[1], new Object[] { 1L });
            Assert.AreEqual(ringBuffer[2], new Object[] { null });
            Assert.AreEqual(ringBuffer[3], new Object[] { null });
        }

        [Test]
        public void ShouldPublishEventsWithinBatch()
        {
            var ringBuffer = RingBuffer<Object[]>.CreateSingleProducer(() => new Object[1], 4);
            var eventTranslator = new NoArgEventTranslator();
            var translators = new IEventTranslator<Object[]>[] { eventTranslator, eventTranslator, eventTranslator };
            ringBuffer.PublishEvents(translators, 1, 2);
            Assert.True(ringBuffer.TryPublishEvents(translators, 1, 2));
            Assert.AreEqual(ringBuffer[0], new Object[] { 0L });
            Assert.AreEqual(ringBuffer[1], new Object[] { 1L });
            Assert.AreEqual(ringBuffer[2], new Object[] { 2L });
            Assert.AreEqual(ringBuffer[3], new Object[] { 3L });
        }

        [Test]
        public void ShouldPublishEventsOneArg()
        {
            var ringBuffer = RingBuffer<Object[]>.CreateSingleProducer(() => new Object[1], 4);
            var translator = new OneArgEventTranslator();
            ringBuffer.PublishEvents(translator, new String[] { "Foo", "Foo" });
            Assert.True(ringBuffer.TryPublishEvents(translator, new String[] { "Foo", "Foo" }));

            Assert.AreEqual(ringBuffer[0], new Object[] { "Foo-0" });
            Assert.AreEqual(ringBuffer[1], new Object[] { "Foo-1" });
            Assert.AreEqual(ringBuffer[2], new Object[] { "Foo-2" });
            Assert.AreEqual(ringBuffer[3], new Object[] { "Foo-3" });
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ShouldNotPublishEventsOneArgIfBatchIsLargerThanRingBuffer()
        {
            var ringBuffer = RingBuffer<Object[]>.CreateSingleProducer(() => new Object[1], 4);
            var translator = new OneArgEventTranslator();
            try
            {
                ringBuffer.TryPublishEvents(translator, new String[] { "Foo", "Foo", "Foo", "Foo", "Foo" });
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [Test]
        public void ShouldPublishEventsOneArgBatchSizeOfOne()
        {
            var ringBuffer = RingBuffer<Object[]>.CreateSingleProducer(() => new Object[1], 4);
            var translator = new OneArgEventTranslator();
            ringBuffer.PublishEvents(translator, 0, 1, new String[] { "Foo", "Foo" });
            Assert.True(ringBuffer.TryPublishEvents(translator, 0, 1, new String[] { "Foo", "Foo" }));
            Assert.AreEqual(ringBuffer[0], new Object[] { "Foo-0" });
            Assert.AreEqual(ringBuffer[1], new Object[] { "Foo-1" });
            Assert.AreEqual(ringBuffer[2], new Object[] { null });
            Assert.AreEqual(ringBuffer[3], new Object[] { null });
        }

        [Test]
        public void ShouldPublishEventsOneArgWithinBatch()
        {
            var ringBuffer = RingBuffer<Object[]>.CreateSingleProducer(() => new Object[1], 4);
            var translator = new OneArgEventTranslator();
            ringBuffer.PublishEvents(translator, 1, 2, new String[] { "Foo", "Foo", "Foo" });
            Assert.True(ringBuffer.TryPublishEvents(translator, 1, 2, new String[] { "Foo", "Foo", "Foo" }));

            Assert.AreEqual(ringBuffer[0], new Object[] { "Foo-0" });
            Assert.AreEqual(ringBuffer[1], new Object[] { "Foo-1" });
            Assert.AreEqual(ringBuffer[2], new Object[] { "Foo-2" });
            Assert.AreEqual(ringBuffer[3], new Object[] { "Foo-3" });
        }

        [Test]
        public void ShouldPublishEventsTwoArg()
        {
            var ringBuffer = RingBuffer<Object[]>.CreateSingleProducer(() => new Object[1], 4);
            var translator = new TwoArgEventTranslator();
            ringBuffer.PublishEvents(translator, new String[] { "Foo", "Foo" }, new String[] { "Bar", "Bar" });
            ringBuffer.TryPublishEvents(translator, new String[] { "Foo", "Foo" }, new String[] { "Bar", "Bar" });
            Assert.AreEqual(ringBuffer[0], new Object[] { "FooBar-0" });
            Assert.AreEqual(ringBuffer[1], new Object[] { "FooBar-1" });
            Assert.AreEqual(ringBuffer[2], new Object[] { "FooBar-2" });
            Assert.AreEqual(ringBuffer[3], new Object[] { "FooBar-3" });
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ShouldNotPublishEventsITwoArgIfBatchSizeIsBiggerThanRingBuffer()
        {
            var ringBuffer = RingBuffer<Object[]>.CreateSingleProducer(() => new Object[1], 4);
            var translator = new TwoArgEventTranslator();
            try
            {
                ringBuffer.TryPublishEvents(translator,
                                            new String[] { "Foo", "Foo", "Foo", "Foo", "Foo" },
                                            new String[] { "Bar", "Bar", "Bar", "Bar", "Bar" });
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [Test]
        public void ShouldPublishEventsTwoArgWithBatchSizeOfOne()
        {
            var ringBuffer = RingBuffer<Object[]>.CreateSingleProducer(() => new Object[1], 4);
            var translator = new TwoArgEventTranslator();

            ringBuffer.PublishEvents(translator, 0, 1, new String[] { "Foo0", "Foo1" }, new String[] { "Bar0", "Bar1" });
            ringBuffer.TryPublishEvents(translator, 0, 1, new String[] { "Foo2", "Foo3" }, new String[] { "Bar2", "Bar3" });

            Assert.AreEqual(ringBuffer[0][0], "Foo0Bar0-0");
            Assert.AreEqual(ringBuffer[1][0], "Foo2Bar2-1");
            Assert.AreEqual(ringBuffer[2][0], null);
            Assert.AreEqual(ringBuffer[3][0], null);
        }

        [Test]
        public void ShouldPublishEventsTwoArgWithinBatch()
        {
            var ringBuffer = RingBuffer<Object[]>.CreateSingleProducer(() => new Object[1], 4);
            var translator = new TwoArgEventTranslator();

            ringBuffer.PublishEvents(translator, 1, 2, new String[] { "Foo0", "Foo1", "Foo2" }, new String[] { "Bar0", "Bar1", "Bar2" });
            ringBuffer.TryPublishEvents(translator, 1, 2, new String[] { "Foo3", "Foo4", "Foo5" }, new String[] { "Bar3", "Bar4", "Bar5" });

            Assert.AreEqual(ringBuffer[0][0], "Foo1Bar1-0");
            Assert.AreEqual(ringBuffer[1][0], "Foo2Bar2-1");
            Assert.AreEqual(ringBuffer[2][0], "Foo4Bar4-2");
            Assert.AreEqual(ringBuffer[3][0], "Foo5Bar5-3");
        }

        [Test]
        public void ShouldPublishEventsThreeArg()
        {
            var ringBuffer = RingBuffer<Object[]>.CreateSingleProducer(() => new Object[1], 4);
            var translator = new ThreeArgEventTranslator();

            ringBuffer.PublishEvents(translator, new String[] { "Foo", "Foo" }, new String[] { "Bar", "Bar" }, new String[] { "Baz", "Baz" });
            ringBuffer.TryPublishEvents(translator, new String[] { "Foo", "Foo" }, new String[] { "Bar", "Bar" }, new String[] { "Baz", "Baz" });

            Assert.AreEqual(ringBuffer[0][0], "FooBarBaz-0");
            Assert.AreEqual(ringBuffer[1][0], "FooBarBaz-1");
            Assert.AreEqual(ringBuffer[2][0], "FooBarBaz-2");
            Assert.AreEqual(ringBuffer[3][0], "FooBarBaz-3");
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ShouldNotPublishEventsThreeArgIfBatchIsLargerThanRingBuffer()
        {
            var ringBuffer = RingBuffer<Object[]>.CreateSingleProducer(() => new Object[1], 4);
            var translator = new ThreeArgEventTranslator();
            try
            {
                ringBuffer.TryPublishEvents(translator,
                                            new String[] { "Foo", "Foo", "Foo", "Foo", "Foo" },
                                            new String[] { "Bar", "Bar", "Bar", "Bar", "Bar" },
                                            new String[] { "Baz", "Baz", "Baz", "Baz", "Baz" });
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [Test]
        public void ShouldPublishEventsThreeArgBatchSizeOfOne()
        {
            var ringBuffer = RingBuffer<Object[]>.CreateSingleProducer(() => new Object[1], 4);
            var translator = new ThreeArgEventTranslator();
            ringBuffer.PublishEvents(translator, 0, 1, new String[] { "Foo", "Foo" }, new String[] { "Bar", "Bar" }, new String[] { "Baz", "Baz" });
            ringBuffer.TryPublishEvents(translator, 0, 1, new String[] { "Foo", "Foo" }, new String[] { "Bar", "Bar" }, new String[] { "Baz", "Baz" });
            Assert.AreEqual(ringBuffer[0][0], "FooBarBaz-0");
            Assert.AreEqual(ringBuffer[1][0], "FooBarBaz-1");
            Assert.AreEqual(ringBuffer[2][0], null);
            Assert.AreEqual(ringBuffer[3][0], null);
        }

        [Test]
        public void ShouldPublishEventsThreeArgWithinBatch()
        {
            var ringBuffer = RingBuffer<Object[]>.CreateSingleProducer(() => new Object[1], 4);
            var translator = new ThreeArgEventTranslator();
            ringBuffer.PublishEvents(translator, 1, 2, new String[] { "Foo0", "Foo1", "Foo2" }, new String[] { "Bar0", "Bar1", "Bar2" }, new String[] { "Baz0", "Baz1", "Baz2" });
            Assert.True(ringBuffer.TryPublishEvents(translator, 1, 2, new String[] { "Foo3", "Foo4", "Foo5" }, new String[] { "Bar3", "Bar4", "Bar5" }, new String[] { "Baz3", "Baz4", "Baz5" }));
            Assert.AreEqual(ringBuffer[0][0], "Foo1Bar1Baz1-0");
            Assert.AreEqual(ringBuffer[1][0], "Foo2Bar2Baz2-1");
            Assert.AreEqual(ringBuffer[2][0], "Foo4Bar4Baz4-2");
            Assert.AreEqual(ringBuffer[3][0], "Foo5Bar5Baz5-3");
        }

        [Test]
        public void ShouldPublishEventsVarArg()
        {
            var ringBuffer = RingBuffer<Object[]>.CreateSingleProducer(() => new Object[1], 4);
            var translator = new VarArgEventTranslator();
            ringBuffer.PublishEvents(translator, new String[] { "Foo", "Bar", "Baz", "Bam" }, new String[] { "Foo", "Bar", "Baz", "Bam" });
            Assert.True(ringBuffer.TryPublishEvents(translator, new String[] { "Foo", "Bar", "Baz", "Bam" }, new String[] { "Foo", "Bar", "Baz", "Bam" }));
            Assert.AreEqual(ringBuffer[0][0], "FooBarBazBam-0");
            Assert.AreEqual(ringBuffer[1][0], "FooBarBazBam-1");
            Assert.AreEqual(ringBuffer[2][0], "FooBarBazBam-2");
            Assert.AreEqual(ringBuffer[3][0], "FooBarBazBam-3");
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        [Description("不应该批量发布大于环形缓冲区大小的消息")]
        public void ShouldNotPublishEventsVarArgIfBatchIsLargerThanRingBuffer()
        {
            var ringBuffer = RingBuffer<Object[]>.CreateSingleProducer(() => new Object[1], 4);
            var translator = new VarArgEventTranslator();
            try
            {
                ringBuffer.TryPublishEvents(translator,
                                            new String[] { "Foo", "Bar", "Baz", "Bam" },
                                            new String[] { "Foo", "Bar", "Baz", "Bam" },
                                            new String[] { "Foo", "Bar", "Baz", "Bam" },
                                            new String[] { "Foo", "Bar", "Baz", "Bam" },
                                            new String[] { "Foo", "Bar", "Baz", "Bam" });
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [Test]
        public void ShouldPublishEventsVarArgBatchSizeOfOne()
        {
            var ringBuffer = RingBuffer<Object[]>.CreateSingleProducer(() => new Object[1], 4);
            var translator = new VarArgEventTranslator();
            ringBuffer.PublishEvents(translator, 0, 1, new String[] { "Foo", "Bar", "Baz", "Bam" }, new String[] { "Foo", "Bar", "Baz", "Bam" });
            Assert.True(ringBuffer.TryPublishEvents(translator, 0, 1, new String[] { "Foo", "Bar", "Baz", "Bam" }, new String[] { "Foo", "Bar", "Baz", "Bam" }));

            Assert.AreEqual(ringBuffer[0][0], "FooBarBazBam-0");
            Assert.AreEqual(ringBuffer[1][0], "FooBarBazBam-1");
            Assert.AreEqual(ringBuffer[2][0], null);
            Assert.AreEqual(ringBuffer[3][0], null);
        }

        [Test]
        public void ShouldPublishEventsVarArgWithinBatch()
        {
            var ringBuffer = RingBuffer<Object[]>.CreateSingleProducer(() => new Object[1], 4);
            var translator = new VarArgEventTranslator();
            ringBuffer.PublishEvents(translator, 1, 2, new String[] { "Foo0", "Bar0", "Baz0", "Bam0" }, new String[] { "Foo1", "Bar1", "Baz1", "Bam1" }, new String[] { "Foo2", "Bar2", "Baz2", "Bam2" });
            Assert.True(ringBuffer.TryPublishEvents(translator, 1, 2, new String[] { "Foo3", "Bar3", "Baz3", "Bam3" }, new String[] { "Foo4", "Bar4", "Baz4", "Bam4" }, new String[] { "Foo5", "Bar5", "Baz5", "Bam5" }));
            Assert.AreEqual(ringBuffer[0][0], "Foo1Bar1Baz1Bam1-0");
            Assert.AreEqual(ringBuffer[1][0], "Foo2Bar2Baz2Bam2-1");
            Assert.AreEqual(ringBuffer[2][0], "Foo4Bar4Baz4Bam4-2");
            Assert.AreEqual(ringBuffer[3][0], "Foo5Bar5Baz5Bam5-3");
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        [Description("不应该批量发布大小小于0的消息")]
        public void ShouldNotPublishEventsWhenBatchSizeIs0()
        {
            var ringBuffer = RingBuffer<Object[]>.CreateSingleProducer(() => new Object[1], 4);
            var translator = new NoArgEventTranslator();
            try
            {
                ringBuffer.PublishEvents(new IEventTranslator<Object[]>[] { translator, translator, translator, translator }, 1, 0);
                ringBuffer.TryPublishEvents(new IEventTranslator<Object[]>[] { translator, translator, translator, translator }, 1, 0);
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }

        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        [Description("不应该批量发布超过事件源大小的事件")]
        public void ShouldNotPublishEventsWhenBatchExtendsPastEndOfArray()
        {
            var ringBuffer = RingBuffer<Object[]>.CreateSingleProducer(() => new Object[1], 4);
            var translator = new NoArgEventTranslator();
            try
            {
                ringBuffer.PublishEvents(new IEventTranslator<Object[]>[] { translator, translator, translator }, 1, 3);
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ShouldNotTryPublishEventsWhenBatchExtendsPastEndOfArray()
        {
            var ringBuffer = RingBuffer<Object[]>.CreateSingleProducer(() => new Object[1], 4);
            var translator = new NoArgEventTranslator();

            try
            {
                ringBuffer.TryPublishEvents(new IEventTranslator<Object[]>[] { translator, translator, translator }, 1, 3);
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ShouldNotPublishEventsWhenBatchSizeIsNegative()
        {
            var ringBuffer = RingBuffer<Object[]>.CreateSingleProducer(() => new Object[1], 4);
            var translator = new NoArgEventTranslator();
            try
            {
                ringBuffer.PublishEvents(new IEventTranslator<Object[]>[] { translator, translator, translator, translator }, 1, -1);
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ShouldNotTryPublishEventsWhenBatchSizeIsNegative()
        {
            var ringBuffer = RingBuffer<Object[]>.CreateSingleProducer(() => new Object[1], 4);
            var translator = new NoArgEventTranslator();

            try
            {
                ringBuffer.TryPublishEvents(new IEventTranslator<Object[]>[] { translator, translator, translator, translator }, 1, -1);
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ShouldNotPublishEventsWhenBatchStartsAtIsNegative()
        {
            var ringBuffer = RingBuffer<Object[]>.CreateSingleProducer(() => new Object[1], 4);
            var translator = new NoArgEventTranslator();

            try
            {
                ringBuffer.TryPublishEvents(new IEventTranslator<Object[]>[] { translator, translator, translator, translator }, -1, 2);
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ShouldNotTryPublishEventsWhenBatchStartsAtIsNegative()
        {
            var ringBuffer = RingBuffer<Object[]>.CreateSingleProducer(() => new Object[1], 4);
            var translator = new NoArgEventTranslator();
            try
            {
                ringBuffer.TryPublishEvents(new IEventTranslator<Object[]>[] { translator, translator, translator, translator }, -1, 2);
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }


        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ShouldNotPublishEventsOneArgWhenBatchSizeIs0()
        {
            var ringBuffer = RingBuffer<Object[]>.CreateSingleProducer(() => new Object[1], 4);
            var translator = new OneArgEventTranslator();
            try
            {
                ringBuffer.PublishEvents(translator, 1, 0, new String[] { "Foo", "Foo" });
                Assert.True(ringBuffer.TryPublishEvents(translator, 1, 0, new String[] { "Foo", "Foo" }));
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ShouldNotPublishEventsOneArgWhenBatchExtendsPastEndOfArray()
        {
            var ringBuffer = RingBuffer<Object[]>.CreateSingleProducer(() => new Object[1], 4);
            var translator = new OneArgEventTranslator();
            try
            {
                ringBuffer.PublishEvents(translator, 1, 3, new String[] { "Foo", "Foo" });
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ShouldNotPublishEventsOneArgWhenBatchSizeIsNegative()
        {
            var ringBuffer = RingBuffer<Object[]>.CreateSingleProducer(() => new Object[1], 4);
            var translator = new OneArgEventTranslator();
            try
            {
                ringBuffer.PublishEvents(translator, 1, -1, new String[] { "Foo", "Foo" });
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ShouldNotPublishEventsOneArgWhenBatchStartsAtIsNegative()
        {
            var ringBuffer = RingBuffer<Object[]>.CreateSingleProducer(() => new Object[1], 4);
            var translator = new OneArgEventTranslator();
            try
            {
                ringBuffer.PublishEvents(translator, -1, 2, new String[] { "Foo", "Foo" });
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ShouldNotTryPublishEventsOneArgWhenBatchExtendsPastEndOfArray()
        {
            var ringBuffer = RingBuffer<Object[]>.CreateSingleProducer(() => new Object[1], 4);
            var translator = new OneArgEventTranslator();
            try
            {
                ringBuffer.PublishEvents(translator, 1, 3, new String[] { "Foo", "Foo" });
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ShouldNotTryPublishEventsOneArgWhenBatchSizeIsNegative()
        {
            var ringBuffer = RingBuffer<Object[]>.CreateSingleProducer(() => new Object[1], 4);
            var translator = new OneArgEventTranslator();
            try
            {
                ringBuffer.PublishEvents(translator, 1, -1, new String[] { "Foo", "Foo" });
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ShouldNotTryPublishEventsOneArgWhenBatchStartsAtIsNegative()
        {

            var ringBuffer = RingBuffer<Object[]>.CreateSingleProducer(() => new Object[1], 4);
            var translator = new OneArgEventTranslator();
            try
            {
                ringBuffer.PublishEvents(translator, -1, 2, new String[] { "Foo", "Foo" });
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ShouldNotPublishEventsTwoArgWhenBatchSizeIs0()
        {
            var ringBuffer = RingBuffer<Object[]>.CreateSingleProducer(() => new Object[1], 4);
            var translator = new TwoArgEventTranslator();
            try
            {
                ringBuffer.PublishEvents(translator, 1, 0, new String[] { "Foo", "Foo" }, new String[] { "Bar", "Bar" });
                Assert.False(ringBuffer.TryPublishEvents(translator, 1, 0, new String[] { "Foo", "Foo" },
                                                        new String[] { "Bar", "Bar" }));
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ShouldNotPublishEventsTwoArgWhenBatchExtendsPastEndOfArray()
        {
            var ringBuffer = RingBuffer<Object[]>.CreateSingleProducer(() => new Object[1], 4);
            var translator = new TwoArgEventTranslator();
            try
            {
                ringBuffer.PublishEvents(translator, 1, 3, new String[] { "Foo", "Foo" }, new String[] { "Bar", "Bar" });
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ShouldNotPublishEventsTwoArgWhenBatchSizeIsNegative()
        {
            var ringBuffer = RingBuffer<Object[]>.CreateSingleProducer(() => new Object[1], 4);
            var translator = new TwoArgEventTranslator();
            try
            {
                ringBuffer.PublishEvents(translator, 1, -1, new String[] { "Foo", "Foo" }, new String[] { "Bar", "Bar" });
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ShouldNotPublishEventsTwoArgWhenBatchStartsAtIsNegative()
        {
            var ringBuffer = RingBuffer<Object[]>.CreateSingleProducer(() => new Object[1], 4);
            var translator = new TwoArgEventTranslator();
            try
            {
                ringBuffer.TryPublishEvents(translator, -1, 2, new String[] { "Foo", "Foo" }, new String[] { "Bar", "Bar" });
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ShouldNotTryPublishEventsTwoArgWhenBatchExtendsPastEndOfArray()
        {
            var ringBuffer = RingBuffer<Object[]>.CreateSingleProducer(() => new Object[1], 4);
            var translator = new TwoArgEventTranslator();
            try
            {
                ringBuffer.TryPublishEvents(translator, 1, 3, new String[] { "Foo", "Foo" }, new String[] { "Bar", "Bar" });
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ShouldNotTryPublishEventsTwoArgWhenBatchSizeIsNegative()
        {
            var ringBuffer = RingBuffer<Object[]>.CreateSingleProducer(() => new Object[1], 4);
            var translator = new TwoArgEventTranslator();
            try
            {
                ringBuffer.TryPublishEvents(translator, 1, -1, new String[] { "Foo", "Foo" }, new String[] { "Bar", "Bar" });
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ShouldNotTryPublishEventsTwoArgWhenBatchStartsAtIsNegative()
        {
            var ringBuffer = RingBuffer<Object[]>.CreateSingleProducer(() => new Object[1], 4);
            var translator = new TwoArgEventTranslator();
            try
            {
                ringBuffer.TryPublishEvents(translator, -1, 2, new String[] { "Foo", "Foo" }, new String[] { "Bar", "Bar" });
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ShouldNotPublishEventsThreeArgWhenBatchSizeIs0()
        {
            var ringBuffer = RingBuffer<Object[]>.CreateSingleProducer(() => new Object[1], 4);
            var translator = new ThreeArgEventTranslator();

            try
            {
                ringBuffer.PublishEvents(translator, 1, 0, new String[] { "Foo", "Foo" }, new String[] { "Bar", "Bar" },
                                         new String[] { "Baz", "Baz" });
                Assert.False(ringBuffer.TryPublishEvents(translator, 1, 0, new String[] { "Foo", "Foo" },
                                                        new String[] { "Bar", "Bar" }, new String[] { "Baz", "Baz" }));
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ShouldNotPublishEventsThreeArgWhenBatchExtendsPastEndOfArray()
        {
            var ringBuffer = RingBuffer<Object[]>.CreateSingleProducer(() => new Object[1], 4);
            var translator = new ThreeArgEventTranslator();
            try
            {
                ringBuffer.PublishEvents(translator, 1, 3, new String[] { "Foo", "Foo" }, new String[] { "Bar", "Bar" },
                                         new String[] { "Baz", "Baz" });
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ShouldNotPublishEventsThreeArgWhenBatchSizeIsNegative()
        {
            var ringBuffer = RingBuffer<Object[]>.CreateSingleProducer(() => new Object[1], 4);
            var translator = new ThreeArgEventTranslator();
            try
            {
                ringBuffer.PublishEvents(translator, 1, -1, new String[] { "Foo", "Foo" }, new String[] { "Bar", "Bar" },
                                         new String[] { "Baz", "Baz" });
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ShouldNotPublishEventsThreeArgWhenBatchStartsAtIsNegative()
        {
            var ringBuffer = RingBuffer<Object[]>.CreateSingleProducer(() => new Object[1], 4);
            var translator = new ThreeArgEventTranslator();
            try
            {
                ringBuffer.PublishEvents(translator, -1, 2, new String[] { "Foo", "Foo" }, new String[] { "Bar", "Bar" },
                                         new String[] { "Baz", "Baz" });
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ShouldNotTryPublishEventsThreeArgWhenBatchExtendsPastEndOfArray()
        {
            var ringBuffer = RingBuffer<Object[]>.CreateSingleProducer(() => new Object[1], 4);
            var translator = new ThreeArgEventTranslator();
            try
            {
                ringBuffer.TryPublishEvents(translator, 1, 3, new String[] { "Foo", "Foo" }, new String[] { "Bar", "Bar" },
                                            new String[] { "Baz", "Baz" });
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ShouldNotTryPublishEventsThreeArgWhenBatchSizeIsNegative()
        {
            var ringBuffer = RingBuffer<Object[]>.CreateSingleProducer(() => new Object[1], 4);
            var translator = new ThreeArgEventTranslator();
            try
            {
                ringBuffer.TryPublishEvents(translator, 1, -1, new String[] { "Foo", "Foo" },
                                            new String[] { "Bar", "Bar" }, new String[] { "Baz", "Baz" });
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ShouldNotTryPublishEventsThreeArgWhenBatchStartsAtIsNegative()
        {
            var ringBuffer = RingBuffer<Object[]>.CreateSingleProducer(() => new Object[1], 4);
            var translator = new ThreeArgEventTranslator();
            try
            {
                ringBuffer.TryPublishEvents(translator, -1, 2, new String[] { "Foo", "Foo" },
                                            new String[] { "Bar", "Bar" }, new String[] { "Baz", "Baz" });
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ShouldNotPublishEventsVarArgWhenBatchSizeIs0()
        {
            var ringBuffer = RingBuffer<Object[]>.CreateSingleProducer(() => new Object[1], 4);
            var translator = new VarArgEventTranslator();
            try
            {
                ringBuffer.PublishEvents(translator, 1, 0, new String[] { "Foo0", "Bar0", "Baz0", "Bam0" },
                                         new String[] { "Foo1", "Bar1", "Baz1", "Bam1" }, new String[] { "Foo2", "Bar2",
                                                                                                    "Baz2", "Bam2" });
                Assert.False(ringBuffer.TryPublishEvents(translator, 1, 0, new String[] { "Foo0", "Bar0", "Baz0", "Bam0" },
                                                        new String[] { "Foo1", "Bar1", "Baz1", "Bam1" },
                                                        new String[] { "Foo2", "Bar2", "Baz2", "Bam2" }));
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ShouldNotPublishEventsVarArgWhenBatchExtendsPastEndOfArray()
        {
            var ringBuffer = RingBuffer<Object[]>.CreateSingleProducer(() => new Object[1], 4);
            var translator = new VarArgEventTranslator();
            try
            {
                ringBuffer.PublishEvents(translator, 1, 3, new String[] { "Foo0", "Bar0", "Baz0", "Bam0" },
                                         new String[] { "Foo1", "Bar1", "Baz1", "Bam1" }, new String[] { "Foo2", "Bar2",
                                                                                                    "Baz2", "Bam2" });
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ShouldNotPublishEventsVarArgWhenBatchSizeIsNegative()
        {
            var ringBuffer = RingBuffer<Object[]>.CreateSingleProducer(() => new Object[1], 4);
            var translator = new VarArgEventTranslator();
            try
            {
                ringBuffer.PublishEvents(translator, 1, -1, new String[] { "Foo0", "Bar0", "Baz0", "Bam0" },
                                         new String[] { "Foo1", "Bar1", "Baz1", "Bam1" }, new String[] { "Foo2", "Bar2",
                                                                                                    "Baz2", "Bam2" });
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ShouldNotPublishEventsVarArgWhenBatchStartsAtIsNegative()
        {
            var ringBuffer = RingBuffer<Object[]>.CreateSingleProducer(() => new Object[1], 4);
            var translator = new VarArgEventTranslator();
            try
            {
                ringBuffer.PublishEvents(translator, -1, 2, new String[] { "Foo0", "Bar0", "Baz0", "Bam0" },
                                         new String[] { "Foo1", "Bar1", "Baz1", "Bam1" }, new String[] { "Foo2", "Bar2",
                                                                                                    "Baz2", "Bam2" });
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ShouldNotTryPublishEventsVarArgWhenBatchExtendsPastEndOfArray()
        {
            var ringBuffer = RingBuffer<Object[]>.CreateSingleProducer(() => new Object[1], 4);
            var translator = new VarArgEventTranslator();
            try
            {
                ringBuffer.TryPublishEvents(translator, 1, 3, new String[] { "Foo0", "Bar0", "Baz0", "Bam0" },
                                            new String[] { "Foo1", "Bar1", "Baz1", "Bam1" }, new String[] { "Foo2", "Bar2",
                                                                                                       "Baz2", "Bam2" });
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ShouldNotTryPublishEventsVarArgWhenBatchSizeIsNegative()
        {
            var ringBuffer = RingBuffer<Object[]>.CreateSingleProducer(() => new Object[1], 4);
            var translator = new VarArgEventTranslator();
            try
            {
                ringBuffer.TryPublishEvents(translator, 1, -1, new String[] { "Foo0", "Bar0", "Baz0", "Bam0" },
                                            new String[] { "Foo1", "Bar1", "Baz1", "Bam1" }, new String[] { "Foo2", "Bar2",
                                                                                                       "Baz2", "Bam2" });
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ShouldNotTryPublishEventsVarArgWhenBatchStartsAtIsNegative()
        {
            var ringBuffer = RingBuffer<Object[]>.CreateSingleProducer(() => new Object[1], 4);
            var translator = new VarArgEventTranslator();
            try
            {
                ringBuffer.TryPublishEvents(translator, -1, 2, new String[] { "Foo0", "Bar0", "Baz0", "Bam0" },
                                            new String[] { "Foo1", "Bar1", "Baz1", "Bam1" }, new String[] { "Foo2", "Bar2",
                                                                                                       "Baz2", "Bam2" });
            }
            finally
            {
                AssertEmptyRingBuffer(ringBuffer);
            }
        }

        [Test]
        public void ShouldAddAndRemoveSequences()
        {
            var ringBuffer = RingBuffer<Object[]>.CreateSingleProducer(() => new Object[1], 16);
            var sequenceThree = new Sequence(-1);
            var sequenceSeven = new Sequence(-1);
            ringBuffer.AddGatingSequences(sequenceThree, sequenceSeven);

            for (int i = 0; i < 10; i++)
            {
                ringBuffer.Publish(ringBuffer.Next());
            }
            sequenceThree.Value = 3;
            sequenceSeven.Value = 7;

            Assert.AreEqual(ringBuffer.GetMinimumGatingSequence(), 3L);
            Assert.True(ringBuffer.RemoveGatingSequence(sequenceThree));
            Assert.AreEqual(ringBuffer.GetMinimumGatingSequence(), 7L);
        }


        [Test]
        public void ShouldHandleResetToAndNotWrapUnecessarilySingleProducer()
        {
            AssertHandleResetAndNotWrap(RingBuffer<StubEvent>.CreateSingleProducer(() => new StubEvent(0), 4));
        }

        [Test]
        public void ShouldHandleResetToAndNotWrapUnecessarilyMultiProducer()
        {
            AssertHandleResetAndNotWrap(RingBuffer<StubEvent>.CreateMultiProducer(() => new StubEvent(0), 4));
        }

        private void AssertEmptyRingBuffer(RingBuffer<Object[]> ringBuffer)
        {
            Assert.AreEqual(ringBuffer[0][0], null);
            Assert.AreEqual(ringBuffer[1][0], null);
            Assert.AreEqual(ringBuffer[2][0], null);
            Assert.AreEqual(ringBuffer[3][0], null);
        }

        private void AssertHandleResetAndNotWrap(RingBuffer<StubEvent> rb)
        {
            var sequence = new Sequence();
            rb.AddGatingSequences(sequence);

            for (int i = 0; i < 128; i++)
            {
                rb.Publish(rb.Next());
                sequence.IncrementAndGet();
            }

            Assert.AreEqual(rb.GetCursor, 127L);

            rb.ResetTo(31);
            sequence.LazySet( 31);

            for (int i = 0; i < 4; i++)
            {
                rb.Publish(rb.Next());
            }

            Assert.AreEqual(rb.HasAvailableCapacity(1), false);
        }

        private Task<List<StubEvent>> GetEvents(long initial, long toWaitFor)
        {
            var barrier = new Barrier(2);
            var dependencyBarrier = ringBuffer.NewBarrier();

            var testWaiter = new TestWaiter(barrier, dependencyBarrier, ringBuffer, initial, toWaitFor);
            var task = Task.Factory.StartNew(() => testWaiter.Call());

            barrier.SignalAndWait();

            return task;
        }
        private class VarArgEventTranslator : IEventTranslatorVararg<Object[]>
        {

            public void TranslateTo(Object[] @event, long sequence, params Object[] args)
            {
                @event[0] = (String)args[0] + args[1] + args[2] + args[3] + "-" + sequence;
            }
        }
        private class ThreeArgEventTranslator : IEventTranslatorThreeArg<Object[], String, String, String>
        {

            #region IEventTranslatorThreeArg<object[],string,string,string> 成员

            public void TranslateTo(object[] @event, long sequence, string arg0, string arg1, string arg2)
            {
                @event[0] = arg0 + arg1 + arg2 + "-" + sequence;
            }

            #endregion
        }
        private class OneArgEventTranslator : IEventTranslatorOneArg<Object[], String>
        {

            #region IEventTranslatorOneArg<object[],string> 成员

            public void TranslateTo(object[] @event, long sequence, string arg0)
            {
                @event[0] = arg0 + "-" + sequence;
            }

            #endregion
        }
        private class TwoArgEventTranslator : IEventTranslatorTwoArg<Object[], String, String>
        {

            public void TranslateTo(Object[] @event, long sequence, String arg0, String arg1)
            {
                @event[0] = arg0 + arg1 + "-" + sequence;
            }
        }
        private class NoArgEventTranslator : IEventTranslator<Object[]>
        {

            #region IEventTranslator<object[]> 成员

            public void TranslateTo(object[] @event, long sequence)
            {
                @event[0] = sequence;
            }

            #endregion
        }

        private class TestEventProcessor : IEventProcessor
        {
            private readonly ISequenceBarrier _sequenceBarrier;
            private readonly Sequence _sequence = new Sequence(InitialCursorValue.INITIAL_CURSOR_VALUE);
            private readonly _Volatile.Boolean running = new _Volatile.Boolean(false);
            public TestEventProcessor(ISequenceBarrier sequenceBarrier)
            {
                _sequenceBarrier = sequenceBarrier;
            }

            public Sequence Sequence
            {
                get { return _sequence; }
            }

            public void Halt()
            {
                running.WriteFullFence(false);
            }

            public void Run()
            {
                _sequenceBarrier.WaitFor(0L);
                _sequence.Value += 1;
            }

            #region IEventProcessor 成员


            public bool IsRunning()
            {
                return running.ReadFullFence();
            }

            #endregion
        }
    }
}
