using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OpenDeepSpace.RabbitMQ;
using OpenDeepSpace.RabbitMQ.Enums;
using OpenDeepSpace.RabbitMQ.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenDeepSpace.RabbitMQ.Demo.MQ
{
    /// <summary>
    /// 测试ConsumerOne
    /// </summary>
    public class TestConsumerOne:RabbitMQConsumer
    {
        private readonly ILogger<TestConsumerOne> logger;


        public TestConsumerOne(IOptions<RabbitMQOptions> options, IMQConsumerHandle mQConsumerHandle, ILogger<TestConsumerOne> logger) : base(options, mQConsumerHandle)
        {
            RoutingKey = "OpenDeepSpacerouting";
            ExchangeName = "OpenDeepSpace";
            ExchangeType = ExchangeType.Direct;
            QueueName = "OpenDeepSpacequeue";
            QosGlobal = true;
            QosPrefetchCount = 3;

            RetryPolicy = ConsumerRetryPolicy.JoinRetryQueue;
            /*RetryCount = 3;
            RetryInterval = 10000;*/
            RetryIntervals = new[] { 10000, 20000 };
            RetryExchangeName = "OpenDeepSpace.retry";
            RetryExchangeType = ExchangeType.Direct;
            RetryQueueName = "OpenDeepSpaceretryqueue";
            RetryRoutingKey = "OpenDeepSpaceretryrouting";
            this.logger = logger;
        }

        public override async Task<MQAction> HandleMsg(string msg)
        {
            //这里为了测试方便 通过把消息内容设置成消息处理的枚举 来测试

            msg = JsonConvert.DeserializeObject<string>(msg);

            logger.LogInformation($"{DateTime.Now}处理消息:{msg}");
            if(msg.Contains("success"))//判断为处理成功的消息
                return MQAction.SUCCESS;
            if(msg.Contains("retry"))
                return MQAction.RETRY;//重试次数达到之后自动转为失败状态
            if (msg.Contains("failure"))
                return MQAction.FAILURE;
            if (msg.Contains("reject"))
                return MQAction.REJECT;


            return MQAction.SUCCESS;


        }
    }
}
