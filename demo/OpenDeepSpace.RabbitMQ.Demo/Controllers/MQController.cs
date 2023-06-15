using Microsoft.AspNetCore.Mvc;
using OpenDeepSpace.Autofacastle.DependencyInjection.Attributes;
using OpenDeepSpace.RabbitMQ;
using OpenDeepSpace.RabbitMQ.Demo.MQ;

namespace OpenDeepSpace.RabbitMQ.Demo.Controllers
{

    /// <summary>
    /// MQ测试Controller
    /// </summary>
    [ApiController]
    [Route("[controller]/[action]")]
    public class MQController : ControllerBase
    {

        [AutomaticInjection]
        private readonly IRabbitMQProducer _producer;

        [AutomaticInjection]
        private readonly ILogger<MQController> _logger;

        [AutomaticInjection(ImplementationType =typeof(ProducerDeliverHandle))]
        private readonly IMQProducerHandle _producerHandle;

        /// <summary>
        /// 
        /// </summary>
        [HttpGet]
        public void Test()
        {

            /*_producer.PushMsg("OpenDeepSpacerouting", "success", Wy.RabbitMQ.Enums.ExchangeType.Direct, "OpenDeepSpace", SuccessAction: () =>
            {
                _logger.LogInformation("成功投递消息");
            }, FailureAction: () => {
                _logger.LogError("失败投递消息格外处理");
            });*/

            //_producer.PushMsg("OpenDeepSpacerouting", "failure", Wy.RabbitMQ.Enums.ExchangeType.Direct, "OpenDeepSpace",mQProducerHandle:_producerHandle);
            /*_producer.PushMsg("OpenDeepSpacerouting", "success", Wy.RabbitMQ.Enums.ExchangeType.Direct, "OpenDeepSpace",mQProducerHandle:_producerHandle);
            _producer.PushMsg("OpenDeepSpacerouting", "success", Wy.RabbitMQ.Enums.ExchangeType.Direct, "OpenDeepSpace",mQProducerHandle:_producerHandle);
            _producer.PushMsg("OpenDeepSpacerouting", "retry", Wy.RabbitMQ.Enums.ExchangeType.Direct, "OpenDeepSpace",mQProducerHandle:_producerHandle);
            _producer.PushMsg("OpenDeepSpacerouting", "retry", Wy.RabbitMQ.Enums.ExchangeType.Direct, "OpenDeepSpace",mQProducerHandle:_producerHandle);
            _producer.PushMsg("OpenDeepSpacerouting", "success", Wy.RabbitMQ.Enums.ExchangeType.Direct, "OpenDeepSpace",mQProducerHandle:_producerHandle);*/
            _producer.PushMsg("OpenDeepSpacerouting", $"retry {DateTime.Now}", OpenDeepSpace.RabbitMQ.Enums.ExchangeType.Direct, "OpenDeepSpace",mQProducerHandle:_producerHandle);
        }

        /// <summary>
        /// 
        /// </summary>
        [HttpGet]
        public void TestDelay()
        {
            //_producer.PushMsg("OpenDeepSpacernafdrouting", new { state="success",time=DateTime.Now}, Wy.RabbitMQ.Enums.ExchangeType.Direct, "OpenDeepSpace.delay", mQProducerHandle: _producerHandle);


            //设置不同过期时间
            _producer.PushMsg("OpenDeepSpacernafdrouting", new { state = "success", time = DateTime.Now }, OpenDeepSpace.RabbitMQ.Enums.ExchangeType.Direct,
                "OpenDeepSpace.delay", expirationMilliSeconds: "5000", mQProducerHandle: _producerHandle);
            _producer.PushMsg("OpenDeepSpacernafdrouting", new { state = "success", time = DateTime.Now }, OpenDeepSpace.RabbitMQ.Enums.ExchangeType.Direct,
                "OpenDeepSpace.delay", expirationMilliSeconds: "3000", mQProducerHandle: _producerHandle);
            _producer.PushMsg("OpenDeepSpacernafdrouting", new { state = "success", time = DateTime.Now }, OpenDeepSpace.RabbitMQ.Enums.ExchangeType.Direct,
                "OpenDeepSpace.delay", expirationMilliSeconds: "10000", mQProducerHandle: _producerHandle);
        }
    }
}
