using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using OpenDeepSpace.RabbitMQ;
using OpenDeepSpace.RabbitMQ.Enums;
using OpenDeepSpace.RabbitMQ.Extensions;
using OpenDeepSpace.RabbitMQ.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExchangeType = OpenDeepSpace.RabbitMQ.Enums.ExchangeType;

namespace OpenDeepSpace.RabbitMQ
{
    /// <summary>
    /// MQ消息生产者
    /// </summary>
    public class RabbitMQProducer : IRabbitMQProducer
    {
        /// <summary>
        /// 频道
        /// </summary>
        public IModel _channel { get; }

        /// <summary>
        /// 连接工厂
        /// </summary>
        private ConnectionFactory ConnectionFactory;

        private readonly RabbitMQOptions rabbitMQOption;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        /// <param name="mQCrashHandle">mq宕机通知处理</param>
        /// <exception cref="Exception"></exception>
        public RabbitMQProducer(IOptions<RabbitMQOptions> options,IMQCrashHandle mQCrashHandle)
        {
            rabbitMQOption = options.Value;

            try
            {
                //rabbitmq多集群连接
                ConnectionFactory = new ConnectionFactory()
                {
                    UserName = rabbitMQOption.UserName,
                    Password = rabbitMQOption.Password,
                    VirtualHost= rabbitMQOption.VirtualHost,

                };

                if (rabbitMQOption.AmqpConnections == null || (rabbitMQOption.AmqpConnections != null && !rabbitMQOption.AmqpConnections.Any()))
                {
                    throw new Exception("请至少设置一个mq连接");
                }
                List<AmqpTcpEndpoint> amqpTcpEndpoints= new List<AmqpTcpEndpoint>();
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
            catch (Exception ex)
            {
                //MQ建立连接失败 需要及时处理
                mQCrashHandle.Crash(ex);

            }
        }


        public async Task PushMsg(string routingKey, object msg, ExchangeType exchangeType, string exchangeName, string expirationMilliSeconds = null, byte priority = 0, Action SuccessAction = null, Action FailureAction = null)
        {
            await PushMsg(routingKey, msg, exchangeType, exchangeName, expirationMilliSeconds, priority, SuccessAction, FailureAction,null);
        }

        public async Task PushMsg(string routingKey, object msg, ExchangeType exchangeType, string exchangeName, string expirationMilliSeconds = null, byte priority = 0, IMQProducerHandle mQProducerHandle = null)
        {
            await PushMsg(routingKey, msg, exchangeType, exchangeName, expirationMilliSeconds, priority,null,null, mQProducerHandle); 
        }

        private async Task PushMsg(string routingKey, object msg, ExchangeType exchangeType, string exchangeName, string expirationMilliSeconds = null, byte priority = 0, Action SuccessAction = null, Action FailureAction = null,IMQProducerHandle mQProducerHandle=null)
        {

            try
            {
                //MQ宕机 _channel为空 抛出异常
                //持久化一个交换机 autoDelete为false 手动ACk
                _channel.ExchangeDeclare(exchangeName, exchangeType.GetDescription(), true, false, null);
                //消息内容
                byte[] msgBodys = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(msg));

                //持久化消息
                var basicProp = _channel.CreateBasicProperties();
                basicProp.DeliveryMode = 2;
                if (!string.IsNullOrWhiteSpace(expirationMilliSeconds))
                    basicProp.Expiration = expirationMilliSeconds;//消息过期时间 消息过期就成为死信消息
                if (priority < 0 || priority > 9)
                    throw new Exception("优先级范围为:0-9");
                basicProp.Priority = priority;//优先级只在优先队列有效

                //防止生产者丢失消息或消息没有成功投递 开启异步Confirm模式
                _channel.ConfirmSelect();
                //消息发送成功的时候进入到这个事件：即RabbitMq服务器告诉生产者，我已经成功收到了消息
                EventHandler<BasicAckEventArgs> BasicAcks = new EventHandler<BasicAckEventArgs>(async(o, basic) =>
                {

                    if (SuccessAction != null)
                        SuccessAction();
                    if (mQProducerHandle != null)
                        await mQProducerHandle.Success(JsonConvert.SerializeObject(msg));
                });
                //消息发送失败的时候进入到这个事件：即RabbitMq服务器告诉生产者，你发送的这条消息我没有成功的投递到Queue中，或者说我没有收到这条消息。
                EventHandler<BasicNackEventArgs> BasicNacks = new EventHandler<BasicNackEventArgs>(async(o, basic) =>
                {
                    //MQ服务器出现了异常，可能会出现Nack的情况

                    if (FailureAction != null)
                        FailureAction();
                    if(mQProducerHandle!=null)
                        await mQProducerHandle.Failure(JsonConvert.SerializeObject(msg));

                });
                _channel.BasicAcks += BasicAcks;
                _channel.BasicNacks += BasicNacks;


                //发布消息
                _channel.BasicPublish(new PublicationAddress(exchangeType.GetDescription(), exchangeName, routingKey), basicProp, msgBodys);
            }
            catch (Exception ex)
            {
                //MQ宕机 _channel为空 执行失败投递
                if (FailureAction != null)
                    FailureAction();
                if (mQProducerHandle != null)
                    await mQProducerHandle.Failure(JsonConvert.SerializeObject(msg));
            }

            await Task.CompletedTask;
        }

        
    }

}
