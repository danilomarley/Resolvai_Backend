using System.Net.Mail;
using FluentValidation;

namespace Resolvai.Application.Common.Validation;

/// <summary>Regras compartilhadas entre cadastro público, criação administrativa e perfil.</summary>
public static class UserValidationRules
{
    public static IRuleBuilderOptions<T, string> ValidName<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty()
            .WithMessage("O nome é obrigatório.")
            .MaximumLength(200)
            .WithMessage("O nome deve ter no máximo 200 caracteres.");

    public static IRuleBuilderOptions<T, string> ValidEmail<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty()
            .WithMessage("O e-mail é obrigatório.")
            .MaximumLength(320)
            .WithMessage("O e-mail deve ter no máximo 320 caracteres.")
            .Must(IsValidEmail)
            .WithMessage("Informe um e-mail válido.");

    public static IRuleBuilderOptions<T, string> ValidNewPassword<T>(
        this IRuleBuilder<T, string> rule
    ) =>
        rule.NotEmpty()
            .WithMessage("A senha é obrigatória.")
            .MinimumLength(8)
            .WithMessage("A senha deve ter no mínimo 8 caracteres.")
            .MaximumLength(128)
            .WithMessage("A senha deve ter no máximo 128 caracteres.");

    private static bool IsValidEmail(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.Trim().ToLowerInvariant();
        return MailAddress.TryCreate(normalized, out var address) && address.Address == normalized;
    }
}
