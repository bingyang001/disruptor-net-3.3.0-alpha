using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Disruptor;
namespace DisruptorGeneralTest
{
    public class DummySequenceBarrier : ISequenceBarrier
    {
        #region ISequenceBarrier 成员

        public long WaitFor(long sequence)
        {
            return 0L;
        }

        public long GetCursor
        {
            get { return 0L; }
        }

        public bool IsAlerted
        {
            get { return false; }
        }

        public void Alert()
        {
            throw new NotImplementedException();
        }

        public void ClearAlert()
        {
            throw new NotImplementedException();
        }

        public void CheckAlert()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
