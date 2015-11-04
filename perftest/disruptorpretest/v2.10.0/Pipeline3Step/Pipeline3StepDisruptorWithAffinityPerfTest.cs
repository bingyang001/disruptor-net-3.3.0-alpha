using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Disruptor.Dsl;
using disruptorpretest.Support;
using Disruptor.Scheduler;
using NUnit.Framework;
using Disruptor;

namespace disruptorpretest.Pipeline3Step
{
    [TestFixture]
    public class Pipeline3StepDisruptorWithAffinityPerfTest : AbstractPipeline3StepPerfTest
    {
        private readonly RingBuffer<FunctionEvent> _ringBuffer;
        private readonly FunctionEventHandler _stepThreeFunctionEventHandler;
        private readonly Disruptor<FunctionEvent> _disruptor;
        private readonly ManualResetEvent _mru;
        private readonly RoundRobinThreadAffinedTaskScheduler _scheduler;

        public Pipeline3StepDisruptorWithAffinityPerfTest()
            : base(100 * Million)//1千万  1亿
        {
            _scheduler = new RoundRobinThreadAffinedTaskScheduler(4);
            _disruptor = new Disruptor<FunctionEvent>(() => new FunctionEvent(),Size,_scheduler, ProducerType.SINGLE,new YieldingWaitStrategy());                                                    

            _mru = new ManualResetEvent(false);
            _stepThreeFunctionEventHandler = new FunctionEventHandler(FunctionStep.Three, Iterations, _mru);

            _disruptor.HandleEventsWith(new FunctionEventHandler(FunctionStep.One, Iterations, _mru))
                .Then(new FunctionEventHandler(FunctionStep.Two, Iterations, _mru))
                .Then(_stepThreeFunctionEventHandler);

            _ringBuffer = _disruptor.GetRingBuffer;
        }

        public override long RunPass()
        {
            _disruptor.Start();

            var sw = Stopwatch.StartNew();

            Task.Factory.StartNew(
                ()=>
                    {
                        var operandTwo = OperandTwoInitialValue;
                        for (long i = 0; i < Iterations; i++)
                        {
                            var sequence = _ringBuffer.Next();
                            var evt = _ringBuffer[sequence];
                            evt.OperandOne = i;
                            evt.OperandTwo = operandTwo--;
                            _ringBuffer.Publish(sequence);
                        }
                    }, CancellationToken.None, TaskCreationOptions.None, _scheduler);

            _mru.WaitOne();

            var opsPerSecond = (Iterations * 1000L) / sw.ElapsedMilliseconds;

            _disruptor.Shutdown();
            _scheduler.Dispose();

            Assert.AreEqual(ExpectedResult, _stepThreeFunctionEventHandler.StepThreeCounter);

            return opsPerSecond;
        }

        [Test]
        public override void RunPerformanceTest()
        {
            RunAsUnitTest();
        }
    }
}