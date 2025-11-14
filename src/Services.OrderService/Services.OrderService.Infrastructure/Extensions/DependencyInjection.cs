using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Services.OrderService.Application.Interfaces;
using Services.OrderService.Infrastructure.Consumers;
using Services.OrderService.Infrastructure.Persistence;
using Services.OrderService.Infrastructure.Publishers;
using Services.OrderService.Infrastructure.Repositories;
using Services.OrderService.Infrastructure.Saga;
using Services.OrderService.Infrastructure.Services;

namespace Services.OrderService.Infrastructure.Extensions
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, string conn, IConfiguration configuration)
        {
            services.AddDbContext<OrderDbContext>(opt =>
                opt.UseNpgsql(conn, npgsql => npgsql.EnableRetryOnFailure()));

            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<IEventPublisher, EventPublisher>();
            services.AddScoped<IInventoryGrpcClient, InventoryGrpcClient>();
            services.AddScoped<IPaymentGrpcClient, PaymentGrpcClient>();
            services.AddMassTransit(x =>
            {
                x.SetKebabCaseEndpointNameFormatter();

                // <-- use built-in MassTransit message scheduler (no Rabbit plugin)
                x.AddMessageScheduler(new Uri("queue:scheduler"));

                x.AddConsumer<BasketCheckedOutConsumer>();
                x.AddConsumer<CompleteOrderConsumer>();
                x.AddConsumer<CancelOrderConsumer>();

                x.AddSagaStateMachine<OrderStateMachine, OrderState>()
                 .MongoDbRepository(r =>
                 {
                     var mongoUrl = configuration.GetConnectionString("Mongo")
                         ?? throw new InvalidOperationException("Mongo connection string is missing!");

                     r.Connection = mongoUrl;
                     r.DatabaseName = "order_saga_db";
                     r.CollectionName = "order_sagas";
                 });

                x.AddEntityFrameworkOutbox<OrderDbContext>(o =>
                {
                    o.UsePostgres();
                    o.UseBusOutbox();
                    o.QueryDelay = TimeSpan.FromSeconds(10);
                    o.DuplicateDetectionWindow = TimeSpan.FromMinutes(1);
                });

                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host("rabbitmq", "/", h =>
                    {
                        h.Username("guest");
                        h.Password("guest");
                    });

                    // <-- use message scheduler, not the rabbit delayed exchange plugin
                    cfg.UseMessageScheduler(new Uri("queue:scheduler"));
                    cfg.ConfigureEndpoints(context);
                });
            });

            services.AddMassTransitHostedService();
            services.AddOptions<MassTransitHostOptions>()
                .Configure(options => options.WaitUntilStarted = true);


            return services;
        }
    }
}
