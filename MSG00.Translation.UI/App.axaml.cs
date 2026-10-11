using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MSG00.Translation.Application;
using MSG00.Translation.UI.ViewModels;
using MSG00.Translation.UI.Views;
using Serilog;
using Serilog.Events;
using System;

namespace MSG00.Translation.UI
{
    public partial class App : Avalonia.Application
    {
        // set in OnFrameworkInitializationCompleted, before any view model is created
        public static IServiceProvider Services { get; private set; } = null!;
        public IHost Host { get; set; } = null!;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            // If we're running inside the XAML designer / previewer, skip runtime-only
            // initialization (Host, DI, services, etc.). The Avalonia previewer sets
            // Design.IsDesignMode to true so we can detect that and return early.
            if (Avalonia.Controls.Design.IsDesignMode)
            {
                base.OnFrameworkInitializationCompleted();
                return;
            }

            IStorageProvider? storageProvider = null;

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
                .UseSerilog((_, configuration) => configuration
                    .MinimumLevel.Debug()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                    .Enrich.FromLogContext()
                    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"))
                .ConfigureServices((_, services) =>
                {
                    services.AddApplicationServices();

                    services.AddSingleton<IStorageProvider>(storageProvider!);

                    // the ROM browser; it creates the editors itself for the selected file
                    services.AddTransient<CsvbSelectViewModel>();
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