using Disruptor;

namespace disruptorpretest.Support
{
    public class ValueAdditionWorkHandler_V3 : IWorkHandler<ValueEvent>
    {
        private long total;
        #region IWorkHandler<ValueEvent> 成员

        public void OnEvent(ValueEvent @event)
        {
            total += @event.Value; 
        }

        #endregion

        public long Total { get { return total; } }
    }
}
