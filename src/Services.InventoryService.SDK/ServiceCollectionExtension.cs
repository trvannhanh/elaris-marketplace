

namespace Services.InventoryService.SDK
{
    public static class ServiceCollectionExtension
    {
        public static void AddGrpcSdk(this IServiceProvider services
        {
            services.AddGrpcClient<>(client =>
            {
                client.Address = new Uri("");
            });
        }
    }
}
