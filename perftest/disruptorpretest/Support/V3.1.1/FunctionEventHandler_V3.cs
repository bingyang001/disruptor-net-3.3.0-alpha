using Disruptor;
using System.Threading;

namespace disruptorpretest.Support
{
    public class FunctionEventHandler_V3 : IEventHandler<FunctionEvent>
    {
        private FunctionStep functionStep;
        private _Volatile.PaddedLong stepThreeCounter = default(_Volatile.PaddedLong);
        private long count;
        private ManualResetEvent latch;

        public FunctionEventHandler_V3(FunctionStep functionStep)
        {
            this.functionStep = functionStep;
        }

        public long GetStepThreeCounter
        {
            get { return stepThreeCounter.ReadUnfenced(); }
        }

        public void Reset(ManualResetEvent latch, long expectedCount)
        {
            this.stepThreeCounter.WriteUnfenced(0L);
            this.latch = latch;
            this.count = expectedCount;
        }

        #region IEventHandler<FunctionEvent> 成员

        public void OnEvent(FunctionEvent @event, long sequence, bool endOfBatch)
        {
            //switch (functionStep)
            //{
            //    case FunctionStep.One:
            //        @event.StepOneResult = @event.OperandOne + @event.OperandTwo;
            //        break;

            //    case FunctionStep.Two:
            //        @event.StepTwoResult = @event.StepOneResult + 3L;
            //        break;

            //    case FunctionStep.Three:
            //        if ((@event.StepTwoResult & 4L) == 4L)
            //        {
            //            stepThreeCounter.WriteUnfenced(stepThreeCounter.ReadUnfenced() + 1L);
            //        }
            //        break;
            //}

            //if (latch != null && count == sequence)
            //{
            //    latch.Set();
            //}

            switch (functionStep)
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
                        stepThreeCounter.WriteUnfenced(stepThreeCounter.ReadUnfenced() + 1);
                    }
                    break;
            }

            if (latch != null && sequence == count)
            {
                latch.Set();
            }
        }

        #endregion
    }
}
