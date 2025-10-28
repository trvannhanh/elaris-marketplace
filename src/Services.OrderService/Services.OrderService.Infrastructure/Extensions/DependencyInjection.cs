using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Services.OrderService.Application.Interfaces;
using Services.OrderService.Infrastructure.Persistence;
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

            services.AddMassTransit(x =>
            {
                x.SetKebabCaseEndpointNameFormatter();
                x.AddEntityFrameworkOutbox<OrderDbContext>(o =>
                {
                    o.QueryDelay = TimeSpan.FromSeconds(10);
                    o.DuplicateDetectionWindow = TimeSpan.FromMinutes(1);
                    o.UsePostgres();
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
