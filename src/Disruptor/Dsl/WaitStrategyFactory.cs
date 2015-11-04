using System;
using System.Collections.Generic;

namespace Disruptor.Dsl
{
    public class WaitStrategyFactory
    {
        private readonly Dictionary<WaitStrategyType, IWaitStrategy> waitrStrategyRepository = new Dictionary<WaitStrategyType, IWaitStrategy>();
        private static readonly Lazy<WaitStrategyFactory> lazy = new Lazy<WaitStrategyFactory>(() => new WaitStrategyFactory());     
        
        private WaitStrategyFactory() 
        {
            waitrStrategyRepository[WaitStrategyType.Yielding] = new YieldingWaitStrategy();
            waitrStrategyRepository[WaitStrategyType.Blocking] = new BlockingWaitStrategy();
            waitrStrategyRepository[WaitStrategyType.TimeoutBlocking] = new TimeoutBlockingWaitStrategy();
            waitrStrategyRepository[WaitStrategyType.BusySpin] = new BusySpinWaitStrategy();
            //waitrStrategyRepository[WaitStrategyType.PhasedBackoffwithLock] = PhasedBackoffWaitStrategy.WithLock(
            waitrStrategyRepository[WaitStrategyType.Sleeping] = new SleepingWaitStrategy();
        }

        public static WaitStrategyFactory Instance { get { return lazy.Value; } }

        public IWaitStrategy BuliderWaitStrategy(WaitStrategyType waitStrategyType)
        {
            return waitrStrategyRepository[waitStrategyType];
        }
    }
}
