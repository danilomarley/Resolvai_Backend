using FluentValidation.TestHelper;
using Resolvai.Application.Auth.DTOs;
using Resolvai.Application.Auth.Validators;
using Resolvai.Application.Users.DTOs;
using Resolvai.Application.Users.Validators;
using Resolvai.Domain.Entities;
using Resolvai.Domain.Enums;
using Resolvai.Domain.ValueObjects;

namespace Resolvai.Api.Tests;

public class ValidationTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-email")]
    [InlineData("Maria <maria@example.com>")]
    public void Registration_rejects_invalid_email(string? email)
    {
        var request = new RegisterRequest
        {
            Name = "Maria",
            Email = email!,
            Password = "Senha123",
        };
        new RegisterRequestValidator()
            .TestValidate(request)
            .ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Registration_rejects_missing_name_and_password(string? value)
    {
        var request = new RegisterRequest
        {
            Name = value!,
            Email = "maria@example.com",
            Password = value!,
        };
        var result = new RegisterRequestValidator().TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Name);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Theory]
    [InlineData(7, false)]
    [InlineData(8, true)]
    [InlineData(128, true)]
    [InlineData(129, false)]
    public void Registration_enforces_password_boundaries(int length, bool expected)
    {
        var request = new RegisterRequest
        {
            Name = "Maria",
            Email = "maria@example.com",
            Password = new string('x', length),
        };
        Assert.Equal(expected, new RegisterRequestValidator().Validate(request).IsValid);
    }

    [Fact]
    public void Registration_enforces_name_and_email_maximum_lengths()
    {
        var request = new RegisterRequest
        {
            Name = new string('a', 201),
            Email = new string('a', 309) + "@example.com",
            Password = "Senha123",
        };
        var result = new RegisterRequestValidator().TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Name);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Login_accepts_short_existing_password_but_rejects_overlong_password()
    {
        var request = new LoginRequest { Email = "maria@example.com", Password = "abc" };
        var validator = new LoginRequestValidator();
        Assert.True(validator.Validate(request).IsValid);
        validator
            .TestValidate(request with { Password = new string('x', 129) })
            .ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Administrative_creation_rejects_unknown_role()
    {
        var request = new CreateUserRequest
        {
            Name = "Maria",
            Email = "maria@example.com",
            Password = "Senha123",
            Role = (UserRole)999,
        };
        new CreateUserRequestValidator()
            .TestValidate(request)
            .ShouldHaveValidationErrorFor(x => x.Role);
    }

    [Fact]
    public void Profile_validation_rejects_invalid_identity_and_fields()
    {
        var user = User.Restore(
            Guid.Empty,
            " ",
            null!,
            (UserRole)999,
            true,
            DateTimeOffset.UtcNow,
            null
        );
        var result = new UserValidator().TestValidate(user);
        result.ShouldHaveValidationErrorFor(x => x.Id);
        result.ShouldHaveValidationErrorFor(x => x.Name);
        result.ShouldHaveValidationErrorFor(x => x.Email);
        result.ShouldHaveValidationErrorFor(x => x.Role);
    }

    [Fact]
    public void Email_normalization_is_preserved()
    {
        Assert.Equal("maria@example.com", Email.Create("  MARIA@EXAMPLE.COM  ").Value);
    }
}
