# 说明：项目Disruptor是从disruptor java 3.3.0版本移植到.net 平台
# 系统要求：.net framework 4.5 vs 2013  
# java 版本地址：http://lmax-exchange.github.io/disruptor/

# disruptor-net-3.3.0-alpha 说明：
# 1.这个版本适合学习并行计算使用，不宜放生产环境
# 2.Atomic 这个项目来源网上 Disruptor.NET 2.10.0 项目
# 3.Disruptor.Scheduler 调度程序微软并行库扩展项目
# 4.disruptor.rabbitmq 是使用disruptor net 库调用rabbitmq测试程序，有bug，跑了几次rabbitmq.NET都异常，具体原因没研究
# 5.tplpretest 是测试dataflow tpl程序
# 6.disruptor.generaltest， disruptor.pretest 是其常规测试和性能测试

 Disruptor .net WaitStrategy：   
 /*1.WaitStrategy等待策略：              
  *          BlockingWaitStrategy  使用锁和条件变量等待屏障，这个策略适合性能，低延迟不重要，重要的是CPu资源（牺牲一定的性能，延迟换取CPU资源）
  *          BusySpinWaitStrategy  一个忙碌自旋等待一个屏障策略，可以避免系统调度引起的不稳定性，当线程绑定到特定的CPU上这是最好策略，但这将牺牲cpu资源
 **          YieldingWaitStrategy  这个策略在CPU资源和低延迟间做好很的平衡作用，不会产生显著的高延迟
  *			 SleepingWaitStrategy  这个策略基于Thread.Sleep 开始自旋，其内部使用Thread.Sleep(0)，挂起当前线程但其他线程可用继续执行 ，
  *								   这个策略是CPU资源和性能之间是个很好的则中 ，但可能出现高峰延迟现象
  *			 TimeoutBlockingWaitStrategy 在BlockingWaitStrategy测试功能上增加了超时时间，在超时时间内没有获取到锁，抛出异常
  *			 PhasedBackoffWaitStrategy 阶段性等待事件处理器完成的屏障，此策略适合吞吐量和低延迟的并不重要，重要的是CPU资源，其在自旋后再应用BlockingWaitStrategy策略
  *2.ringBuffer:使用策略         
  *          MultiThreadedLowContentionClaimStrategy   多个发布者同时使用ringBuffer，需要足够的CPU核数
  *          SingleThreadedClaimStrategy               单个发布者,此策略不适合多个线程发布者同时使用ringBuffer，只需要1个cas操作
  *          MultiThreadedClaimStrategy                适合多个发布者使用ringBuffer,要求有足够的cpu处理多个发布者线程，需要执行2个cas（比较交换）
  *
  *
  *	3.AbstractSequencer  序列基类提供通用的add,remove序列的功能
  * 4.MultiProducerSequencer  多生产者，适合多个线程生产者
  * 5.SingleProducerSequencer 单生产者，多线程下不是安全的，因为他执行任何屏障等待
  *
  *
  * 6.ProcessingSequenceBarrier 处理序列屏障,  
  *
  *
  * 7.Sequence ringBuffer核心单元，这个是个可以并发操作的序列类，拥有跟踪ringBuffer对事件的处理进度，解决伪共享，提供一系列原子操作
  * 8.SequenceGroup ，SequenceGroupManaging SequenceGroupManaging封装了对SequenceGroup的内部管理（增加序列，删除序列操作）
  * 9.NoOpEventProcessor 仅此用于跟踪事件处理器，在测试和发布者预填充ringBuffer场景下非常有用
  * 10.WorkProcessor 实现有效的消费序列中的事件，和确保有效的屏障
  *
  * 11.RingBuffer Disruptor显示的核心类，可重复使用的包含数据的环形缓冲
  * 12.BatchEventProcessor 批量事件处理
  */
//修改日志
3.3.0 还有些性能测试没有完成，有兴趣的童鞋，拉源码去翻译吧。
   
