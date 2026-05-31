using Application;
using Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("SnackBuildersDb")));

        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IMenuRepository, MenuRepository>();

        services.Configure<SchedulerOptions>(configuration.GetSection("Scheduler"));
        services.AddSingleton<ISchedulerConfigProvider, OptionsSchedulerConfigProvider>();
        services.AddSingleton<IAgingPolicy, LinearAgingPolicy>();
        services.AddSingleton<KitchenScheduler>();
        services.AddSingleton<ISchedulerCoordinator, SchedulerCoordinator>();
        services.AddSingleton(TimeProvider.System);

        services.AddTransient<SchedulerReconstructionService>();

        services.Configure<PaymentGatewayOptions>(configuration.GetSection("PaymentGateway"));
        services.AddScoped<MockPaymentGateway>();
        services.AddScoped<IdempotencyStore>();
        services.AddScoped<IPaymentGateway, IdempotentPaymentGateway>();

        return services;
    }
}
