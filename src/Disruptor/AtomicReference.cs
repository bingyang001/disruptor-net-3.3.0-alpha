using System.Threading;
using System.Runtime.CompilerServices;

namespace Disruptor
{
    [System.Obsolete("use _Volatile.Reference<T>")]
    public class AtomicReference<T> where T : class
    {
        private T _value;

        /// <summary>
        /// Create a new <see cref="Reference{T}"/> with the given initial value.
        /// </summary>
        /// <param name="value">Initial value</param>
        public AtomicReference(T value)
        {
            _value = value;
        }

        /// <summary>
        /// Read the value without applying any fence
        /// </summary>
        /// <returns>The current value</returns>
        public T ReadUnfenced()
        {
            return _value;
        }

        /// <summary>
        /// Read the value applying acquire fence semantic
        /// </summary>
        /// <returns>The current value</returns>
        public T ReadAcquireFence()
        {
            var value = _value;
            Thread.MemoryBarrier();
            return value;
        }

        /// <summary>
        /// Read the value applying full fence semantic
        /// </summary>
        /// <returns>The current value</returns>
        public T ReadFullFence()
        {
            T value = _value;
            Thread.MemoryBarrier();
            return value;
        }

        /// <summary>
        /// Read the value applying a compiler only fence, no CPU fence is applied
        /// </summary>
        /// <returns>The current value</returns>
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public T ReadCompilerOnlyFence()
        {
            return _value;
        }

        /// <summary>
        /// Write the value applying release fence semantic
        /// </summary>
        /// <param name="newValue">The new value</param>
        public void WriteReleaseFence(T newValue)
        {
            Thread.MemoryBarrier();
            _value = newValue;
        }

        /// <summary>
        /// Write the value applying full fence semantic
        /// </summary>
        /// <param name="newValue">The new value</param>
        public void WriteFullFence(T newValue)
        {
            Thread.MemoryBarrier();
            _value = newValue;
        }

        /// <summary>
        /// Write the value applying a compiler fence only, no CPU fence is applied
        /// </summary>
        /// <param name="newValue">The new value</param>

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void WriteCompilerOnlyFence(T newValue)
        {
            _value = newValue;
        }

        /// <summary>
        /// Write without applying any fence
        /// </summary>
        /// <param name="newValue">The new value</param>
        public void WriteUnfenced(T newValue)
        {
            _value = newValue;
        }

        /// <summary>
        /// Atomically set the value to the given updated value if the current value equals the comparand
        /// </summary>
        /// <param name="newValue">The new value</param>
        /// <param name="comparand">The comparand (expected value)</param>
        /// <returns></returns>
        public bool AtomicCompareExchange(T newValue, T comparand)
        {
            return Interlocked.CompareExchange(ref _value, newValue, comparand) == comparand;
        }

        /// <summary>
        /// Atomically set the value to the given updated value
        /// </summary>
        /// <param name="newValue">The new value</param>
        /// <returns>The original value</returns>
        public T AtomicExchange(T newValue)
        {
            return Interlocked.Exchange(ref _value, newValue);
        }

        /// <summary>
        /// Returns the string representation of the current value.
        /// </summary>
        /// <returns>the string representation of the current value.</returns>
        public override string ToString()
        {
            var value = ReadFullFence();
            return value.ToString();
        }
    }
}
