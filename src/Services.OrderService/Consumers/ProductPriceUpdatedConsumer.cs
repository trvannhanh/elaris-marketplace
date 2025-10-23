using MassTransit;
using BuildingBlocks.Contracts.Events;

namespace Services.OrderService.Consumers;

public class ProductPriceUpdatedConsumer : IConsumer<ProductPriceUpdatedEvent>
{
    private readonly ILogger<ProductPriceUpdatedConsumer> _logger;
    public ProductPriceUpdatedConsumer(ILogger<ProductPriceUpdatedConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<ProductPriceUpdatedEvent> context)
    {
        var m = context.Message;
        _logger.LogInformation("Received ProductPriceUpdatedEvent: ProductId={ProductId} old={Old} new={New}", m.ProductId, m.OldPrice, m.NewPrice);

        // TODO: update local read model / recalc order prices / notify users
        return Task.CompletedTask;
    }
}
