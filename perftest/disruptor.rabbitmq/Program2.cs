using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Disruptor;
using Disruptor.Dsl;
using Disruptor.Scheduler;
using RabbitMQ.Client;

namespace disruptor.rabbitmq
{
    class Program2
    {
        static void Main(string[] args)
        {
        try
        {
            RingBuffer<RabbitmqMessage2> _ringBuffer = RingBuffer<RabbitmqMessage2>.CreateSingleProducer(() => new RabbitmqMessage2(), 1024 * 64, new YieldingWaitStrategy());
            var sequenceBarrier = _ringBuffer.NewBarrier();
            var batchEventProcessor = new BatchEventProcessor<RabbitmqMessage2>(_ringBuffer, sequenceBarrier, new REventHandler());
            _ringBuffer.AddGatingSequences(batchEventProcessor.Sequence);
            var connFactory = new ConnectionFactory();
            connFactory.HostName="172.16.100.105";
            connFactory.Port =5672;
            var watch=Stopwatch.StartNew ();
            var count=  500000;
            using (var conn = connFactory.CreateConnection())
            using (var channel = conn.CreateModel())
            {
               //var _ringBuffer = _disruptor.Start () ;
                channel.ExchangeDeclare("gaoxu", "topic", true);
               Console.WriteLine("disruptor 已启动，开始发送消息");
               Task.Factory.StartNew(batchEventProcessor.Run, CancellationToken.None, TaskCreationOptions.None, new RoundRobinThreadAffinedTaskScheduler(4));
               for (var i = 0; i < count; i++) 
               {
                   var sequence = _ringBuffer.Next();
                   var evt = _ringBuffer[sequence];
                   evt.M ="hell word "+i;
                   evt.Channel=channel ;
                   _ringBuffer.Publish(sequence);
               }
            }
            watch.Stop ();
            var total=watch.ElapsedMilliseconds;
            Console.WriteLine("耗时 {0},ops {1}", total, (count * 1000L) / total);          
            Console.Read ();
        }
        catch (Exception ex) 
        {
            Console.WriteLine (ex.ToString ());
         }
         Console.Read ();
        }
    }
    public class REventHandler : IEventHandler<RabbitmqMessage2> 
    {

        #region IEventHandler<RabbitmqMessage> 成员

        public void OnEvent(RabbitmqMessage2 @event, long sequence, bool endOfBatch)
        {
            //@event.Channel.ExchangeDeclare("gaoxu","topic",true);
            @event.Channel.BasicPublish("gaoxu", "#.#", null, System.Text.Encoding.GetEncoding("utf-8").GetBytes(@event.M));
        }

        #endregion
    }
    public class RabbitmqMessage2
    {
        public string M { get; set; }
        public IModel Channel { get; set; }
    }
}
