namespace Resolvai.Application.Common.Exceptions;

/// <summary>
/// A conta externa pode existir, mas a gravação do perfil não foi confirmada.
/// Uma nova tentativa deve comprovar a posse da conta antes de retomá-la.
/// </summary>
public sealed class RegistrationPendingException : Exception
{
    public RegistrationPendingException()
        : base("Não foi possível concluir o cadastro. Confirme seu e-mail, caso necessário, e tente novamente com as mesmas credenciais.")
    {
    }
}
