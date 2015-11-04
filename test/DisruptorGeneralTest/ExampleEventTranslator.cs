using System;
using Disruptor;


namespace DisruptorGeneralTest
{
    public class ExampleEventTranslator : IEventTranslator<StubEvent>
    {

        private readonly String testValue;

        public ExampleEventTranslator(String testValue)
        {
            this.testValue = testValue;
        }


        #region IEventTranslator<StubEvent> 成员

        public void TranslateTo(StubEvent @event, long sequence)
        {
            @event.TestString = testValue; 
        }

        #endregion
    }
}
