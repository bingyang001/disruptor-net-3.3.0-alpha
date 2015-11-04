using System;
namespace Disruptor
{
    /// <summary>
    /// Set of common functions used by the Disruptor
    /// </summary>
    public static class Util
    {
        /// <summary>
        /// Calculate the next power of 2, greater than or equal to x.
        /// </summary>
        /// <param name="x">Value to round up</param>
        /// <returns>The next power of 2 from x inclusive</returns>
        public static int CeilingNextPowerOfTwo(this int x)
        {
            var result = 2;

            while (result < x)
            {
                result <<= 1;
            }

            return result;
        }

        /// <summary>
        /// Test whether a given integer is a power of 2 
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static bool IsPowerOf2(this int x)
        {
            return x > 0 && (x & (x - 1)) == 0;
        }


        /// <summary>
        /// Get the minimum sequence from an array of <see cref="Sequence"/>s.
        /// </summary>
        /// <param name="sequences">sequences to compare.</param>
        /// <returns>the minimum sequence found or lon.MaxValue if the array is empty.</returns>
        public static long GetMinimumSequence(Sequence[] sequences)
        {
            if (sequences.Length == 0) return long.MaxValue;

            var min = long.MaxValue;
            for (var i = 0; i < sequences.Length; i++)
            {
                var sequence = sequences[i].Value; // volatile read
                min = min < sequence ? min : sequence;
            }
            return min;
        }

        /// <summary>
        /// Get the minimum sequence from an array of <see cref="Sequence"/>s.
        /// </summary>
        /// <param name="sequences"> to compare.</param>
        /// <param name="minimum">an initial default minimum.  If the array is empty this value will be</param>
        /// <returns>the minimum sequence found or Long.MAX_VALUE if the array is empty.</returns>
        public static long GetMinimumSequence(Sequence[] sequences, long minimum)
        {
            for (int i = 0, n = sequences.Length; i < n; i++)
            {
                long value = sequences[i].Value;
                minimum = Math.Min(minimum, value);
            }
         
            return minimum;
        }

        /// <summary>
        /// Get an array of <see cref="Sequence"/>s for the passed <see cref="IEventProcessor"/>s
        /// </summary>
        /// <param name="processors">processors for which to get the sequences</param>
        /// <returns>the array of <see cref="Sequence"/>s</returns>
        public static Sequence[] GetSequencesFor(params IEventProcessor[] processors)
        {
            var sequences = new Sequence[processors.Length];
            for (int i = 0; i < sequences.Length; i++)
            {
                sequences[i] = processors[i].Sequence;
            }

            return sequences;
        }

        /// <summary>
        /// Calculate the log base 2 of the supplied integer, essentially reports the location of the highest bit.
        /// </summary>
        /// <param name="i">Value to calculate log2 for.</param>
        /// <returns>he log2 value</returns>
        public static int Log2(int i)
        {
            ////Math.Log(bufferSize, 2);
            int r = 0;
            while ((i >>= 1) != 0)
            {
                ++r;
            }
            return r;
        }
        public static T[] CopyToNewArray<T>(T[] original, int newLength) 
        {
            T[] newArray   = typeof (T)==typeof(object)?new object[newLength] as T[]
                            :Array.CreateInstance(typeof(T),newLength) as T[];

            var copyLen=Math.Min (original.Length ,newLength);

            Array.Copy(original,0,newArray,0,copyLen);
            return newArray ;
        }
    }
}