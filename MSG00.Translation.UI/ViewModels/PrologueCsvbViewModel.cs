using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MSG00.Translation.Application.Features.Csvb.Mapi.PrologueEpilogue;
using MSG00.Translation.Application.Features.Csvb;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading.Tasks;
using System.Windows.Input;
using MSG00.Translation.Application.Features.Rom;
using MSG00.Translation.UI.Models;

namespace MSG00.Translation.UI.ViewModels
{
    public class PrologueCsvbViewModel : CsvbViewModelBase
    {
        private readonly IWriter _writer;
        private readonly IRomService _romService;
        private readonly string _romPath;

        public PrologueCsvbViewModel(PrologueCsvb csvbFile, IStorageProvider storageProvider, IWriter writer, IRomService romService, string romPath) : base(storageProvider)
        {
            _writer = writer;
            _romService = romService;
            _romPath = romPath;
            PrologueFile = csvbFile;
            IsFileLoaded = true;
        }

        public string RomPath => _romPath;
        

        /// <summary>
        /// Writes the csvb into the ROM kept in memory; "Save ROM as" writes the new .nds.
        /// </summary>
        protected override async Task SaveAsync()
        {
            using var stream = new MemoryStream();
            await _writer.WriteFileAsync(stream, PrologueFile!);
            _romService.ReplaceFile(_romPath, stream.ToArray());

            await MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
            {
                ContentTitle = "Applied",
                ContentMessage = $"{_romPath} was updated in the opened ROM.\nUse \"Save ROM as\" to write a new .nds file.",
                ButtonDefinitions = ButtonEnum.Ok,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            }).ShowAsync();
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
        
            var json = JsonSerializer.Serialize(TextPointers, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
            });
        
            await using var fileStream = await selectedFile.OpenWriteAsync().ConfigureAwait(false);
            await using var streamWriter = new StreamWriter(fileStream);
            await streamWriter.WriteAsync(json);
        }
        
        public new ICommand ExportCommand => new AsyncRelayCommand(ExportAsync);
        
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
            var importedTexts = JsonSerializer.Deserialize<ProloguePointerText[]>(fileStream);

            // foreach (var text in importedTexts!)
            // {
            //     var validationResult = await new TextHolderValidator().ValidateAsync(text);
            //
            //     if (!validationResult.IsValid)
            //     {
            //         Dispatcher.UIThread.Post(async void () =>
            //         {
            //             var dialog = MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
            //             {
            //                 ContentTitle = "Error",
            //                 ContentMessage = JsonSerializer.Serialize(validationResult.Errors),
            //                 ButtonDefinitions = ButtonEnum.Ok,
            //                 WindowStartupLocation = WindowStartupLocation.CenterOwner
            //             });
            //
            //             await dialog.ShowAsync().ConfigureAwait(false);
            //         });
            //     }
            // }
        
            // apply the imported texts in order onto the loaded pointers, so the entries and durations stay attached
            foreach (var (entry, imported) in Entries.Zip(importedTexts!))
            {
                entry.Text = imported.TextLines;
            }
        }
        
        public new ICommand ImportCommand => new AsyncRelayCommand(ImportAsync);
        
        private PrologueCsvb? _prologueFile;
        public PrologueCsvb? PrologueFile
        {
            get => _prologueFile;
            set
            {
                SetProperty(ref _prologueFile, value);
                TextPointers = new ObservableCollection<ProloguePointerText>(_prologueFile!.ProEpiloguePointers.Where(x => x.Type == ProloguePointerType.Text).Cast<ProloguePointerText>().ToList());
                Entries = new ObservableCollection<PrologueTextEntry>(PrologueTextEntry.CreateEntries(_prologueFile.ProEpiloguePointers));
            }
        }

        private ObservableCollection<PrologueTextEntry> _entries = new();
        public ObservableCollection<PrologueTextEntry> Entries
        {
            get => _entries;
            set => SetProperty(ref _entries, value);
        }

        private ObservableCollection<ProloguePointerText> _textPointer = new();
        public ObservableCollection<ProloguePointerText> TextPointers
        {
            get => _textPointer;
            set => SetProperty(ref _textPointer, value);
        }

        private bool _isReadingFile;
        public bool IsReadingFile
        {
            get => _isReadingFile;
            set => SetProperty(ref _isReadingFile, value);
        }

        private bool _isFileLoaded;
        public bool IsFileLoaded
        {
            get => _isFileLoaded;
            set => SetProperty(ref _isFileLoaded, value);
        }

        private bool _isSaving;
        public bool IsSaving
        {
            get => _isSaving;
            set => SetProperty(ref _isSaving, value);
        }
    }
}
