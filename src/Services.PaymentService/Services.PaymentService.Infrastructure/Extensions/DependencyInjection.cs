using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Services.PaymentService.Application.Interfaces;
using Services.PaymentService.Infrastructure.Consumers;
using Services.PaymentService.Infrastructure.Persistence;
using Services.PaymentService.Infrastructure.Repositories;
using Services.PaymentService.Infrastructure.Services;

namespace Services.PaymentService.Infrastructure.Extensions
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, string conn)
        {
            // DbContext + repo 
            services.AddDbContext<PaymentDbContext>(opt => opt.UseNpgsql(conn));
            services.AddScoped<IPaymentRepository, PaymentRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IPaymentGateway, PaymentGatewayService>();
            services.AddScoped<IOrderServiceClient, OrderServiceClient>();

            // MassTransit + consumer
            services.AddMassTransit(x =>
            {
                x.AddConsumer<AuthorizePaymentConsumer>();
                x.AddConsumer<RefundPaymentConsumer>();
                x.AddConsumer<CapturePaymentConsumer>();
                x.AddConsumer<VoidPaymentConsumer>();

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
