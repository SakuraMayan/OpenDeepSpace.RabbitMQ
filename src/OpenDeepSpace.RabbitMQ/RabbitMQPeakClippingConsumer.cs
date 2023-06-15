using Microsoft.Extensions.Options;
using OpenDeepSpace.RabbitMQ.Enums;
using OpenDeepSpace.RabbitMQ.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenDeepSpace.RabbitMQ
{
    /// <summary>
    /// 消峰
    /// </summary>
    public abstract class RabbitMQPeakClippingConsumer : IRabbitMQConsumer, IDisposable
    {

        public IModel _channel { get; }
        /// <summary>
        /// 连接工厂
        /// </summary>
        private ConnectionFactory ConnectionFactory;

        private readonly RabbitMQOptions rabbitMQOption;

        private readonly IConnection connection;

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
        public string ExchangeType { get; set; }


        /// <summary>
        /// 管道中消息的大小 默认为0不限制
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
        /// 队列中存储消息的数量处于ready状态消息的数量
        /// 默认为-1不限制
        /// </summary>
        public int XMaxLength { get; set; } = -1;

        /// <summary>
        /// 存储处于ready状态消息占用内存的大小(只计算消息体的字节数，不计算消息头、消息属性占用的字节数)
        /// 单位：字节
        /// 默认为-1 不限制大小
        /// </summary>
        public int XMaxLengthBytes { get; set; } = -1;

        /// <summary>
        /// 处于ready状态存储消息的个数或消息占用的容量超过设定值后的处理策略
        /// drop-head 删除queue头部的消息
        /// reject-publish 最近发来的消息将被丢弃
        /// reject-publish-dlx 拒绝发送到死信交换器
        /// 类型为quorum的queue只支持drop-head
        /// </summary>
        public string XOverflow { get; set; } = "reject-publish";

        /// <summary>
        /// 削峰队列参数
        /// </summary>
        private readonly Dictionary<string, object> peakClippingQueueArgs = new Dictionary<string, object>();


        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        /// <exception cref="Exception"></exception>
        public RabbitMQPeakClippingConsumer(IOptions<RabbitMQOptions> options)
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
        }

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

        public abstract Task<MQAction> HandleMsg(string msg);

        /// <summary>
        /// 注册消费者
        /// </summary>
        public async Task RegisterConsumer()
        {
            //削峰队列参数设置
            if (XMaxLength != -1)
                peakClippingQueueArgs.Add("x-max-length", XMaxLength);
            if (XMaxLengthBytes != -1)
                peakClippingQueueArgs.Add("x-max-length-bytes", XMaxLengthBytes);
            peakClippingQueueArgs.Add("x-overflow", XOverflow);

            //持久化一个交换机 autoDelete为false 手动ACk
            _channel.ExchangeDeclare(ExchangeName, ExchangeType, true, false, null);

            //一个持久化队列
            _channel.QueueDeclare(QueueName, true, false, false, peakClippingQueueArgs);//手动确认

            //路由绑定交换机
            _channel.QueueBind(QueueName, ExchangeName, RoutingKey, null);

            //同一时刻服务器只发送一条消息给消费者
            _channel.BasicQos(QosPrefetchSize, QosPrefetchCount, QosGlobal);

            //创建消费者
            var consumer = new EventingBasicConsumer(_channel);

            //消费者开启监听
            _channel.BasicConsume(QueueName, false, consumer);

            //消费
            consumer.Received += async (ch, ea) =>
            {
                byte[] bytes = ea.Body.ToArray();
                string msg = Encoding.UTF8.GetString(bytes);
                var result = await HandleMsg(msg);

                if (result == MQAction.SUCCESS)//处理成功，确认消费
                    _channel.BasicAck(ea.DeliveryTag, false);
                if (result == MQAction.RETRY)//失败 重回队列
                    _channel.BasicNack(ea.DeliveryTag, false, true);
                if (result == MQAction.REJECT)//拒绝
                    _channel.BasicNack(ea.DeliveryTag, false, false);

            };
        }
        
    }
}
