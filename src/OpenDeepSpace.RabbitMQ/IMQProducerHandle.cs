using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenDeepSpace.RabbitMQ
{
    /// <summary>
    /// MQ生产者 消息投递结果处理接口
    /// </summary>
    public interface IMQProducerHandle
    {

        /// <summary>
        /// 投递成功的处理
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public Task Success(string msg);

        /// <summary>
        /// 投递失败的处理
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public Task Failure(string msg);
    }
}
