using FluentValidation;
using Resolvai.Application.Common.Validation;
using Resolvai.Application.Users.DTOs;

namespace Resolvai.Application.Users.Validators;

public sealed class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        RuleFor(request => request.Name).ValidName();
        RuleFor(request => request.Email).ValidEmail();
        RuleFor(request => request.Password).ValidNewPassword();
        RuleFor(request => request.Role).IsInEnum().WithMessage("O papel do usuário é inválido.");
    }
}
