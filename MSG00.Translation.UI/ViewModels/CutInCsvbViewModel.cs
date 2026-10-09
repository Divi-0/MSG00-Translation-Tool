using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MSG00.Translation.Application.Features.Csvb.CutIn;
using MSG00.Translation.Application.Features.Rom;
using MSG00.Translation.UI.Models;
using MSG00.Translation.UI.Services;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MSG00.Translation.UI.ViewModels
{
    /// <summary>
    /// Edits the cut-in lines of Cut/CUTIN.fcmd.
    /// </summary>
    public class CutInCsvbViewModel : ViewModelBase
    {
        private readonly CutInCsvb _cutInCsvb;
        private readonly IWriter _writer;
        private readonly IRomService _romService;
        private readonly IStorageProvider _storageProvider;
        private readonly string _romPath;

        public CutInCsvbViewModel(CutInCsvb cutInCsvb, IWriter writer, IRomService romService, IStorageProvider storageProvider, string romPath)
        {
            _cutInCsvb = cutInCsvb;
            _writer = writer;
            _romService = romService;
            _storageProvider = storageProvider;
            _romPath = romPath;
            Rows = new ObservableCollection<CutInLineRow>(cutInCsvb.Lines.Select((line, index) => new CutInLineRow(line, index)));
        }

        public ObservableCollection<CutInLineRow> Rows { get; }

        public ICommand SaveCommand => new AsyncRelayCommand(ApplyAsync);

        public ICommand ExportCommand => new AsyncRelayCommand(ExportAsync);

        public ICommand ImportCommand => new AsyncRelayCommand(ImportAsync);

        /// <summary>
        /// Writes the file into the ROM kept in memory; "Save ROM as" writes the new .nds.
        /// </summary>
        private async Task ApplyAsync()
        {
            using var stream = new MemoryStream();
            await _writer.WriteFileAsync(stream, _cutInCsvb);
            _romService.ReplaceFile(_romPath, stream.ToArray());

            await ShowMessageAsync("Applied", $"{_romPath} was updated in the opened ROM.\nUse \"Save ROM as\" to write a new .nds file.");
        }

        private async Task ExportAsync()
        {
            var texts = Rows.Select(x => new CutInTextJson(x.Index, x.Line.Table, x.Original, x.Text)).ToList();
            await JsonTextFile.ExportAsync(_storageProvider, "CUTIN.json", texts);
        }

        /// <summary>
        /// Applies texts by index and checks the table name, so a file from another ROM version is not applied to wrong lines.
        /// </summary>
        private async Task ImportAsync()
        {
            if (await JsonTextFile.ImportAsync<CutInTextJson>(_storageProvider) is not { } imported)
            {
                return;
            }

            var skipped = 0;
            foreach (var text in imported)
            {
                if (text.Index < 0 || text.Index >= Rows.Count || Rows[text.Index].Line.Table != text.Table)
                {
                    skipped++;
                    continue;
                }

                Rows[text.Index].Text = text.Text;
            }

            if (skipped > 0)
            {
                await ShowMessageAsync("Import", $"{skipped} texts did not match a line of this file and were skipped.");
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

        /// <param name="Index">position of the line in the file, with <paramref name="Table"/> the key on import</param>
        private sealed record CutInTextJson(int Index, string Table, string Original, string Text);
    }
}