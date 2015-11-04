using System;
using Disruptor;
using NUnit.Framework;


namespace DisruptorGeneralTest
{
    [TestFixture]
    public class EventTranslatorTest
    {
        private static readonly String TEST_VALUE = "Wibble";

        [Test]
        public void ShouldTranslateOtherDataIntoAnEvent()
        {
            StubEvent @event = new StubEvent(-1);
            IEventTranslator<StubEvent> eventTranslator = new ExampleEventTranslator(TEST_VALUE);

            eventTranslator.TranslateTo(@event, 0);

            Assert.AreEqual(TEST_VALUE, @event.TestString);
        }
    }
}
