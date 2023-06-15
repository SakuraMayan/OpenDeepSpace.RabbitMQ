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
    /// 
    /// </summary>
    public abstract class RabbitMQConsumer :  IRabbitMQConsumer,IDisposable
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
        /// 交换机类型
        /// </summary>
        public Enums.ExchangeType ExchangeType { get; set; }

        /// <summary>
        /// 重试次数 默认为-1表示不限制重试次数
        /// 重试次数设置至少大于1
        /// </summary>
        public int RetryCount { get; set; } = -1;

        /// <summary>
        /// 每次重试的平均时间间隔 单位毫秒
        /// 利用消息在整个管道中过期时间TTL
        /// </summary>
        public int RetryInterval { get; set; } = -1;

        /// <summary>
        /// 消息每次重试时间间隔设置
        /// 如果设置了这个参数 那么重试次数以及时间间隔以该集合中的个数为主
        /// RetryCount RetryInterval将无效
        /// </summary>
        public int[] RetryIntervals { get; set; }

        /// <summary>
        /// 管道中消息的大小 默认为0不限制
        /// </summary>
        public uint QosPrefetchSize { get;} = 0;

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


        //以下定义重试交换机以及队列实现重试机制
        /// <summary>
        /// 重试路由键
        /// </summary>
        public string RetryRoutingKey { get; set; }
        /// <summary>
        /// 重试队列名称
        /// </summary>
        public string RetryQueueName { get; set; }
        /// <summary>
        /// 重试交换机名称
        /// </summary>
        public string RetryExchangeName { get; set; }

        /// <summary>
        /// 重试交换机类型
        /// </summary>
        public Enums.ExchangeType RetryExchangeType { get; set; }

        /// <summary>
        /// 重试队列参数
        /// </summary>
        public Dictionary<string, object> RetryQueueArgs { get; set; }

        //以下定义消息失败交换机以及队列

        //以下定义消息拒绝交换机以及队列

        /// <summary>
        /// 消费者重回策略
        /// </summary>
        public ConsumerRetryPolicy RetryPolicy { get; set; } = ConsumerRetryPolicy.Requeue;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        /// <param name="mQConsumerHandle"></param>
        /// <exception cref="Exception"></exception>
        public RabbitMQConsumer(IOptions<RabbitMQOptions> options,IMQConsumerHandle mQConsumerHandle)
        {
            rabbitMQOption = options.Value;
            this.mQConsumerHandle = mQConsumerHandle;

            //重试队列 
            RetryQueueArgs = new Dictionary<string, object>();
            

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
        }

        /// <summary>
        /// 注册消费者
        /// </summary>
        public async Task RegisterConsumer()
        {

            if (RetryPolicy != ConsumerRetryPolicy.Requeue)
            {
                if (RetryInterval == -1 && (RetryIntervals?.Length == null || RetryIntervals?.Length == 0))
                    throw new Exception("请设置重试时间间隔");
                RetryCount = RetryIntervals?.Length ?? RetryCount;
                if (RetryCount <= 0 && RetryCount != -1)
                    throw new Exception("设置的重试次数需要大于1");
                if (RetryInterval <= 0 && (RetryIntervals?.Length == null || RetryIntervals?.Length == 0))
                    throw new Exception("设置的重试时间间隔必须大于0");

                //重试队列参数设置
                if (RetryInterval>0)
                    RetryQueueArgs.Add("x-message-ttl", RetryInterval);//统一平均间隔重试时间
                //重试队列 用于将 Retry 队列中超时的消息投递给正常的队列
                RetryQueueArgs.Add("x-dead-letter-exchange", ExchangeName);
                RetryQueueArgs.Add("x-dead-letter-routing-key", RoutingKey);
               
                //持久化一个重试交换机
                _channel.ExchangeDeclare(RetryExchangeName, RetryExchangeType.GetDescription(), true, false, null);

                //持久化一个重试队列
                _channel.QueueDeclare(RetryQueueName, true, false, false, RetryQueueArgs);//手动确认

                //路由绑定交换机
                _channel.QueueBind(RetryQueueName, RetryExchangeName, RetryRoutingKey, null);
                
            }

 
            //持久化一个交换机 autoDelete为false 手动ACk
            _channel.ExchangeDeclare(ExchangeName, ExchangeType.GetDescription(), true, false, null);

            //一个持久化队列
            _channel.QueueDeclare(QueueName, true, false, false, null);//手动确认

            //路由绑定交换机
            _channel.QueueBind(QueueName, ExchangeName, RoutingKey, null);
           

            //同一时刻服务器只发送一条消息给消费者
            _channel.BasicQos(QosPrefetchSize, QosPrefetchCount, QosGlobal);

            //创建消费者
            var consumer = new EventingBasicConsumer(_channel);

            //消费者开启监听
            _channel.BasicConsume(QueueName, false, consumer);

            //创建重试队列消费者 轮询消息 不确认实现消息不同过期时间
            if (RetryInterval == -1)
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

            //消费
            consumer.Received += async (ch, ea) =>
            {
                byte[] bytes = ea.Body.ToArray();
                string msg = Encoding.UTF8.GetString(bytes);
                var result = await HandleMsg(msg);


                //重试相关次数查看
                var _headers = ea.BasicProperties.Headers;        //消息头
                var _death = default(Dictionary<string, object>);       //死信参数
                if (_headers != null && _headers.ContainsKey("x-death"))
                    _death = (Dictionary<string, object>)(_headers["x-death"] as List<object>)[0];


                
                if (result == MQAction.SUCCESS)//处理成功，确认消费
                { 
                    _channel.BasicAck(ea.DeliveryTag, false);
                    if (mQConsumerHandle != null)
                       await mQConsumerHandle.Success(msg);
                }
                if (result == MQAction.RETRY)//重试 重回队列
                {
                    if (RetryPolicy == ConsumerRetryPolicy.Requeue)
                    {
                        //如果重回队列达到一定次数 也转为失败
                        var retryCount = (long)(_death?["count"] ?? default(long)); //查询当前消息被重新投递的次数 (首次则为0)
                        //重试达到一定次数 直接转入失败
                        if (retryCount == RetryCount)
                        { 
                            result = MQAction.FAILURE;//失败处理
                        }
                        _channel.BasicNack(ea.DeliveryTag, false, true);
                        
                    }
                    //加入重试队列
                    if (RetryPolicy == ConsumerRetryPolicy.JoinRetryQueue)
                    {
                        var retryCount = (long)(_death?["count"] ?? default(long)); //查询当前消息被重新投递的次数 (首次则为0)
                        //重试达到一定次数 直接转入失败
                        if (retryCount == RetryCount)
                            result = MQAction.FAILURE;//失败处理
                        else 
                        {

                            //下一次投递的间隔时间
                            if (RetryInterval != -1)//平均投递时间
                                ea.BasicProperties.Expiration = RetryInterval.ToString();
                            else//自定义重试时间
                                ea.BasicProperties.Expiration = RetryIntervals[retryCount].ToString();

                            //发布消息到重试队列
                            //将消息投递给重试队列(会自动增加 death 次数)
                            _channel.BasicPublish(new PublicationAddress(RetryExchangeType.GetDescription(),RetryExchangeName,RetryRoutingKey), ea.BasicProperties, ea.Body);


                            _channel.BasicNack(ea.DeliveryTag, false, false);
                        }


                    }

                    //不重试 外部接口直接处理
                    if (mQConsumerHandle != null && RetryPolicy == ConsumerRetryPolicy.NoRetry)
                    {
                        
                        _channel.BasicNack(ea.DeliveryTag, false, false);
                        await mQConsumerHandle.Retry(msg);
                    }
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
                    if(mQConsumerHandle!=null)
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
