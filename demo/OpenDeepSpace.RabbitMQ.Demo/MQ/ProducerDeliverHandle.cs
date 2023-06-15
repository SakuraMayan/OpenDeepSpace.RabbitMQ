using Microsoft.Extensions.Logging;
using OpenDeepSpace.Autofacastle.DependencyInjection;
using OpenDeepSpace.RabbitMQ;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenDeepSpace.RabbitMQ.Demo.MQ
{
    public class ProducerDeliverHandle : IMQProducerHandle,ISingleton
    {
        private readonly ILogger<ProducerDeliverHandle> logger;


        public ProducerDeliverHandle(ILogger<ProducerDeliverHandle> logger)
        {
            this.logger = logger;
        }

        public async Task Failure(string msg)
        {
            logger.LogError("我自己实现的消息处理失败");

            await Task.CompletedTask;
        }

        public async Task Success(string msg)
        {

            logger.LogInformation("我自己实现的消息处理成功");
            await Task.CompletedTask;
        }
    }
}
