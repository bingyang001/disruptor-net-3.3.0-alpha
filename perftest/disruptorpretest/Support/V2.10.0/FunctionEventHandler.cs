using System.Threading;
using Disruptor;

namespace disruptorpretest.Support
{
    public class FunctionEventHandler : IEventHandler<FunctionEvent>
    {
        private readonly FunctionStep _functionStep;
        private _Volatile.PaddedLong _stepThreeCounter = default(_Volatile.PaddedLong);
        private readonly long _iterations;
        private readonly ManualResetEvent _mru;

        public long StepThreeCounter
        {
            get { return _stepThreeCounter.ReadUnfenced(); }
        }

        public FunctionEventHandler(FunctionStep functionStep, long iterations, ManualResetEvent mru)
        {
            _functionStep = functionStep;
            _iterations = iterations;
            _mru = mru;
        }

      

        #region IEventHandler<FunctionEvent> ≥…‘±

        public void OnEvent(FunctionEvent @event, long sequence, bool endOfBatch)
        {
            switch (_functionStep)
            {
                case FunctionStep.One:
                    @event.StepOneResult = @event.OperandOne + @event.OperandTwo;
                    break;
                case FunctionStep.Two:
                    @event.StepTwoResult = @event.StepOneResult + 3L;
                    break;

                case FunctionStep.Three:
                    if ((@event.StepTwoResult & 4L) == 4L)
                    {
                        _stepThreeCounter.WriteUnfenced(_stepThreeCounter.ReadUnfenced() + 1);
                    }
                    break;
            }

            if (sequence == _iterations - 1)
            {
                _mru.Set();
            }
        }

        #endregion
    }
}