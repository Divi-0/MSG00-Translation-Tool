using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MSG00.Translation.UI.Models;

namespace MSG00.Translation.UI.ViewModels;

public abstract class CsvbViewModelBase : ViewModelBase
{
    protected readonly IStorageProvider StorageProvider;

    public CsvbViewModelBase(IStorageProvider storageProvider)
    {
        StorageProvider = storageProvider;
    }
    
    private async Task ExportAsync()
    {
        var selectedFile = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Select where to save file",
            SuggestedFileType = new FilePickerFileType("json")
            {
                Patterns = new List<string>()
                {
                    "*.json"
                }
            },
            SuggestedFileName = "csvb.json"
        });

        if (selectedFile == null)
        {
            return;
        }
        
        var json = JsonSerializer.Serialize(Texts, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
        });
        
        await using var fileStream = await selectedFile.OpenWriteAsync().ConfigureAwait(false);
        await using var streamWriter = new StreamWriter(fileStream);
        await streamWriter.WriteAsync(json);
    }

    private async Task ImportAsync()
    {
        var selectFileList = await StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                AllowMultiple = false,
                Title = "Select a json file to import",
                FileTypeFilter = new List<FilePickerFileType>()
                {
                    new FilePickerFileType("json")
                    {
                        Patterns = new List<string>()
                        {
                            "*.json"
                        }
                    }
                }
            }).ConfigureAwait(false);

        if (!selectFileList.Any())
        {
            return;
        }

        await using var fileStream = await selectFileList[0].OpenReadAsync().ConfigureAwait(false);
        var importedTexts = JsonSerializer.Deserialize<TextHolder[]>(fileStream);

        foreach (var text in importedTexts!)
        {
            var validationResult = await new TextHolderValidator().ValidateAsync(text);

            if (!validationResult.IsValid)
            {
                Dispatcher.UIThread.Post(async void () =>
                {
                    var dialog = MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
                    {
                        ContentTitle = "Error",
                        ContentMessage = JsonSerializer.Serialize(validationResult.Errors),
                        ButtonDefinitions = ButtonEnum.Ok,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    });

                    await dialog.ShowAsync().ConfigureAwait(false);
                });
            }
        }
        
        Texts = new ObservableCollection<TextHolder>(importedTexts!);
    }

    protected abstract Task SaveAsync();
    
    private ObservableCollection<TextHolder> _texts = new();
    public ObservableCollection<TextHolder> Texts
    {
        get => _texts;
        set => SetProperty(ref _texts, value);
    }
    
    public ICommand ExportCommand => new AsyncRelayCommand(ExportAsync);
    public ICommand ImportCommand => new AsyncRelayCommand(ImportAsync);
    public ICommand SaveCommand => new AsyncRelayCommand(SaveAsync);
}