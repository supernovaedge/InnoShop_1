using FluentValidation;
using UserManagement.Application.DTOs;

namespace UserManagement.Application.Validators
{
    public class UserDtoValidator : AbstractValidator<UserDto>
    {
        public UserDtoValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Id is required.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .Length(2, 50).WithMessage("Name must be between 2 and 50 characters.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Invalid email format.");

            RuleFor(x => x.Role)
                .NotEmpty().WithMessage("Role is required.");

            RuleFor(x => x.IsActive)
                .NotNull().WithMessage("IsActive status is required.");

            RuleFor(x => x.CreatedAt)
                .NotEmpty().WithMessage("CreatedAt is required.");

            RuleFor(x => x.UpdatedAt)
                .GreaterThanOrEqualTo(x => x.CreatedAt).When(x => x.UpdatedAt.HasValue)
                .WithMessage("UpdatedAt must be greater than or equal to CreatedAt.");
        }
    }
}
