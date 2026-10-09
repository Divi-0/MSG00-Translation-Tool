using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using Mediator;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MSG00.Translation.Application.Features.Csvb.EvmBase;
using MSG00.Translation.Application.Features.Csvb.EvmBase.Write;
using MSG00.Translation.Application.Features.Rom;
using MSG00.Translation.UI.Models;
using MSG00.Translation.UI.Services;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ICommand = System.Windows.Input.ICommand;

namespace MSG00.Translation.UI.ViewModels
{
    /// <summary>
    /// Edits the messages (EV_SEQ command 0x67) of a mission script (Ev/**/EVM_*.csvb).
    /// </summary>
    public class EvmCsvbViewModel : ViewModelBase
    {
        private readonly EvmBaseCsvb _evmBaseCsvb;
        private readonly IStorageProvider _storageProvider;
        private readonly IMediator _mediator;
        private readonly IRomService _romService;
        private readonly string _romPath;

        public EvmCsvbViewModel(EvmBaseCsvb evmBaseCsvb, IStorageProvider storageProvider, IMediator mediator, IRomService romService, string romPath)
        {
            _evmBaseCsvb = evmBaseCsvb;
            _storageProvider = storageProvider;
            _mediator = mediator;
            _romService = romService;
            _romPath = romPath;

            Rows = new ObservableCollection<EvmMessageRow>(EvmMessages.Find(evmBaseCsvb)
                .Select(x => new EvmMessageRow(x, EvmMessages.OriginalText(evmBaseCsvb, x.TextOffset))));
        }

        public ObservableCollection<EvmMessageRow> Rows { get; }

        /// <summary>e.g. EVM_UNI013/014: the mission shows only cut-ins (Cut/CUTIN.fcmd), the script has no text</summary>
        public bool HasNoMessages => Rows.Count == 0;

        public ICommand SaveCommand => new AsyncRelayCommand(ApplyAsync);

        public ICommand ExportCommand => new AsyncRelayCommand(ExportAsync);

        public ICommand ImportCommand => new AsyncRelayCommand(ImportAsync);

        /// <summary>
        /// Writes the csvb into the ROM kept in memory; "Save ROM as" writes the new .nds.
        /// </summary>
        private async Task ApplyAsync()
        {
            var bytes = await _mediator.Send(new WriteEvmBase(_evmBaseCsvb));
            _romService.ReplaceFile(_romPath, bytes);

            await ShowMessageAsync("Applied", $"{_romPath} was updated in the opened ROM.\nUse \"Save ROM as\" to write a new .nds file.");
        }

        private async Task ExportAsync()
        {
            var texts = Rows.Select(x => new EvmTextJson(x.Message.Index, x.Message.Kind.ToString(), x.Original, x.Text)).ToList();
            await JsonTextFile.ExportAsync(_storageProvider, $"{Path.GetFileNameWithoutExtension(_romPath)}.json", texts);
        }

        /// <summary>
        /// Applies texts by their EV_SEQ row, so the order in the file does not matter.
        /// </summary>
        private async Task ImportAsync()
        {
            if (await JsonTextFile.ImportAsync<EvmTextJson>(_storageProvider) is not { } imported)
            {
                return;
            }

            var rows = Rows.ToDictionary(x => x.Message.Index);
            var unknown = imported.Where(x => !rows.ContainsKey(x.Index)).ToList();
            foreach (var text in imported.Where(x => rows.ContainsKey(x.Index)))
            {
                rows[text.Index].Text = text.Text;
            }

            if (unknown.Count > 0)
            {
                await ShowMessageAsync("Import", $"{unknown.Count} texts are not messages of this file and were skipped: " + string.Join(", ", unknown.Select(x => $"#{x.Index}")));
            }
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

        /// <param name="Index">EV_SEQ row, identifies the message on import</param>
        private sealed record EvmTextJson(int Index, string Kind, string Original, string Text);
    }
}