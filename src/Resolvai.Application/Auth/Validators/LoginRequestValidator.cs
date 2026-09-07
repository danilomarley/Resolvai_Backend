using FluentValidation;
using Resolvai.Application.Auth.DTOs;
using Resolvai.Application.Common.Validation;

namespace Resolvai.Application.Auth.Validators;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        RuleFor(request => request.Email).ValidEmail();
        // Login não impõe o tamanho mínimo exigido na criação de novas senhas.
        RuleFor(request => request.Password)
            .NotEmpty()
            .WithMessage("A senha é obrigatória.")
            .MaximumLength(128)
            .WithMessage("A senha deve ter no máximo 128 caracteres.");
    }
}
