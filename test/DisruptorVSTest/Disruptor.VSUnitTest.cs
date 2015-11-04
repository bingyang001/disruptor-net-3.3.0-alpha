using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Disruptor;
using Disruptor.Tests.Support;

namespace DisruptorVSTest
{
    /// <summary>
    /// Disruptor 的摘要说明
    /// </summary>
    [TestClass]
    public class DisruptorTest : IEventTranslator<LongEvent>
    {
        private static readonly int BUFFER_SIZE = 32;
        private readonly RingBuffer<LongEvent> ringBuffer = RingBuffer<LongEvent>.CreateSingleProducer(() => new LongEvent(0L), BUFFER_SIZE);
        public DisruptorTest()
        {
            //
            //TODO: 在此处添加构造函数逻辑
            //
        }

        private TestContext testContextInstance;

        /// <summary>
        ///获取或设置测试上下文，该上下文提供
        ///有关当前测试运行及其功能的信息。
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;        
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region 附加测试特性
        //
        // 编写测试时，可以使用以下附加特性:
        //
        // 在运行类中的第一个测试之前使用 ClassInitialize 运行代码
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // 在类中的所有测试都已运行之后使用 ClassCleanup 运行代码
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // 在运行每个测试之前，使用 TestInitialize 来运行代码
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // 在每个测试运行完之后，使用 TestCleanup 来运行代码
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

       [TestMethod]
        public void ShouldPublishEvent()
        {
            ringBuffer.AddGatingSequences(new NoOpEventProcessor<LongEvent>(ringBuffer).Sequence);

            ringBuffer.PublishEvent(this);
            ringBuffer.PublishEvent(this);

            Console.WriteLine("ringBuffer[0].Value={0}", ringBuffer[0].Value);
            Console.WriteLine("ringBuffer[0].Value={0}", ringBuffer[1].Value);
            Assert.AreEqual(ringBuffer[0].Value, 29L);
            Assert.AreEqual(ringBuffer[1].Value, 30L);
        }

       #region IEventTranslator<LongEvent> 成员

       public void TranslateTo(LongEvent @event, long sequence)
       {
           @event.Value = sequence + 29;
       }

       #endregion
    }
}
