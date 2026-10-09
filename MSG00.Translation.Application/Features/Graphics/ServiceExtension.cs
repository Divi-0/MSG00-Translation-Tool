using Microsoft.Extensions.DependencyInjection;

namespace MSG00.Translation.Application.Features.Graphics;

public static class ServiceExtension
{
    public static IServiceCollection AddGraphics(this IServiceCollection services)
    {
        services.AddSingleton<IReader, Reader>();
        services.AddSingleton<IWriter, Writer>();

        return services;
    }
}