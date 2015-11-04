using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using disruptorpretest.V3._3._0.Sequenced;

namespace disruptorpretest
{
    class Program
    {
        static void Main(string[] args)
        {
            new ThreeToOneSequencedBatchThroughputTest().TestImplementations();
            Console.Read();
        }
    }
}
