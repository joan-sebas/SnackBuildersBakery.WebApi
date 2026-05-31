using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IDomainEventDispatcher, InMemoryDomainEventDispatcher>();

        services.AddScoped<CreateMenuItemUseCase>();
        services.AddScoped<GetMenuItemUseCase>();
        services.AddScoped<UpdateMenuItemUseCase>();
        services.AddScoped<RemoveMenuItemUseCase>();
        services.AddScoped<ListMenuItemsUseCase>();

        services.AddScoped<PlaceOrderUseCase>();
        services.AddScoped<PayOrderUseCase>();

        services.AddScoped<TrackOrderQuery>();
        services.AddScoped<KitchenMonitoringQuery>();

        return services;
    }
}
