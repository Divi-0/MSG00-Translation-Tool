using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MSG00.Translation.Application.Features.Csvb.Mapi.StaffRoll;
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
    /// Edits the texts of a staff roll (2D/StaffRoll*.csvb).
    /// </summary>
    public class StaffRollTextViewModel : ViewModelBase
    {
        private readonly StaffRollTextCsvb _staffRoll;
        private readonly IWriter _writer;
        private readonly IRomService _romService;
        private readonly IStorageProvider _storageProvider;
        private readonly string _romPath;

        public StaffRollTextViewModel(StaffRollTextCsvb staffRoll, IWriter writer, IRomService romService, IStorageProvider storageProvider, string romPath)
        {
            _staffRoll = staffRoll;
            _writer = writer;
            _romService = romService;
            _storageProvider = storageProvider;
            _romPath = romPath;
            Rows = new ObservableCollection<StaffRollTextRow>(staffRoll.Texts.Select((text, index) => new StaffRollTextRow(text, index)));
        }

        public ObservableCollection<StaffRollTextRow> Rows { get; }

        public ICommand SaveCommand => new AsyncRelayCommand(ApplyAsync);

        public ICommand ExportCommand => new AsyncRelayCommand(ExportAsync);

        public ICommand ImportCommand => new AsyncRelayCommand(ImportAsync);

        /// <summary>
        /// Writes the file into the ROM kept in memory; "Save ROM as" writes the new .nds.
        /// </summary>
        private async Task ApplyAsync()
        {
            using var stream = new MemoryStream();
            await _writer.WriteFileAsync(stream, _staffRoll);
            _romService.ReplaceFile(_romPath, stream.ToArray());

            await ShowMessageAsync("Applied", $"{_romPath} was updated in the opened ROM.\nUse \"Save ROM as\" to write a new .nds file.");
        }

        private async Task ExportAsync()
        {
            var texts = Rows.Select(x => new StaffRollTextJson(x.Index, x.Original, x.Text)).ToList();
            await JsonTextFile.ExportAsync(_storageProvider, $"{Path.GetFileNameWithoutExtension(_romPath)}.json", texts);
        }

        /// <summary>
        /// Applies texts by index when the original matches, so all three staff roll files can share one file.
        /// </summary>
        private async Task ImportAsync()
        {
            if (await JsonTextFile.ImportAsync<StaffRollTextJson>(_storageProvider) is not { } imported)
            {
                return;
            }

            var skipped = 0;
            foreach (var text in imported)
            {
                if (text.Index < 0 || text.Index >= Rows.Count || Rows[text.Index].Original != text.Original)
                {
                    skipped++;
                    continue;
                }

                Rows[text.Index].Text = text.Text;
            }

            if (skipped > 0)
            {
                await ShowMessageAsync("Import", $"{skipped} texts did not match a text of this file and were skipped.");
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

        /// <param name="Index">position of the text in the file, with <paramref name="Original"/> the key on import</param>
        private sealed record StaffRollTextJson(int Index, string Original, string Text);
    }
}