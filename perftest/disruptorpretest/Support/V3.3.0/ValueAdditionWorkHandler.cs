using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Disruptor;

namespace disruptorpretest.Support.V3._3._0
{
   public class ValueAdditionWorkHandler : IWorkHandler<ValueEvent>
    {
        private long total;


        public void OnEvent(ValueEvent @event)
        {
            long value = @event.Value;
            total += value;
        }

        public long getTotal()
        {
            return total;
        }

       
    }
}
