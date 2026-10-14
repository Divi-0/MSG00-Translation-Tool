using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using Mediator;
using Microsoft.Extensions.Logging;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MSG00.Translation.Application.Features.Arm9;
using MSG00.Translation.Application.Features.Csvb.Mapi.PrologueEpilogue;
using MSG00.Translation.Application.Features.Csvb.EvmBase.Write;
using MSG00.Translation.Application.Features.Csvb.Read;
using MSG00.Translation.Application.Features.Font;
using MSG00.Translation.Application.Features.Rom;
using MSG00.Translation.Application.Features.Csvb.CutIn;
using MSG00.Translation.Application.Features.Csvb.EvmBase;
using MSG00.Translation.Application.Features.Csvb.Table;
using MSG00.Translation.Application.Features.Csvb.Mapi.Conversation;
using MSG00.Translation.UI.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ConversationEquivalence = MSG00.Translation.Application.Features.Csvb.Mapi.Conversation.Equivalence;
using ConversationWriter = MSG00.Translation.Application.Features.Csvb.Mapi.Conversation.IWriter;
using IWriter = MSG00.Translation.Application.Features.Csvb.Mapi.PrologueEpilogue.IWriter;
using TableWriter = MSG00.Translation.Application.Features.Csvb.Table.IWriter;
using CutInWriter = MSG00.Translation.Application.Features.Csvb.CutIn.IWriter;
using StaffRollWriter = MSG00.Translation.Application.Features.Csvb.Mapi.StaffRoll.IWriter;
using MSG00.Translation.Application.Features.Csvb.Mapi.StaffRoll;
using GraphicsReader = MSG00.Translation.Application.Features.Graphics.IReader;
using GraphicsWriter = MSG00.Translation.Application.Features.Graphics.IWriter;
using ICommand = System.Windows.Input.ICommand;

namespace MSG00.Translation.UI.ViewModels
{
    /// <summary>
    /// Opens an .nds ROM, lists its csvb files by category and opens the matching editor.
    /// Changes are kept in memory and only written with "Save ROM as" to a new file.
    /// </summary>
    public class CsvbSelectViewModel : ViewModelBase
    {
        private static readonly FilePickerFileType NdsFileType = new("Nintendo DS ROM") { Patterns = new List<string> { "*.nds" } };

        /// <summary>texts inside the ARM9 program instead of a csvb, e.g. the in-game menu</summary>
        private const string ProgramTextsCategory = "Program texts";

        /// <summary>extra characters of the game font, kept in every copy of LC_font_12.NFTR</summary>
        private const string FontCategory = "Font";

        private readonly IMediator _mediator;
        private readonly IStorageProvider _storageProvider;
        private readonly IWriter _writer;
        private readonly ConversationWriter _conversationWriter;
        private readonly TableWriter _tableWriter;
        private readonly CutInWriter _cutInWriter;
        private readonly StaffRollWriter _staffRollWriter;
        private readonly GraphicsReader _graphicsReader;
        private readonly GraphicsWriter _graphicsWriter;
        private readonly IRomService _romService;
        private readonly IFontService _fontService;
        private readonly ILogger<CsvbSelectViewModel> _logger;
        private Uri? _openedRomUri;

        public CsvbSelectViewModel(IMediator mediator, IStorageProvider storageProvider, IWriter writer, ConversationWriter conversationWriter, TableWriter tableWriter, CutInWriter cutInWriter, StaffRollWriter staffRollWriter, GraphicsReader graphicsReader, GraphicsWriter graphicsWriter, IRomService romService, IFontService fontService, ILogger<CsvbSelectViewModel> logger)
        {
            _fontService = fontService;
            _staffRollWriter = staffRollWriter;
            _graphicsReader = graphicsReader;
            _graphicsWriter = graphicsWriter;
            _logger = logger;
            _mediator = mediator;
            _storageProvider = storageProvider;
            _writer = writer;
            _conversationWriter = conversationWriter;
            _tableWriter = tableWriter;
            _cutInWriter = cutInWriter;
            _romService = romService;
            _romService.Changed += (_, _) =>
            {
                RaisePropertyChanged(nameof(IsRomOpen));
                RaisePropertyChanged(nameof(RomTitle));
            };
        }

        public bool IsRomOpen => _romService.IsOpen;

        public string RomTitle => _romService.IsOpen
            ? $"{_romService.FileName}{(_romService.HasChanges ? " (unsaved changes)" : string.Empty)}"
            : "No ROM opened";

        private ObservableCollection<RomCategory> _categories = new();
        public ObservableCollection<RomCategory> Categories
        {
            get => _categories;
            set => SetProperty(ref _categories, value);
        }

        private object? _selectedItem;
        public object? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (SetProperty(ref _selectedItem, value) && value is RomFile file)
                {
                    _ = OpenCsvbAsync(file);
                }
            }
        }

        private static readonly TimeSpan LoadingIndicatorDelay = TimeSpan.FromMilliseconds(100);

        /// <summary>counts file selections, so a slow load finishing after a newer selection is dropped</summary>
        private int _navigation;

        /// <summary>the selection still loading, 0 when none</summary>
        private int _loadingNavigation;

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            private set => SetProperty(ref _isLoading, value);
        }

        private ViewModelBase? _currentView;
        public ViewModelBase? CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }

        public ICommand OpenRomCommand => new AsyncRelayCommand(OpenRomAsync);
        public ICommand SaveRomAsCommand => new AsyncRelayCommand(SaveRomAsAsync);

        private async Task OpenRomAsync()
        {
            if (_romService.HasChanges && !await ConfirmAsync("Open ROM", "The current ROM has changes that were not saved to a new ROM. Discard them?"))
            {
                return;
            }

            var files = await _storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                AllowMultiple = false,
                Title = "Select a Nintendo DS ROM",
                FileTypeFilter = new List<FilePickerFileType> { NdsFileType }
            });

            if (!files.Any())
            {
                return;
            }

            try
            {
                await using (var stream = await files[0].OpenReadAsync())
                {
                    await _romService.OpenAsync(stream, files[0].Name);
                }

                // texts are read with the extra characters of this ROM's font (none in the original ROM)
                await Task.Run(_fontService.LoadCharacterTable);

                _openedRomUri = files[0].Path;
                CurrentView = null;
                Categories = new ObservableCollection<RomCategory>(_romService.CsvbFiles
                    .GroupBy(x => x.Category)
                    .OrderBy(x => CsvbCategories.Names.ToList().IndexOf(x.Key))
                    .Select(x => new RomCategory(x.Key, x.OrderBy(file => file.Path, StringComparer.OrdinalIgnoreCase).ToList()))
                    .Prepend(new RomCategory(ProgramTextsCategory, Arm9Texts.Sections.Select(x => new RomFile(RomPaths.Arm9, x, ProgramTextsCategory, 0)).ToList()))
                    .Prepend(new RomCategory(FontCategory, [new RomFile("LC_font_12.NFTR", "Extra characters", FontCategory, 0)])));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Could not open ROM {FileName}", files[0].Name);
                await ShowMessageAsync("Error", $"Could not open {files[0].Name}:\n{e.Message}");
            }
        }

        private async Task SaveRomAsAsync()
        {
            if (!_romService.IsOpen)
            {
                return;
            }

            var target = await _storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save new ROM",
                DefaultExtension = "nds",
                ShowOverwritePrompt = true,
                SuggestedFileName = $"{Path.GetFileNameWithoutExtension(_romService.FileName)}_translated.nds",
                FileTypeChoices = new List<FilePickerFileType> { NdsFileType }
            });

            if (target == null)
            {
                return;
            }

            if (_openedRomUri != null && Uri.Compare(target.Path, _openedRomUri, UriComponents.AbsoluteUri, UriFormat.Unescaped, StringComparison.OrdinalIgnoreCase) == 0)
            {
                _logger.LogWarning("Refused to overwrite the opened ROM {Path}", target.Path);
                await ShowMessageAsync("Save ROM as", "The opened ROM is never overwritten. Please choose a different file.");
                return;
            }

            try
            {
                await using (var stream = await target.OpenWriteAsync())
                {
                    stream.SetLength(0);
                    await _romService.SaveAsAsync(stream);
                }

                await ShowMessageAsync("Save ROM as", $"Saved {target.Name}.");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Could not save ROM {Path}", target.Path);
                await ShowMessageAsync("Error", $"Could not save {target.Name}:\n{e.Message}");
            }
        }

        /// <summary>
        /// Loads the editor off the UI thread; a loading indicator appears when it takes longer than
        /// <see cref="LoadingIndicatorDelay"/>. When another file is selected meanwhile, the older result is dropped.
        /// </summary>
        private async Task OpenCsvbAsync(RomFile file)
        {
            var navigation = ++_navigation;
            _ = ShowLoadingIndicatorAsync(navigation);
            try
            {
                var view = await Task.Run(() => LoadEditorAsync(file));
                if (navigation != _navigation)
                {
                    return;
                }

                CurrentView = view;
                (view as GraphicsViewModel)?.SelectFirst();
            }
            catch (Exception e) when (navigation != _navigation)
            {
                _logger.LogDebug(e, "Dropped result of {Path}, another file was selected", file.Path);
            }
            catch (Exception e) when (e is NotSupportedException or NotImplementedException)
            {
                _logger.LogWarning("Can not edit {Path} ({Category}): {Reason}", file.Path, file.Category, e.Message);
                CurrentView = null;
                IsLoading = false;
                await ShowMessageAsync("Not supported yet", $"{file.Path} ({file.Category}) can not be edited yet.\n{e.Message}");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Could not read {Path}", file.Path);
                CurrentView = null;
                IsLoading = false;
                await ShowMessageAsync("Error", $"Could not read {file.Path}:\n{e.Message}");
            }
            finally
            {
                if (navigation == _navigation)
                {
                    _loadingNavigation = 0;
                    IsLoading = false;
                }
            }
        }

        private async Task ShowLoadingIndicatorAsync(int navigation)
        {
            _loadingNavigation = navigation;
            await Task.Delay(LoadingIndicatorDelay);
            if (_loadingNavigation == navigation)
            {
                IsLoading = true;
            }
        }

        /// <summary>reads the file, checks it can be written back unchanged and creates the editor; runs on a worker thread</summary>
        private async Task<ViewModelBase> LoadEditorAsync(RomFile file)
        {
            if (file.Category == FontCategory)
            {
                _logger.LogInformation("Opened the font editor ({Files})", _fontService.FontFiles);
                return new FontViewModel(_fontService, _storageProvider);
            }

            if (file.Path == RomPaths.Arm9)
            {
                _logger.LogInformation("Opened program texts {Section}", file.Name);
                return new ProgramTextsViewModel(_romService, _storageProvider, file.Name);
            }

            {
                await using var stream = _romService.OpenFile(file.Path);
                if (file.Path.EndsWith(".narc", StringComparison.OrdinalIgnoreCase))
                {
                    var narc = new byte[stream.Length];
                    await stream.ReadExactlyAsync(narc);
                    _logger.LogInformation("Opened graphics archive {Path}", file.Path);
                    return new GraphicsViewModel(narc, _graphicsReader, _graphicsWriter, _romService, _storageProvider, file.Path);
                }

                var csvb = await _mediator.Send(new ReadCsvbFile(stream));

                Func<Stream, Task>? write = csvb switch
                {
                    PrologueCsvb prologue => output => _writer.WriteFileAsync(output, prologue),
                    ConversationCsvb conversation => output => _conversationWriter.WriteFileAsync(output, conversation),
                    TableCsvb table => output => _tableWriter.WriteFileAsync(output, table),
                    CutInCsvb cutIn => output => _cutInWriter.WriteFileAsync(output, cutIn),
                    StaffRollTextCsvb staffRoll => output => _staffRollWriter.WriteFileAsync(output, staffRoll),
                    EvmBaseCsvb evm => async output => await output.WriteAsync(await _mediator.Send(new WriteEvmBase(evm))),
                    _ => null,
                };

                if (write != null && !await IsWrittenUnchangedAsync(write, stream)
                    && !(csvb is ConversationCsvb conversationCsvb && await IsSameConversationAsync(conversationCsvb, stream)))
                {
                    // e.g. epilogues share the prologue MAPI type, but the reader loses data for them
                    throw new NotSupportedException("The reader/writer can not reproduce this file exactly yet, saving it would damage it.");
                }

                ViewModelBase view = csvb switch
                {
                    PrologueCsvb prologue => new PrologueCsvbViewModel(prologue, _storageProvider, _writer, _romService, file.Path),
                    ConversationCsvb conversation => new ConversationCsvbViewModel(conversation, _storageProvider, _conversationWriter, _romService, file.Path),
                    TableCsvb table => new TableCsvbViewModel(table, _tableWriter, _romService, _storageProvider, file.Path),
                    CutInCsvb cutIn => new CutInCsvbViewModel(cutIn, _cutInWriter, _romService, _storageProvider, file.Path),
                    StaffRollTextCsvb staffRoll => new StaffRollTextViewModel(staffRoll, _staffRollWriter, _romService, _storageProvider, file.Path),
                    EvmBaseCsvb evm => new EvmCsvbViewModel(evm, _storageProvider, _mediator, _romService, file.Path),
                    _ => throw new NotSupportedException($"{csvb.Type} files are not supported yet"),
                };
                _logger.LogInformation("Opened {Path} as {Type} in {ViewModel}", file.Path, csvb.GetType().Name, view.GetType().Name);
                return view;
            }
        }

        private static async Task<bool> IsWrittenUnchangedAsync(Func<Stream, Task> write, Stream original)
        {
            using var written = new MemoryStream();
            await write(written);

            var originalBytes = new byte[original.Length];
            original.Position = 0;
            await original.ReadExactlyAsync(originalBytes);

            return written.ToArray().AsSpan().SequenceEqual(originalBytes);
        }

        /// <summary>
        /// Conversations edited by other tools differ only in the text table layout; reading the written file back
        /// must give the same script and texts.
        /// </summary>
        private async Task<bool> IsSameConversationAsync(ConversationCsvb conversation, Stream original)
        {
            using var written = new MemoryStream();
            await _conversationWriter.WriteFileAsync(written, conversation);
            written.Position = 0;
            var fromWritten = await _mediator.Send(new ReadCsvbFile(written));

            var originalBytes = new byte[original.Length];
            original.Position = 0;
            await original.ReadExactlyAsync(originalBytes);

            return fromWritten is ConversationCsvb writtenConversation
                && ConversationEquivalence.IsEquivalent(originalBytes, conversation, written.ToArray(), writtenConversation);
        }

        private static async Task ShowMessageAsync(string title, string message)
        {
            await MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
            {
                ContentTitle = title,
                ContentMessage = message,
                ButtonDefinitions = ButtonEnum.Ok,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            }).ShowAsync();
        }

        private static async Task<bool> ConfirmAsync(string title, string message)
        {
            var result = await MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
            {
                ContentTitle = title,
                ContentMessage = message,
                ButtonDefinitions = ButtonEnum.YesNo,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            }).ShowAsync();

            return result == ButtonResult.Yes;
        }
    }
}