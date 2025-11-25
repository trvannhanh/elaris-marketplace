using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.OrderService.Application.Orders.Commands.CancelOrder
{
    /// <summary>
    /// Command hủy order (chỉ khi Pending)
    /// User chỉ hủy được order của mình
    /// Admin hủy được mọi order
    /// </summary>
    public record CancelOrderCommand(
        Guid OrderId,
        string UserId,      // From token
        string? Reason
    ) : IRequest<bool>;
}
