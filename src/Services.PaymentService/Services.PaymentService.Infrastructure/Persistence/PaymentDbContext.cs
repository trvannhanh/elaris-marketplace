
using Microsoft.EntityFrameworkCore;
using Services.PaymentService.Domain.Entities;


namespace Services.PaymentService.Infrastructure.Persistence
{
    public class PaymentDbContext : DbContext
    {
        public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options) { }

        public DbSet<Payment> Payments => Set<Payment>();
    }
}
