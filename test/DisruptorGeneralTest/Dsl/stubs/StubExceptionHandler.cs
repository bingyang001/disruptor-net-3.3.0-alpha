using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Disruptor;

namespace DisruptorGeneralTest.Dsl.stubs
{
    public class StubExceptionHandler : IExceptionHandler
    {
        #region IExceptionHandler 成员

        public void HandleEventException(Exception ex, long sequence, object @event)
        {
            Console.WriteLine(string.Format("ex={0},sequence={1},@event={2}"), ex.ToString(), sequence, @event.ToString());
        }

        public void HandleOnStartException(Exception ex)
        {
            Console.WriteLine(string.Format("ex={0}",ex.ToString()));
        }

        public void HandleOnShutdownException(Exception ex)
        {
            Console.WriteLine(string.Format("ex={0}", ex.ToString()));
        }

        #endregion
    }
}
