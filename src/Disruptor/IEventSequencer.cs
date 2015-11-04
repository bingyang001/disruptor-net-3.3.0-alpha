namespace Disruptor
{
    public interface IEventSequencer<T> : IDataProvider<T>,ISequenced
    {
    }
}
