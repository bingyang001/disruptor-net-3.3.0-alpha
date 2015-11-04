namespace Disruptor.Dsl
{
    public enum WaitStrategyType : byte
    {
        Blocking,
        BusySpin,
        PhasedBackoffwithSleep,
        PhasedBackoffwithLock,
        Sleeping,
        TimeoutBlocking,
        Yielding
    }
}
