using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.InventoryService.Application.Inventory.Queries.CheckProductsAvailability
{
    public record CheckProductsAvailabilityQuery(string ProductId, int Quantity) : IRequest<CheckAvailabilityResponse>;


    public class CheckAvailabilityResponse
    {
        public bool InStock { get; set; }
        public int AvailableStock { get; set; }
        public string? Message { get; set; }
    }

}


