using Microsoft.Extensions.DependencyInjection;

namespace MSG00.Translation.Application.Features.Font;

public static class ServiceExtension
{
    public static IServiceCollection AddFont(this IServiceCollection services)
    {
        services.AddSingleton(GameEncoding.Instance);
        services.AddSingleton<IFontService, FontService>();

        return services;
    }
}