using Microsoft.Extensions.DependencyInjection;
using Resolvai.Application.Services;
using Resolvai.Application.Services.Interfaces;

namespace Resolvai.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IOrderService, OrderService>();

        return services;
    }
}
