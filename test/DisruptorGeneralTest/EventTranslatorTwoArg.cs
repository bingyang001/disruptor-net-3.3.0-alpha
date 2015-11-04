using System;
using Disruptor;

namespace DisruptorGeneralTest
{
    public class EventTranslatorTwoArg : IEventTranslatorTwoArg<StubEvent, int, string>
    {
        public void TranslateTo(StubEvent @event, long sequence, int arg0, string arg1)
        {
            @event.Value = arg0;
            @event.TestString = arg1;
        }
    }
}
