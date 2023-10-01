using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MSG00.Translation.Infrastructure;
using MSG00.Translation.UI.ViewModels;
using MSG00.Translation.UI.Views;
using System;

namespace MSG00.Translation.UI
{
    public partial class App : Application
    {
        public static IServiceProvider Services { get; private set; }
        public IHost Host { get; set; }

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            IStorageProvider storageProvider = null;

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow();

                storageProvider = desktop.MainWindow.StorageProvider;
            }
            else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
            {
                singleViewPlatform.MainView = new MainView();

                storageProvider = TopLevel.GetTopLevel(singleViewPlatform.MainView)!.StorageProvider;
            }

            Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
                .ConfigureServices((_, services) =>
                {
                    //infrastructure project injection
                    services.AddInfrastructureServices();

                    services.AddSingleton<IStorageProvider>(storageProvider!);

                    //viewModels
                    services.AddTransient<WelcomeViewModel>();
                    services.AddTransient<ConversationCsvbViewModel>();
                    services.AddTransient<PrologueCsvbViewModel>();
                    services.AddTransient<EpilogueCsvbViewModel>();
                    services.AddTransient<EtcCsvbViewModel>();
                    services.AddTransient<RequirementCsvbViewModel>();
                    services.AddTransient<StaffRollCsvbViewModel>();
                    services.AddTransient<EvmCsvbViewModel>();
                })
                .Build();

            Services = Host.Services;

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop1)
            {
                desktop1.MainWindow = new MainWindow()
                {
                    DataContext = new MainViewModel()
                };

                storageProvider = desktop1.MainWindow.StorageProvider;
            }
            else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
            {
                singleViewPlatform.MainView = new MainView
                {
                    DataContext = new MainViewModel()
                };

                storageProvider = TopLevel.GetTopLevel(singleViewPlatform.MainView)!.StorageProvider;
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}