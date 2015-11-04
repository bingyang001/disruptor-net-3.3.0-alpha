using System;
namespace disruptorpretest.Support
{
    public sealed class PerfTestUtil
    {
        public static long AccumulatedAddition(long iterations)
        {
            long temp = 0L;
            for (long i = 0L; i < iterations; i++)
            {
                temp += i;
            }

            return temp;
        }

        public static void failIf(long a, long b)
        {
            if (a == b)
            {
                throw new Exception();
            }
        }

        public static void failIfNot(long a, long b)
        {
            if (a != b)
            {
            Console.WriteLine(a+"    "+b);
                throw new Exception("a!=b");
            }
        }
    }
}
