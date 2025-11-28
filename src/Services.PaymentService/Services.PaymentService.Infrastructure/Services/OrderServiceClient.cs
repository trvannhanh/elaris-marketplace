
using Services.PaymentService.Application.Interfaces;
using System.Net.Http.Json;

namespace Services.PaymentService.Infrastructure.Services
{
    public class OrderServiceClient : IOrderServiceClient
    {
        private readonly HttpClient _client;

        public OrderServiceClient(HttpClient client)
        {
            _client = client;
        }

        public Task<OrderDto?> GetOrderAsync(Guid orderId, CancellationToken ct = default)
        {
            try
            {
                return _client.GetFromJsonAsync<OrderDto>(
                    $"/api/order/{orderId}", ct);
            }
            catch
            {
                return null;
            }
        }
    }
}
