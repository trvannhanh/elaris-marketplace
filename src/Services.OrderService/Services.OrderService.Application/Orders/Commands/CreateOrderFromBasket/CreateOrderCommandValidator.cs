using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.OrderService.Application.Orders.Commands.CreateOrderFromBasket
{
    public class CreateOrderCommandValidator : AbstractValidator<CreateOrderFromBasketCommand>
    {
        public CreateOrderCommandValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.TotalPrice).GreaterThan(0);
        }
    }
}
