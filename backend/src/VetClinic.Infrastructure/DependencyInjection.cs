using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VetClinic.Application.Interfaces;
using VetClinic.Infrastructure.Data;
using VetClinic.Infrastructure.Repositories;
using VetClinic.Infrastructure.Storage;

namespace VetClinic.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<VetClinicDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<VetClinicDbContext>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IFileStorage, LocalFileStorage>();

        return services;
    }
}