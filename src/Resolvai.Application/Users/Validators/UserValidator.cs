using FluentValidation;
using Resolvai.Application.Common.Validation;
using Resolvai.Domain.Entities;

namespace Resolvai.Application.Users.Validators;

/// <summary>Valida o perfil antes de gravá-lo, incluindo o identificador recebido do Supabase.</summary>
public sealed class UserValidator : AbstractValidator<User>
{
    public UserValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        RuleFor(user => user.Id)
            .NotEmpty()
            .WithMessage("O identificador do usuário é obrigatório.");
        RuleFor(user => user.Name).ValidName();
        RuleFor(user => user.Email).NotNull().WithMessage("O e-mail é obrigatório.");
        When(
            user => user.Email is not null,
            () =>
            {
                RuleFor(user => user.Email.Value).ValidEmail().OverridePropertyName("Email");
            }
        );
        RuleFor(user => user.Role).IsInEnum().WithMessage("O papel do usuário é inválido.");
    }
}
