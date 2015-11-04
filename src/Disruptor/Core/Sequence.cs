using System.Threading;

namespace Disruptor
{
    /// <summary>
    /// <p>Concurrent sequence class used for tracking the progress of
    /// the ring buffer and event processors.  Support a number
    /// of concurrent operations including CAS and order writes.
    /// </p>
    /// <p>Also attempts to be more efficient with regards to false
    /// sharing by adding padding around the volatile field.
    /// </p>
    /// <c>
    /// <remarks>  2015.01.16
    /// Sequence是Disruptor最核心的组件。生产者对RingBuffer的互斥访问，
    /// 生产者与消费者之间的协调以及消费者之间的协调，都是通过Sequence实现。
    /// 几乎每一个重要的组件都包含Sequence。那么Sequence是什么呢？
    /// 首先Sequence是一个递增的序号，说白了就是计数器；
    /// 其次，由于需要在线程间共享，所以Sequence是引用传递，并且是线程安全的；
    /// 再次，Sequence支持CAS操作；最后，为了提高效率，Sequence通过padding来避免伪共享
    /// </remarks></c>
    /// </summary>
    public class Sequence
    {       
        private _Volatile.PaddedLong _value = new _Volatile.PaddedLong(InitialCursorValue.INITIAL_CURSOR_VALUE);

        /// <summary>
        /// Default Constructor that uses an initial value of <see cref="ISequencer.InitialCursorValue"/>
        /// </summary>
        public Sequence()
        {
        }

        /// <summary>
        /// Construct a new sequence counter that can be tracked across threads.
        /// </summary>
        /// <param name="initialValue">initial value for the counter</param>
        public Sequence(long initialValue)
        {
            _value.WriteCompilerOnlyFence(initialValue);
        }

        /// <summary>
        /// get/set Current sequence number
        /// 注：读取，写入值都将使用内存栅栏
        /// </summary>
        public virtual long Value
        {
            get { return _value.ReadFullFence(); }
            set { _value.WriteFullFence(value); }
        }

        /// <summary>
        /// Eventually sets to the given value. (代码不会被优化)
        /// </summary>
        /// <param name="value">the new value</param>
        /// <remarks>代码不会被优化</remarks>
        public virtual void LazySet(long value)
        {
            _value.WriteCompilerOnlyFence(value);
        }

        /// <summary>
        /// Atomically set the value to the given updated value if the current value == the expected value.
        /// </summary>
        /// <param name="expectedSequence">the expected value for the sequence</param>
        /// <param name="newSequence">the new value for the sequence</param>
        /// <returns>true if successful. False return indicates that the actual value was not equal to the expected value.</returns>
        public virtual bool CompareAndSet(long expectedSequence, long newSequence)
        {
            return _value.AtomicCompareExchange(newSequence, expectedSequence);
        }

        /// <summary>
        /// Value of the <see cref="Sequence"/> as a String.
        /// </summary>
        /// <returns>String representation of the sequence.</returns>
        public override string ToString()
        {
            return _value.ToString();
        }

        ///<summary>
        /// Increments the sequence and stores the result, as an atomic operation.
        ///</summary>
        ///<returns>incremented sequence</returns>
        public virtual long IncrementAndGet()
        {
            return AddAndGet(1);
        }

        ///<summary>
        /// Increments the sequence and stores the result, as an atomic operation.
        ///</summary>
        ///<returns>incremented sequence</returns>
        public virtual long AddAndGet(long value)
        {
            long currentValue;
            long newValue;

            do
            {
                currentValue = Value;
                newValue = currentValue + value;
            }
            while (!CompareAndSet(currentValue, newValue));

            return newValue;
           // return _value.AtomicAddAndGet(value);
        }
    }
}
