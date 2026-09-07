using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Resolvai.Application.Auth;
using Resolvai.Application.Auth.DTOs;
using Resolvai.Application.Auth.Validators;
using Resolvai.Application.Users;
using Resolvai.Application.Users.DTOs;
using Resolvai.Application.Users.Validators;
using Resolvai.Domain.Entities;

namespace Resolvai.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();

        services.AddScoped<IValidator<LoginRequest>, LoginRequestValidator>();
        services.AddScoped<IValidator<RegisterRequest>, RegisterRequestValidator>();
        services.AddScoped<IValidator<CreateUserRequest>, CreateUserRequestValidator>();
        services.AddScoped<IValidator<User>, UserValidator>();

        return services;
    }
}
