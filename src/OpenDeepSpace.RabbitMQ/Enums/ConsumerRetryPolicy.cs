using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenDeepSpace.RabbitMQ.Enums
{
    /// <summary>
    /// 消费重试策略
    /// </summary>
    public enum ConsumerRetryPolicy
    {

        /// <summary>
        /// 直接重回当前队列
        /// </summary>
        Requeue,

        /// <summary>
        /// 加入重试队列需要设置重试交换机及其队列
        /// </summary>
        JoinRetryQueue,

        /// <summary>
        /// 不重试 需要外部接口直接处理 
        /// 依然需要设置重试队列和交换机
        /// </summary>
        NoRetry

    }
}
