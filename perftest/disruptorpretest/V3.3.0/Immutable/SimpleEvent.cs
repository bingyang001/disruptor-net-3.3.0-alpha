using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace disruptorpretest.V3._3._0.Immutable
{
    public class SimpleEvent
    {
        private readonly long id;
        private readonly long v1;
        private readonly long v2;
        private readonly long v3;

        public SimpleEvent(long id, long v1, long v2, long v3)
        {
            this.id = id;
            this.v1 = v1;
            this.v2 = v2;
            this.v3 = v3;
        }

        public long getCounter()
        {
            return v1;
        }


        public String toString()
        {
            return "SimpleEvent [id=" + id + ", v1=" + v1 + ", v2=" + v2 + ", v3=" + v3 + "]";
        }
    }
}
