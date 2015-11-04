using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace disruptorpretest.V3._3._0.Immutable
{
    public interface IEventAccessor<T>
    {
        T take(long sequence);
    }
}
