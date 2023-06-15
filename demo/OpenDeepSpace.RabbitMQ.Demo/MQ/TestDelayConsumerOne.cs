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
    /// 测试延时队列
    /// </summary>
    public class TestDelayConsumerOne : RabbitMQDelayDeadLetterConsumer
    {
        private readonly ILogger<TestDelayConsumerOne> logger;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        public TestDelayConsumerOne(IOptions<RabbitMQOptions> options, IMQConsumerHandle mQConsumerHandle, ILogger<TestDelayConsumerOne> logger) : base(options, mQConsumerHandle)
        {
            RoutingKey = "OpenDeepSpacernafdrouting";//延迟队列路由键
            ExchangeName = "OpenDeepSpace.delay";
            ExchangeType = ExchangeType.Direct;
            QueueName = "OpenDeepSpacedeleyqueue";//延迟队列名称

            XDeadLetterRoutingKey = "OpenDeepSpacedlrnafxrouting";//延迟过期到死信队列
            XDeadLetterExchange = "OpenDeepSpace.dead";
            XDeadLetterExchangeType = ExchangeType.Direct;
            XDeadLetterQueue = "OpenDeepSpacedlqueue";
            //XMessageTTL = 10000;//10秒后消费消息
            this.logger = logger;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public async override Task<MQAction> HandleMsg(string msg)
        {
            
            var msgObj=JsonConvert.DeserializeObject<object>(msg);

            logger.LogInformation($"处理消息:{msg}:{DateTime.Now}");
            if (msg.Contains("success"))//判断为处理成功的消息
                return MQAction.SUCCESS;
            if (msg.Contains("retry"))
                return MQAction.RETRY;//重试次数达到之后自动转为失败状态
            if (msg.Contains("failure"))
                return MQAction.FAILURE;
            if (msg.Contains("reject"))
                return MQAction.REJECT;

            return MQAction.SUCCESS;
        }
    }
}
