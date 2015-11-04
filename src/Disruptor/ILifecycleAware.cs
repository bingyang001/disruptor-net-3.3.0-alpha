namespace Disruptor
{
    /// <summary>
    /// 
    /// Implement this interface in your {@link EventHandler} to be notified when a thread for the
    /// {@link BatchEventProcessor} starts and shuts down.
    /// </summary>
    public interface ILifecycleAware
    {
        /// <summary>
        /// Called once on thread start before first event is available.
        /// </summary>
        void OnStart();

        /// <summary>
        /// <p>Called once just before the thread is shutdown.</p>
        /// Sequence event processing will already have stopped before this method is called. No events will
        /// be processed after this message.
        /// </summary>
        void OnShutdown();
    }
}
