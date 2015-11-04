//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Disruptor;
//using Disruptor.Dsl;
//using RabbitMQ.Client;

//namespace disruptor.rabbitmq
//{
//    class Program
//    {
//        static void Main(string[] args)
//        {
//            var _disruptor = new Disruptor<RabbitmqMessage>(() => new RabbitmqMessage(), 1024 * 64, TaskScheduler.Default, ProducerType.SINGLE, new BusySpinWaitStrategy());
//            _disruptor.HandleEventsWith(new REventHandler());
//            var connFactory = new ConnectionFactory();
//            connFactory.HostName = "172.16.100.105";
//            connFactory.Port = 5672;
//            var watch = Stopwatch.StartNew();
//            var count = 100000000;
//            using (var conn = connFactory.CreateConnection())
//            using (var channel = conn.CreateModel())
//            {
//                channel.ExchangeDeclare("gaoxu", "topic", true);
//                var _ringBuffer = _disruptor.Start();
//                Console.WriteLine("disruptor 已启动，开始发送消息");
//                for (var i = 0; i < count; i++)
//                {
//                    var sequence = _ringBuffer.Next();
//                    var evt = _ringBuffer[sequence];
//                    evt.M = "hell word " + i;
//                    evt.Channel = channel ;
//                    _ringBuffer.Publish(sequence);
//                }
//            }
//            watch.Stop();
//            var total = watch.ElapsedMilliseconds;
//            Console.WriteLine("耗时 {0},ops {1}", total, (count * 1000L) / total);
//            Console.Read();

//        }
//    }
//    public class REventHandler : IEventHandler<RabbitmqMessage>
//    {

//        #region IEventHandler<RabbitmqMessage> 成员

//        public void OnEvent(RabbitmqMessage @event, long sequence, bool endOfBatch)
//        {
//            //@event.Channel.ExchangeDeclare("gaoxu", "topic", true);
//            @event.Channel.BasicPublish("gaoxu", "#.#", null, System.Text.Encoding.GetEncoding("utf-8").GetBytes(@event.M));
//        }

//        #endregion
//    }
//    public class RabbitmqMessage
//    {
//        public string M { get; set; }
//        public IModel Channel { get; set; }
//    }
//}
