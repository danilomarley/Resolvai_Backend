using FluentValidation;
using Resolvai.Application.Auth.DTOs;
using Resolvai.Application.Common.Validation;

namespace Resolvai.Application.Auth.Validators;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        RuleFor(request => request.Name).ValidName();
        RuleFor(request => request.Email).ValidEmail();
        RuleFor(request => request.Password).ValidNewPassword();
    }
}
