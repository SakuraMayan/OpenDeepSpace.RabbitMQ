using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenDeepSpace.RabbitMQ
{
    /// <summary>
    /// MQ出现宕机等处理
    /// 通过注入具体实例来执行
    /// </summary>
    public interface IMQCrashHandle
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Task Crash(Exception ex);
    }
}
