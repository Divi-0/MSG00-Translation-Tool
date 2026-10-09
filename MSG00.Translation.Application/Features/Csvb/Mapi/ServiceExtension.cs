using Microsoft.Extensions.DependencyInjection;
using MSG00.Translation.Application.Features.Csvb.Mapi.Conversation;
using MSG00.Translation.Application.Features.Csvb.Mapi.PrologueEpilogue;
using MSG00.Translation.Application.Features.Csvb.Mapi.StaffRoll;

namespace MSG00.Translation.Application.Features.Csvb.Mapi;

public static class ServiceExtension
{
    public static IServiceCollection AddMapi(this IServiceCollection services)
    {
        services.AddSingleton<IMapiService, MapiService>();
        services.AddMapiPrologueEpilogue();
        services.AddMapiConversation();
        services.AddMapiStaffRoll();

        return services;
    }
}