namespace Disruptor
{
    /// <summary>
    /// Implementors of this interface must provide a single long value
    /// that represents their current cursor value.  Used during dynamic
    /// add/remove of Sequences from a
    /// {@link SequenceGroups#addSequences(Object, java.util.concurrent.atomic.AtomicReferenceFieldUpdater, Cursored, Sequence...)}.
    /// </summary>
    public interface ICursored
    {
        /// <summary>
        ///  Get the current cursor value.
        /// </summary>
        /// <returns></returns>
        long GetCursor { get; }
    }
}
