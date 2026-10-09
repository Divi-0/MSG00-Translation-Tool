using System.Text;
using Microsoft.Extensions.DependencyInjection;
using MSG00.Translation.Application.Features.Csvb.CutIn;
using MSG00.Translation.Application.Features.Csvb.EvmBase;
using MSG00.Translation.Application.Features.Csvb.Mapi;
using MSG00.Translation.Application.Features.Csvb.Read;
using MSG00.Translation.Application.Features.Csvb.Table;
using MSG00.Translation.Application.Features.Graphics;
using MSG00.Translation.Application.Features.Rom;

namespace MSG00.Translation.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // all game texts are Shift-JIS; must be registered before the first Encoding.GetEncoding("Shift-JIS")
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        services.AddMediator();
        services.AddCsvbRead();
        services.AddMapi();
        services.AddEvmBase();
        services.AddTable();
        services.AddCutIn();
        services.AddGraphics();
        services.AddRom();

        return services;
    }
}