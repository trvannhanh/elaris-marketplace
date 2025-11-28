using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore;
using Services.PaymentService.Infrastructure.Persistence;

namespace Services.PaymentService.API
{
    public class PaymentDbContextFactory : IDesignTimeDbContextFactory<PaymentDbContext>
    {
        public PaymentDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<PaymentDbContext>();
            optionsBuilder.UseNpgsql(
                "Host=localhost;Port=5433;Database=elaris_paymentdb;Username=elaris;Password=elaris_pwd",
                o => o.EnableRetryOnFailure()
            );
            return new PaymentDbContext(optionsBuilder.Options);
        }
    }
}
