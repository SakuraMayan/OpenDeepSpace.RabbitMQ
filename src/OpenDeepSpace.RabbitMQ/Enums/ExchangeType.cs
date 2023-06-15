using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenDeepSpace.RabbitMQ.Enums
{
    /// <summary>
    /// 交换机类型
    /// </summary>
    public enum ExchangeType
    {

        /// <summary>
        /// 直连 点对点 根据路由键
        /// </summary>
        [Description("direct")]
        Direct,

        /// <summary>
        /// 广播模式 发布订阅
        /// </summary>
        [Description("fanout")]
        Fanout,

        /// <summary>
        /// hearders属性进行匹配
        /// </summary>
        [Description("headers")]
        Headers,

        /// <summary>
        /// 根据路由键模糊匹配
        /// </summary>
        [Description("topic")]
        Topic

    }
}
