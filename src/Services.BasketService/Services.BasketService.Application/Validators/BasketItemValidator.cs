using FluentValidation;
using Services.BasketService.Application.Models;

namespace Services.BasketService.Application.Validators
{
    public class BasketItemValidator : AbstractValidator<BasketItem>
    {
        public BasketItemValidator()
        {
            RuleFor(x => x.ProductId)
                .NotEmpty().WithMessage("ProductId is required");

            RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("Quantity must be greater than 0");

            RuleFor(x => x.Price)
                .GreaterThanOrEqualTo(0).WithMessage("Price cannot be negative");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Product name is required")
                .MaximumLength(200).WithMessage("Name too long");

            RuleFor(x => x.ImageUrl)
                .NotEmpty().WithMessage("ImageUrl is required")
                .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
                .WithMessage("ImageUrl must be a valid URL");
        }
    }
}
