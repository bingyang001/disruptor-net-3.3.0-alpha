using System.Threading;
using Disruptor;

namespace disruptorpretest.Support
{
    public class FunctionEventHandler_v3 : IEventHandler<FunctionEvent>
    {
        private FunctionStep _functionStep;
        private _Volatile.PaddedLong _stepThreeCounter = default(_Volatile.PaddedLong);
        private long _iterations;
        private CountdownEvent latch;

        public long StepThreeCounter
        {
            get { return _stepThreeCounter.ReadUnfenced(); }
        }

        public FunctionEventHandler_v3(FunctionStep functionStep)
        {
            _functionStep = functionStep;
        }

        public void reset(CountdownEvent latch, long expectedCount)
        {
            _stepThreeCounter.WriteCompilerOnlyFence(0L);
            this.latch = latch;
            _iterations = expectedCount;
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

            if (latch != null && sequence == _iterations)
            {
                latch.Signal();
            }
        }

        #endregion
    }
}