using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenDeepSpace.RabbitMQ.Exceptions
{
    /// <summary>
    /// MQ连接异常 或已宕机
    /// 出现该异常需要把消息放到数据库中
    /// 并以短信或邮件方式通知开发人员处理MQ
    /// </summary>
    public class MQConnectException:Exception
    {

        public MQConnectException()
        {

        }

        public MQConnectException(string? message):base(message)
        {

        }

        public MQConnectException(string? message, Exception? innerException):base(message, innerException)
        {

        }
    }
}
