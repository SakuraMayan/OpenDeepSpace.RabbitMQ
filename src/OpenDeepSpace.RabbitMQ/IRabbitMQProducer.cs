using OpenDeepSpace.RabbitMQ.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenDeepSpace.RabbitMQ
{
    /// <summary>
    /// MQ生产者
    /// </summary>
    public interface IRabbitMQProducer
    {

        /// <summary>
        /// 发布消息
        /// </summary>
        /// <param name="routingKey">路由键</param>
        /// <param name="msg">消息</param>
        /// <param name="exchangeType">交换机类型 读取Description上的内容</param>
        /// <param name="exchangeName">交换机名称</param>
        /// <param name="expirationMilliSeconds">过期时间</param>
        /// <param name="priority">消息优先级</param>
        /// <param name="SuccessAction">投递成功的动作</param>
        /// <param name="FailureAction">投递失败的动作</param>
        public Task PushMsg(string routingKey, object msg, ExchangeType exchangeType,string exchangeName, string expirationMilliSeconds=null,byte priority=0,Action SuccessAction=null,Action FailureAction=null);
        
        /// <summary>
        /// 发布消息
        /// </summary>
        /// <param name="routingKey"></param>
        /// <param name="msg"></param>
        /// <param name="exchangeType"></param>
        /// <param name="exchangeName"></param>
        /// <param name="expirationMilliSeconds"></param>
        /// <param name="priority"></param>
        /// <param name="mQProducerHandle">生产者投递消息后处理</param>
        /// <returns></returns>
        public Task PushMsg(string routingKey, object msg, ExchangeType exchangeType,string exchangeName, string expirationMilliSeconds=null,byte priority=0,IMQProducerHandle mQProducerHandle=null);



        
    }
}
