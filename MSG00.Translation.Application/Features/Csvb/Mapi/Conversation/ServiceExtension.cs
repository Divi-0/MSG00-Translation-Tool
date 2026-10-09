using Microsoft.Extensions.DependencyInjection;

namespace MSG00.Translation.Application.Features.Csvb.Mapi.Conversation;

public static class ServiceExtension
{
    public static IServiceCollection AddMapiConversation(this IServiceCollection services)
    {
        services.AddSingleton<IReader, Reader>();
        services.AddSingleton<IWriter, Writer>();

        return services;
    }
}