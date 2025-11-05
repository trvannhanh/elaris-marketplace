
using MassTransit;
using MassTransit.MongoDbIntegration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Services.OrderService.Infrastructure.Saga
{
    public static class SagaConfig
    {
        public static void AddOrderSaga(this IServiceCollection services, IConfiguration config)
        {
            services.AddMassTransit(x =>
            {
                x.AddSagaStateMachine<OrderStateMachine, OrderState>()
                 .MongoDbRepository(r =>
                 {
                     r.Connection = config.GetConnectionString("Mongo");
                     r.DatabaseName = "order_saga_db";
                     r.CollectionName = "order_sagas";
                 });

                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host("rabbitmq", "/", h =>
                    {
                        h.Username("guest");
                        h.Password("guest");
                    });

                    cfg.ConfigureEndpoints(context);
                });
            });
        }
    }
}
