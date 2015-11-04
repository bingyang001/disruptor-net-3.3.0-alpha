using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Disruptor.ConsoleApp.Test
{
    public class FalseSharing
    {
        [StructLayout(LayoutKind.Explicit)]
        public class Data
        {
            [FieldOffset(0)]
            public Int32 field1;
            [FieldOffset(64)]
            public Int32 field2;
        }
     
        //迭代次数
        private int iterations = 100000000;
        private static Int32 s_operations = 2;
        private static Int64 s_startTIme;

        public FalseSharing()
        {
            var p = Process.GetProcesses();
        }

        public void DoWork()
        {
            var data = new Data();
            s_startTIme = Stopwatch.GetTimestamp();
            ThreadPool.QueueUserWorkItem(o => AccessData(data, 0));
            ThreadPool.QueueUserWorkItem(o => AccessData(data, 1));

            Console.Read();
        }

        private void AccessData(Data data, Int32 field)
        {
            for (var x = 0; x < iterations; x++)
            {
                if (field == 0) data.field1++;
                else data.field2++;
            }
            if (Interlocked.Decrement(ref s_operations) == 0)
            {
                Console.WriteLine("未解决伪共享 {0:N0}", (Stopwatch.GetTimestamp() - s_startTIme)
                    / (Stopwatch.Frequency / 1000));
            }
        }       
    }
}
