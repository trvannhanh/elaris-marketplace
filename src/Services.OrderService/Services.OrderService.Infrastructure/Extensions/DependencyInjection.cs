using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Services.OrderService.Application.Interfaces;
using Services.OrderService.Infrastructure.Consumers;
using Services.OrderService.Infrastructure.Persistence;
using Services.OrderService.Infrastructure.Publishers;
using Services.OrderService.Infrastructure.Repositories;

namespace Services.OrderService.Infrastructure.Extensions
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, string conn)
        {
            services.AddDbContext<OrderDbContext>(opt =>
                opt.UseNpgsql(conn, npgsql => npgsql.EnableRetryOnFailure()));

            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<IEventPublisher, EventPublisher>();

            services.AddMassTransit(x =>
            {
                x.SetKebabCaseEndpointNameFormatter();

                x.AddConsumer<OrderStockAvailableConsumer>();
                x.AddConsumer<PaymentSucceededConsumer>();
                x.AddConsumer<PaymentFailedConsumer>();

                x.AddConsumers(typeof(DependencyInjection).Assembly);

                x.AddEntityFrameworkOutbox<OrderDbContext>(o =>
                {  
                    o.UsePostgres();
                    o.QueryDelay = TimeSpan.FromSeconds(1);
                    o.DuplicateDetectionWindow = TimeSpan.FromMinutes(1);
                    
                    o.UseBusOutbox();
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

            return services;
        }
    }
}
