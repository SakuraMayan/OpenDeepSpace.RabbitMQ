using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenDeepSpace.RabbitMQ
{
    /// <summary>
    /// 空MQ宕机实现
    /// </summary>
    public class NullMQCrashHandle : IMQCrashHandle
    {
        private readonly ILogger<NullMQCrashHandle> logger;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="logger"></param>
        public NullMQCrashHandle(ILogger<NullMQCrashHandle> logger)
        {
            this.logger = logger;   
        }
        public async Task Crash(Exception ex)
        {
            
            logger.LogError($"MQ连接出现问题或已宕机:{ex.Message},stack:{ex.StackTrace}");

            await Task.CompletedTask;

        }
    }
}
