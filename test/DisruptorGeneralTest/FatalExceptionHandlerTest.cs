using System;

using NUnit.Framework;
using Disruptor;

namespace DisruptorGeneralTest
{
    [TestFixture]
    public class FatalExceptionHandlerTest
    {
        [Test]
        public void ShouldHandleFatalException()
        {
            var causeException = new Exception();
            var evt = new StubEvent(0);

            var exceptionHandler = new FatalExceptionHandler();

            try
            {
                exceptionHandler.HandleEventException(causeException, 0L, evt);
            }
            catch (Exception ex)
            {
                Assert.AreEqual(causeException, ex.InnerException);
            }
        }
    }
}
