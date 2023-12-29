using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenDeepSpace.RabbitMQ.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OpenDeepSpace.RabbitMQ.Extensions
{
    public static class RabbitMQExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <param name="assemblies"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public static IServiceCollection InitRabbitMQ(this IServiceCollection services, IEnumerable<Assembly> assemblies, Action<RabbitMQOptions> action = null)
        {

            if(assemblies == null)
                throw new ArgumentNullException(nameof(assemblies));

            return services.InitRabbitMQ(assemblies.SelectMany(t => t.GetTypes()), action);
        }

        /// <summary>
        /// 配置MQ
        /// </summary>
        /// <param name="services"></param>
        /// <param name="types">类型集</param>
        /// <param name="action"></param>
        /// <returns></returns>
        public static IServiceCollection InitRabbitMQ(this IServiceCollection services, IEnumerable<Type> types,Action<RabbitMQOptions> action=null)
        {
            if(types ==null)
                throw new ArgumentNullException(nameof(types));
           
            var configuration=services.BuildServiceProvider().GetRequiredService<IConfiguration>();

            //注入MQ配置
            if (action != null)
            {
                services.Configure(action);
            }
            else
            { 
            
                services.Configure<RabbitMQOptions>(configuration.GetSection("RabbitMQ"));
            }
            //注入MQ信息 单例生产者
            services.AddSingleton<IRabbitMQProducer, RabbitMQProducer>();

            //注入MQ宕机等问题 统一通知处理
            services.AddTransient<IMQCrashHandle, NullMQCrashHandle>();
            //MQ生产者消息投递Confirm后续处理
            services.AddTransient<IMQProducerHandle, NullMQProducerHandle>();
            //MQ消费者消费情况后续处理
            services.AddTransient<IMQConsumerHandle, NullMQConsumerHandle>();

            //查找集成了RabbitMQConsumer的类
            types = types.Where(t => t.IsClass && !t.IsAbstract  && t.GetInterface(typeof(IRabbitMQConsumer).Name) != null).ToArray();

            foreach (Type hostserviceType in types)
            {
                services.AddSingleton(hostserviceType);//开启消费者监听 单例消费者

                var mqConsumer = services.BuildServiceProvider().GetRequiredService(hostserviceType);
                if (mqConsumer is RabbitMQConsumer)
                    _ = (mqConsumer as RabbitMQConsumer).RegisterConsumer();
                if (mqConsumer is RabbitMQDelayDeadLetterConsumer)
                    _ = (mqConsumer as RabbitMQDelayDeadLetterConsumer).RegisterConsumer();
            }

            return services;
        }

        
    }
}
