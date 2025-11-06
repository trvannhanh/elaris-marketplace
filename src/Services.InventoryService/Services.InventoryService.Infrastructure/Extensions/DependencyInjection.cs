using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Services.InventoryService.Application.Interfaces;
using Services.InventoryService.Infrastructure.Consumers;
using Services.InventoryService.Infrastructure.Persistence;
using Services.InventoryService.Infrastructure.Repositories;
using System.Reflection;


namespace Services.InventoryService.Infrastructure.Extensions
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, string conn)
        {

            services.AddDbContext<InventoryDbContext>(options =>
                options.UseNpgsql(conn, npgsql => npgsql.EnableRetryOnFailure()));


            services.AddScoped<IInventoryRepository, InventoryRepository>();

            services.AddMassTransit(x =>
            {

                x.AddConsumer<ReserveInventoryConsumer>();
                x.AddConsumer<ReleaseInventoryConsumer>();
                x.AddConsumer<ConfirmInventoryConsumer>();
                x.AddConsumer<ProductCreatedConsumer>();

                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host("rabbitmq", "/", h => { h.Username("guest"); h.Password("guest"); });
                    cfg.ConfigureEndpoints(context);
                });
            });

            return services;
        }
    }
}
