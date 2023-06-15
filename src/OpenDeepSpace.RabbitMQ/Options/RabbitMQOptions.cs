using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenDeepSpace.RabbitMQ.Options
{
    /// <summary>
    /// MQOption
    /// </summary>
    public class RabbitMQOptions
    {
        

        /// <summary>
        /// 用户名
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// 密码
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string VirtualHost { get; set; }

        public List<AmqpConnection> AmqpConnections { get; set; }
    }


    /// <summary>
    /// Amqp连接
    /// </summary>
    public class AmqpConnection
    {
        /// <summary>
        /// 主机名
        /// </summary>
        public string HostName { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        public int Port { get; set; }
    }
   
}
