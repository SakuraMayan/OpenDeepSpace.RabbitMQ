using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenDeepSpace.RabbitMQ.Enums
{
    /// <summary>
    /// MQ动作枚举
    /// </summary>
    public enum MQAction
    {
        SUCCESS,  // 处理成功
        RETRY,   // 可以重试的错误
        FAILURE, // 失败的 即重试达到指定次数后进入失败
        REJECT,  // 无需重试的错误 直接拒绝
    }
}
