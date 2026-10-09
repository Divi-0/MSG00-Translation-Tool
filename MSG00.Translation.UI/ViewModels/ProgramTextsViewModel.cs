using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MSG00.Translation.Application.Features.Arm9;
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
    /// Edits texts inside the ARM9 program (e.g. the in-game menu); they are written in place.
    /// </summary>
    public class ProgramTextsViewModel : ViewModelBase
    {
        private readonly IRomService _romService;
        private readonly IStorageProvider _storageProvider;

        public ProgramTextsViewModel(IRomService romService, IStorageProvider storageProvider, string section)
        {
            _romService = romService;
            _storageProvider = storageProvider;
            Section = section;

            var arm9 = ReadArm9();
            Rows = new ObservableCollection<ProgramTextRow>(Arm9Texts.Entries
                .Where(x => x.Section == section)
                .Select(x => new ProgramTextRow(x, Arm9Texts.Read(arm9, x), Arm9Texts.MaxBytes(arm9, x))));
        }

        public string Section { get; }

        public ObservableCollection<ProgramTextRow> Rows { get; }

        public ICommand SaveCommand => new AsyncRelayCommand(ApplyAsync);

        public ICommand ExportCommand => new AsyncRelayCommand(ExportAsync);

        public ICommand ImportCommand => new AsyncRelayCommand(ImportAsync);

        private async Task ExportAsync()
        {
            var texts = Rows.Select(x => new ProgramTextJson($"0x{x.Entry.Offset:X}", x.Label, x.Japanese, x.Text)).ToList();
            await JsonTextFile.ExportAsync(_storageProvider, $"{Section}.json", texts);
        }

        /// <summary>
        /// Applies texts by their ARM9 offset, so a file stays valid when entries are added or reordered.
        /// </summary>
        private async Task ImportAsync()
        {
            if (await JsonTextFile.ImportAsync<ProgramTextJson>(_storageProvider) is not { } imported)
            {
                return;
            }

            var rows = Rows.ToDictionary(x => $"0x{x.Entry.Offset:X}");
            var unknown = imported.Where(x => !rows.ContainsKey(x.Offset)).ToList();
            foreach (var text in imported.Where(x => rows.ContainsKey(x.Offset)))
            {
                rows[text.Offset].Text = text.Text;
            }

            if (unknown.Count > 0)
            {
                await ShowMessageAsync("Import", $"{unknown.Count} texts do not belong to {Section} and were skipped:\n" + string.Join("\n", unknown.Select(x => $"{x.Offset} {x.Label}")));
            }
        }

        /// <summary>
        /// Writes the texts into the ARM9 of the ROM kept in memory; "Save ROM as" writes the new .nds.
        /// </summary>
        private async Task ApplyAsync()
        {
            var invalid = Rows.Where(x => x.IsTooLong || x.HasPlaceholderWarning).ToList();
            if (invalid.Count > 0)
            {
                await ShowMessageAsync("Not applied", "Nothing was applied, these texts need a fix:\n" + string.Join("\n", invalid.Select(x => $"{x.Label}: {(x.IsTooLong ? x.Usage : x.PlaceholderWarning)}")));
                return;
            }

            var arm9 = ReadArm9();
            foreach (var row in Rows)
            {
                Arm9Texts.Write(arm9, row.Entry, row.Text);
            }

            _romService.ReplaceFile(RomPaths.Arm9, arm9);
            await ShowMessageAsync("Applied", $"The {Section} texts were updated in the opened ROM.\nUse \"Save ROM as\" to write a new .nds file.");
        }

        private byte[] ReadArm9()
        {
            using var stream = _romService.OpenFile(RomPaths.Arm9);
            using var copy = new MemoryStream();
            stream.CopyTo(copy);
            return copy.ToArray();
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

        /// <param name="Offset">ARM9 offset, used to match texts on import</param>
        private sealed record ProgramTextJson(string Offset, string Label, string Japanese, string Text);
    }
}