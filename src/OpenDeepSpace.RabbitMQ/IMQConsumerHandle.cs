using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenDeepSpace.RabbitMQ
{
    /// <summary>
    /// MQ消费者 消费结果处理后续处理接口
    /// </summary>
    public interface IMQConsumerHandle
    {
        /// <summary>
        /// 成功消费
        /// </summary>
        /// <param name="msg">序列化的消息</param>
        /// <returns></returns>
        public Task Success(string msg);

        /// <summary>
        /// 重试处理
        /// </summary>
        /// <param name="msg">序列化的消息</param>
        /// <returns></returns>
        public Task Retry(string msg);

        /// <summary>
        /// 失败消费
        /// </summary>
        /// <param name="msg">序列化的消息</param>
        /// <returns></returns>
        public Task Failure(string msg);

        /// <summary>
        /// 直接拒绝
        /// </summary>
        /// <param name="msg">序列化的消息</param>
        /// <returns></returns>
        public Task Reject(string msg);

    }
}
