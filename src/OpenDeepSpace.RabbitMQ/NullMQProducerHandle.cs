using Microsoft.Extensions.Logging;
using OpenDeepSpace.RabbitMQ;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenDeepSpace.RabbitMQ
{
    /// <summary>
    /// 
    /// </summary>
    public class NullMQProducerHandle : IMQProducerHandle
    {

        private readonly ILogger<NullMQCrashHandle> logger;

        public NullMQProducerHandle(ILogger<NullMQCrashHandle> logger)
        {
            this.logger = logger;
        }

        public async Task Failure(string msg)
        {
            logger.LogError($"生产者投递消息失败:{msg}");

            await Task.CompletedTask;
        }

        public async Task Success(string msg)
        {

            logger.LogInformation($"生产者投递消息成功:{msg}");

            await Task.CompletedTask;
        }
    }
}
