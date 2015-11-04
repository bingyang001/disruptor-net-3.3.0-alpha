using System;
using Disruptor;
using NUnit.Framework;

namespace DisruptorGeneralTest
{
    [TestFixture]
    public class IgnoreExceptionHandlerTest
    {
        [Test]
        public void ShouldHandleAndIgnoreException()
        {
            var ex = new Exception();
            var @event = new TestEvent();

            var exceptionHandler = new IgnoreExceptionHandler();
            exceptionHandler.HandleEventException(ex, 0L, @event);
        }
    }
}
