using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace disruptorpretest.Support.V3._3._0
{
    public class FizzBuzzQueueProcessor
    {
        private readonly FizzBuzzStep _fizzBuzzStep;

        private readonly ConcurrentQueue<long> _fizzInputQueue;
        private readonly ConcurrentQueue<long> _buzzInputQueue;
        private readonly ConcurrentQueue<bool> _fizzOutputQueue;
        private readonly ConcurrentQueue<bool> _buzzOutputQueue;
        private long _iterations;
        private volatile bool _running;
        private long fizzBuzzCounter = 0;
        private long _sequence;
        private CountdownEvent _latch = null;
        public FizzBuzzQueueProcessor(FizzBuzzStep fizzBuzzStep,
                                 ConcurrentQueue<long> fizzInputQueue,
                                 ConcurrentQueue<long> buzzInputQueue,
                                 ConcurrentQueue<bool> fizzOutputQueue,
                                 ConcurrentQueue<bool> buzzOutputQueue,
                                 long iterations)
        {
            _fizzBuzzStep = fizzBuzzStep;

            _fizzInputQueue = fizzInputQueue;
            _buzzInputQueue = buzzInputQueue;
            _fizzOutputQueue = fizzOutputQueue;
            _buzzOutputQueue = buzzOutputQueue;
            _iterations = iterations;

           // this._latch=new CountdownEvent (1);

        }


        public long FizzBuzzCounter
        {
            get { return _iterations; }
        }

        public void Reset(CountdownEvent latch)
        {
            _iterations = 0L;
            _sequence = 0L;
            this._latch = latch;
        }

        public void Halt()
        {
            _running = false;
        }

        public void Run()
        {
            _running = true;

            while (true)
            {
                try
                {
                    switch (_fizzBuzzStep)
                    {
                        case FizzBuzzStep.Fizz:
                            {
                                long v;
                                var value = _fizzInputQueue.TryDequeue(out v);
                                _fizzOutputQueue.Enqueue((v % 3) == 0);
                                break;
                            }

                        case FizzBuzzStep.Buzz:
                            {
                                long v;
                                var value = _buzzInputQueue.TryDequeue(out v);
                                _buzzOutputQueue.Enqueue((v % 5) == 0);
                                break;
                            }

                        case FizzBuzzStep.FizzBuzz:
                            {
                                bool v, v2;
                                var fizz = _fizzOutputQueue.TryDequeue(out v);
                                var buzz = _buzzOutputQueue.TryDequeue(out v2);
                                if (v && v2)
                                {
                                    ++_iterations;
                                }
                                break;
                            }
                    }
                    if (null != _latch && _sequence++ == _iterations)
                    {
                        _latch.Signal();
                    }

                }
                catch (Exception)
                {
                    break;
                }
            }
        }
    }
}
