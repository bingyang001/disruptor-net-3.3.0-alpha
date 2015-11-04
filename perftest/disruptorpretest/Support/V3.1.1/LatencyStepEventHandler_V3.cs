using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;

using Disruptor;
using Disruptor.Collections;


namespace disruptorpretest.Support
{
    public class LatencyStepEventHandler_V3 : IEventHandler<ValueEvent>
    {
        private readonly FunctionStep _functionStep;
        private readonly Histogram _histogram;
        private readonly long _nanoTimeCost;
        private readonly long _ticksToNanos;
        private long _count;
        private ManualResetEvent _mru;

        public LatencyStepEventHandler_V3(FunctionStep functionStep, Histogram histogram, long nanoTimeCost)
        {
            _functionStep = functionStep;
            _histogram = histogram;
            _nanoTimeCost = nanoTimeCost;
        }

        public void Reset(ManualResetEvent latch, long expectedCount)
        {
            this._mru = latch;
            this._count = expectedCount;
        }


        #region IEventHandler<ValueEvent> 成员

        public void OnEvent(ValueEvent @event, long sequence, bool endOfBatch)
        {
            switch (_functionStep)
            {
                case FunctionStep.One:
                case FunctionStep.Two:
                    break;
                case FunctionStep.Three:
                    var duration = (Stopwatch.GetTimestamp() - @event.Value) * (_ticksToNanos == 0L ? 1 : _ticksToNanos);
                    duration /= 3;
                    duration -= _nanoTimeCost;
                    _histogram.AddObservation(duration);
                    break;
            }
            if (sequence == _count && _mru != null)
            {
                _mru.Set();
            }
        }

        #endregion
    }
}
