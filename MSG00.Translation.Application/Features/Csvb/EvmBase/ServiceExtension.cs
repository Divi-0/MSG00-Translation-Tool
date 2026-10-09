using Microsoft.Extensions.DependencyInjection;
using MSG00.Translation.Application.Features.Csvb.EvmBase.Read;

namespace MSG00.Translation.Application.Features.Csvb.EvmBase;

public static class ServiceExtension
{
    public static IServiceCollection AddEvmBase(this IServiceCollection services)
    {
        services.AddSingleton<IEvmBaseReader, EvmBaseReader>();

        return services;
    }
}