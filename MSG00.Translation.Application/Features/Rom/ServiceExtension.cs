using Microsoft.Extensions.DependencyInjection;

namespace MSG00.Translation.Application.Features.Rom;

public static class ServiceExtension
{
    public static IServiceCollection AddRom(this IServiceCollection services)
    {
        services.AddSingleton<IRomService, RomService>();

        return services;
    }
}