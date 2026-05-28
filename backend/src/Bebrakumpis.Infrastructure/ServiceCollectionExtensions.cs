using Bebrakumpis.Application.Interfaces;
using Bebrakumpis.Domain.Interfaces;
using Bebrakumpis.Infrastructure.Persistence;
using Bebrakumpis.Infrastructure.Persistence.Repositories;
using Bebrakumpis.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Bebrakumpis.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IDbConnectionFactory, SqlConnectionFactory>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IHouseRepository, HouseRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        return services;
    }
}
