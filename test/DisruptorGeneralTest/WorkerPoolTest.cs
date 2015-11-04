using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Disruptor;

namespace DisruptorGeneralTest
{
    [TestFixture]
    public class WorkerPoolTest
    {
        WorkerPool<VolatileLong> workpool1;
        WorkerPool<VolatileLong> workpool2;

        [SetUp]
        public void SetUp()
        {
            workpool1 = new WorkerPool<VolatileLong>(() => new VolatileLong()
                , new FatalExceptionHandler()
                , new AtomicLongWorkHandler()
                , new AtomicLongWorkHandler());

            workpool2 = new WorkerPool<VolatileLong>(() => new VolatileLong()
               , new FatalExceptionHandler()
               , new AtomicLongWorkHandler()
               , new AtomicLongWorkHandler());
        }

        [TestFixtureTearDown]
        public void Dowon()
        {
            workpool1.halt();
            workpool2.halt();
        }

        [Test]
        public void ShouldProcessEachMessageByOnlyOneWorker()
        {
            var ringBuffer = workpool1.start(TaskScheduler.Default);
            ringBuffer.Next();
            ringBuffer.Next();
            ringBuffer.Publish(0);
            ringBuffer.Publish(1);
           
             Thread.Sleep(500);

            Assert.AreEqual(ringBuffer[0].VL, 1L);
            Assert.AreEqual(ringBuffer[1].VL, 1L);

        }

        [Test]
        public void ShouldProcessOnlyOnceItHasBeenPublished()
        {
        
            var ringBuffer = workpool2.start(TaskScheduler.Default);

            ringBuffer.Next();
            ringBuffer.Next();
            ringBuffer.Publish(0);
            ringBuffer.Publish(1);
            Thread.Sleep(1000);
            Assert.AreEqual(ringBuffer[0].VL, 1L);
            Assert.AreEqual(ringBuffer[1].VL, 1L);
        }

        class VolatileLong
        {
            public int VL { get; set; }
        }

        class AtomicLongWorkHandler : IWorkHandler<VolatileLong>
        {
            #region IWorkHandler<VolatileLong> 成员

            public void OnEvent(VolatileLong @event)
            {
                @event.VL += 1;
            }

            #endregion
        }
    }
}
