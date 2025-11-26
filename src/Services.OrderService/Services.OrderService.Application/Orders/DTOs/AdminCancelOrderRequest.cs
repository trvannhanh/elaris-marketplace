using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.OrderService.Application.Orders.DTOs
{
    public class AdminCancelOrderRequest
    {
        public string Reason { get; set; } = string.Empty;
    }
}
