

using Services.OrderService.Application.Interfaces;
using Services.OrderService.Application.Orders.DTOs;
using System.Net.Http.Json;

namespace Services.OrderService.Infrastructure.Services
{
    public class CatalogServiceClient : ICatalogServiceClient
    {
        private readonly HttpClient _client;

        public CatalogServiceClient(HttpClient client)
        {
            _client = client;
        }

        public async Task<ProductDto?> GetProductAsync(string productId, CancellationToken ct = default)
        {
            try
            {
                return await _client.GetFromJsonAsync<ProductDto>(
                    $"/api/products/{productId}", ct);
            }
            catch
            {
                return null;
            }
        }
    }
}
