using Resolvai.Domain.Common;
using Resolvai.Domain.Enums;
using Resolvai.Domain.Exceptions;
using Resolvai.Domain.ValueObjects;

namespace Resolvai.Domain.Entities;

public sealed class User : Entity<Guid>, IAggregateRoot
{
    private User(
        Guid id,
        string name,
        Email email,
        UserRole role,
        bool isActive,
        DateTimeOffset createdAt,
        DateTimeOffset? updatedAt)
        : base(id)
    {
        Name = name;
        Email = email;
        Role = role;
        IsActive = isActive;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public string Name { get; private set; }

    public Email Email { get; private set; }

    public UserRole Role { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    /// <summary>
    /// Cria o perfil local vinculado a um usuário já autenticado no Supabase Auth.
    /// O <paramref name="id"/> deve ser o mesmo UUID de auth.users.
    /// </summary>
    public static User Register(Guid id, string name, Email email, UserRole role)
    {
        ArgumentNullException.ThrowIfNull(email);

        if (id == Guid.Empty)
        {
            throw new DomainException("O identificador do usuário é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("O nome do usuário é obrigatório.");
        }

        return new User(
            id,
            name.Trim(),
            email,
            role,
            isActive: true,
            createdAt: DateTimeOffset.UtcNow,
            updatedAt: null);
    }

    /// <summary>
    /// Reidrata um usuário já persistido. Uso exclusivo da camada de persistência.
    /// </summary>
    public static User Restore(
        Guid id,
        string name,
        Email email,
        UserRole role,
        bool isActive,
        DateTimeOffset createdAt,
        DateTimeOffset? updatedAt)
        => new(id, name, email, role, isActive, createdAt, updatedAt);

    public void ChangeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("O nome do usuário é obrigatório.");
        }

        Name = name.Trim();
        Touch();
    }

    public void ChangeRole(UserRole role)
    {
        Role = role;
        Touch();
    }

    public void Activate()
    {
        if (IsActive)
        {
            return;
        }

        IsActive = true;
        Touch();
    }

    public void Deactivate()
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        Touch();
    }

    private void Touch() => UpdatedAt = DateTimeOffset.UtcNow;
}
