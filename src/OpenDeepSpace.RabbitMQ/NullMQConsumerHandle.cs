using Microsoft.Extensions.Logging;
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
    public class NullMQConsumerHandle : IMQConsumerHandle
    {
        private readonly ILogger<NullMQConsumerHandle> logger;

        public NullMQConsumerHandle(ILogger<NullMQConsumerHandle> logger)
        {
            this.logger = logger;
        }

        public async Task Failure(string msg)
        {

            logger.LogError($"消费失败:{msg}");

            await Task.CompletedTask;
        }

        public async Task Reject(string msg)
        {
            logger.LogError($"消费拒绝:{msg}");

            await Task.CompletedTask;
        }

        public async Task Retry(string msg)
        {
            logger.LogError($"消费重试:{msg}");
            await Task.CompletedTask;
        }

        public async Task Success(string msg)
        {
            logger.LogInformation($"消费成功:{msg}");
            await Task.CompletedTask;
        }
    }
}
