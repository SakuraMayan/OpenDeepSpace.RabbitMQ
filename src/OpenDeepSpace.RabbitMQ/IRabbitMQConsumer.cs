using OpenDeepSpace.RabbitMQ.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenDeepSpace.RabbitMQ
{
    /// <summary>
    /// MQ消费者
    /// </summary>
    public interface IRabbitMQConsumer
    {

        /// <summary>
        /// 处理消息
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public Task<MQAction> HandleMsg(string msg);

        /// <summary>
        /// 管道中消息的大小 默认为0不限制
        /// </summary>
        public uint QosPrefetchSize { get; }

        /// <summary>
        /// 管道一次性向消费者推送的消息数量不大于该数字
        /// 如果有这么多没被消费 消费者将不会接收到新的消息
        /// </summary>
        public ushort QosPrefetchCount { get; set; } 

        /// <summary>
        /// 是否将对整个管道设置上面Qos参数
        /// 设置为true整个管道有效 设置为false只针对当前消费者
        /// </summary>
        public bool QosGlobal { get; set; } 
    }
}
