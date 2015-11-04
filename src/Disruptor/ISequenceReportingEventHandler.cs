namespace Disruptor
{
    /// <summary>
    ///Used by the {@link BatchEventProcessor} to set a callback allowing the {@link EventHandler} to notify
    /// when it has finished consuming an event if this happens after the {@link EventHandler#OnEvent(Object, long, boolean)} call.
    /// 
    /// Typically this would be used when the handler is performing some sort of batching operation such as writing to an IO
    /// device; after the operation has completed, the implementation should call {@link Sequence#set} to update the
    /// sequence and allow other processes that are dependent on this handler to progress. 
    /// </summary>
    public interface ISequenceReportingEventHandler<T> : IEventHandler<T>
    {
        /// <summary>
        /// Call by the {@link BatchEventProcessor} to setup the callback.
        /// </summary>
        /// <param name="sequenceCallback"></param>
        void SetSequenceCallback(Sequence sequenceCallback);
    }
}
