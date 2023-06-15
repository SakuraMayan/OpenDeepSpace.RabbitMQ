using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using OpenDeepSpace.RabbitMQ.Enums;
using OpenDeepSpace.RabbitMQ.Extensions;
using OpenDeepSpace.RabbitMQ.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpenDeepSpace.RabbitMQ
{
    /// <summary>
    /// MQ延迟死信消费者
    /// 
    /// 生产者消息发送到延时队列 
    /// 消费者消费死信队列
    /// 
    /// 对于消息过期时间不同的TTL设置为-1
    /// 用于队列中每个消息的过期时间不一样的情况
    /// 可通过生产者发布消息的时候设置消息过期时间
    /// 但如果消息过期时间 
    /// 出现 7 5 3 那么后面5会被7阻塞 需要解决？
    /// 5时间过期了 但7还没过期 7又排在5前面7就会阻塞5先进入死信队列
    /// 
    /// 解决这种情况可以为延时队列增加一个消费者 不ack消费 轮询回队列 
    /// 当时间到其后按照错开时间的时间差并且又轮询到对头即可按时进入死信队列
    /// 
    /// 
    /// </summary>
    public abstract class RabbitMQDelayDeadLetterConsumer : IRabbitMQConsumer,IDisposable
    {
        public IModel _channel { get; }
        /// <summary>
        /// 连接工厂
        /// </summary>
        private ConnectionFactory ConnectionFactory;

        private readonly RabbitMQOptions rabbitMQOption;

        private readonly IConnection connection;

        private readonly IMQConsumerHandle mQConsumerHandle;

        /// <summary>
        /// 路由键
        /// </summary>
        public string RoutingKey { get; set; }
        /// <summary>
        /// 队列名称
        /// </summary>
        public string QueueName { get; set; }
        /// <summary>
        /// 交换机名称
        /// </summary>
        public string ExchangeName { get; set; }

        /// <summary>
        /// 管道中消息的大小 默认为0不限制
        /// 目前不支持
        /// </summary>
        public uint QosPrefetchSize { get; } = 0;

        /// <summary>
        /// 管道一次性向消费者推送的消息数量不大于该数字
        /// 如果有这么多没被消费 消费者将不会接收到新的消息
        /// </summary>
        public ushort QosPrefetchCount { get; set; } = 1;

        /// <summary>
        /// 是否将对整个管道设置上面Qos参数
        /// 设置为true整个管道有效 设置为false只针对当前消费者
        /// </summary>
        public bool QosGlobal { get; set; } = true;

        /// <summary>
        /// 交换机类型
        /// </summary>
        public Enums.ExchangeType ExchangeType { get; set; }

        /// <summary>
        /// 死信交换机
        /// </summary>
        public string XDeadLetterExchange { get; set; }

        /// <summary>
        /// 死信交换机类型
        /// </summary>
        public Enums.ExchangeType XDeadLetterExchangeType { get; set; }

        /// <summary>
        /// 死信路由键
        /// </summary>
        public string XDeadLetterRoutingKey { get; set; }

        /// <summary>
        /// 死信队列
        /// </summary>
        public string XDeadLetterQueue { get; set; }

        /// <summary>
        /// 队列消息存活时间 默认为-1 表示该队列不设置消息过期时间 单位毫秒
        /// 而是在消息上设置过期时间
        /// 消息过期时间是从消息进入延迟队列开始计算
        /// </summary>
        public long XMessageTTL { get; set; } = -1;

        /// <summary>
        /// 延迟队列参数
        /// </summary>
        private readonly Dictionary<string, object> delayQueueArgs = new Dictionary<string, object>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        /// <param name="mQConsumerHandle"></param>
        /// <exception cref="Exception"></exception>
        public RabbitMQDelayDeadLetterConsumer(IOptions<RabbitMQOptions> options, IMQConsumerHandle mQConsumerHandle)
        {
            rabbitMQOption = options.Value;


            try
            {
                //rabbitmq多集群连接
                ConnectionFactory = new ConnectionFactory()
                {
                    UserName = rabbitMQOption.UserName,
                    Password = rabbitMQOption.Password,
                    VirtualHost = rabbitMQOption.VirtualHost,

                };

                if (rabbitMQOption.AmqpConnections == null || (rabbitMQOption.AmqpConnections != null && !rabbitMQOption.AmqpConnections.Any()))
                {
                    throw new Exception("请至少设置一个mq连接");
                }
                List<AmqpTcpEndpoint> amqpTcpEndpoints = new List<AmqpTcpEndpoint>();
                foreach (var item in rabbitMQOption.AmqpConnections!)
                {
                    amqpTcpEndpoints.Add(new AmqpTcpEndpoint()
                    {
                        HostName = item.HostName,
                        Port = item.Port,

                    });
                }

                var connection = ConnectionFactory.CreateConnection(amqpTcpEndpoints);
                _channel = connection.CreateModel();
            }
            catch (Exception e)
            {
                throw new Exception("消费端连接MQ失败:" + e.Message);
            }

            this.mQConsumerHandle = mQConsumerHandle;
        }

        /// <summary>
        /// 注册消费者
        /// </summary>
        public async Task RegisterConsumer()
        {

            //配置延迟队列参数
            delayQueueArgs.Add("x-dead-letter-exchange", XDeadLetterExchange);//延迟队列的死信交换机
            delayQueueArgs.Add("x-dead-letter-routing-key", XDeadLetterRoutingKey);//延迟队列的私信路由键
            if (XMessageTTL != -1)//设置延迟队列中所有消息的统一过期时间
            {
                delayQueueArgs.Add("x-message-ttl", XMessageTTL);
            }

            //持久化延时队列用于消息发送

            //持久化一个延时交换机 autoDelete为false 手动ACk
            _channel.ExchangeDeclare(ExchangeName, ExchangeType.GetDescription(), true, false);

            //一个持久化延时队列
            _channel.QueueDeclare(QueueName, true, false, false, delayQueueArgs);//手动确认

            //路由绑定交换机
            _channel.QueueBind(QueueName, ExchangeName, RoutingKey, null);


            //持久化一个死信队列用于接受延时队列发的消息，以及消费者消费

            //持久化一个死信交换机
            _channel.ExchangeDeclare(XDeadLetterExchange, XDeadLetterExchangeType.GetDescription(), true, false);

            //持久化一个死信队列
            _channel.QueueDeclare(XDeadLetterQueue, true, false, false);

            //路由绑定交换机
            _channel.QueueBind(XDeadLetterQueue, XDeadLetterExchange, XDeadLetterRoutingKey);


            //同一时刻服务器只发送一条消息给消费者
            _channel.BasicQos(QosPrefetchSize, QosPrefetchCount, QosGlobal);


            //创建延时队列消费者 轮询消息 不确认实现消息不同过期时间
            if (XMessageTTL == -1)
            {
                //创建消费者
                var delayConsumer = new EventingBasicConsumer(_channel);

                //消费者开启监听
                _channel.BasicConsume(QueueName, false, delayConsumer);

                //消费
                delayConsumer.Received += (ch, ea) =>
                {
                    byte[] bytes = ea.Body.ToArray();
                    string msg = Encoding.UTF8.GetString(bytes);

                    //Thread.Sleep(20);//消息少的时候可以加个睡眠时间减少IO
                    _channel.BasicNack(ea.DeliveryTag, false, requeue: true);

                };
            }


            //创建消费者
            var consumer = new EventingBasicConsumer(_channel);

            //消费者开启监听
            _channel.BasicConsume(XDeadLetterQueue, false, consumer);

            //消费
            consumer.Received += async (ch, ea) =>
            {
                byte[] bytes = ea.Body.ToArray();
                string msg = Encoding.UTF8.GetString(bytes);
                var result = await HandleMsg(msg);

                if (result == MQAction.SUCCESS)//处理成功，确认消费
                {
                    _channel.BasicAck(ea.DeliveryTag, false);
                    if (mQConsumerHandle != null)
                        await mQConsumerHandle.Success(msg);
                }
                if (result == MQAction.RETRY)//重试 重回队列
                {
                    
                    //_channel.BasicNack(ea.DeliveryTag, false, true);//重回队列
                   
                    _channel.BasicNack(ea.DeliveryTag, false, false);
                    if(mQConsumerHandle!=null)
                        await mQConsumerHandle.Retry(msg);

                }
                if (result == MQAction.FAILURE)//重试到达一定次数后转入失败
                {
                    _channel.BasicNack(ea.DeliveryTag, false, false);

                    //失败放入失败队列

                    //失败外部接口直接处理
                    if (mQConsumerHandle != null)
                        await mQConsumerHandle.Failure(msg);
                }
                if (result == MQAction.REJECT)//不可接收的失败
                {

                    //放入拒绝队列

                    //外部接口直接处理
                    _channel.BasicNack(ea.DeliveryTag, false, false);
                    if (mQConsumerHandle != null)
                        await mQConsumerHandle.Reject(msg);
                }

            };

            await Task.CompletedTask;

        }


        /// <summary>
        /// 处理消息
        /// </summary>
        public abstract Task<MQAction> HandleMsg(string msg);


        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="Exception"></exception>
        public void Dispose()
        {
            try
            {
                if (connection != null && connection.IsOpen)
                    connection.Close();

            }
            catch (Exception)
            {

                throw new Exception("MQ连接关闭失败");
            }

        }
    }
}
