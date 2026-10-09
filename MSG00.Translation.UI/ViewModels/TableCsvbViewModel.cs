using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MSG00.Translation.Application.Features.Csvb.Table;
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
    /// Edits the translatable text columns (<see cref="TextColumns"/>) of a csvb table database like App/OG_MapMission.csvb.
    /// </summary>
    public class TableCsvbViewModel : ViewModelBase
    {
        private readonly TableCsvb _tableCsvb;
        private readonly IWriter _writer;
        private readonly IRomService _romService;
        private readonly string _romPath;
        private readonly IStorageProvider _storageProvider;

        public TableCsvbViewModel(TableCsvb tableCsvb, IWriter writer, IRomService romService, IStorageProvider storageProvider, string romPath)
        {
            _tableCsvb = tableCsvb;
            _storageProvider = storageProvider;
            _writer = writer;
            _romService = romService;
            _romPath = romPath;

            Rows = new ObservableCollection<TableTextRow>(tableCsvb.Texts
                .Select(text => (text, column: TextColumns.Find(text.Table, text.Column)))
                .Where(x => x.column != null)
                .OrderBy(x => TextColumns.All.ToList().IndexOf(x.column!))
                .ThenBy(x => x.text.Row)
                .Select(x => new TableTextRow(x.text, x.column!, RowLabel(x.text, x.column!))));
        }

        public ObservableCollection<TableTextRow> Rows { get; }

        public ICommand SaveCommand => new AsyncRelayCommand(ApplyAsync);

        public ICommand ExportCommand => new AsyncRelayCommand(ExportAsync);

        public ICommand ImportCommand => new AsyncRelayCommand(ImportAsync);

        private async Task ExportAsync()
        {
            var texts = Rows.Select(x => new TableTextJson(x.TableText.Table, x.TableText.Row, x.TableText.Column, x.RowLabel, x.Original, x.Text)).ToList();
            await JsonTextFile.ExportAsync(_storageProvider, $"{Path.GetFileNameWithoutExtension(_romPath)}.json", texts);
        }

        /// <summary>
        /// Applies texts by table, row and column, so the order in the file does not matter.
        /// </summary>
        private async Task ImportAsync()
        {
            if (await JsonTextFile.ImportAsync<TableTextJson>(_storageProvider) is not { } imported)
            {
                return;
            }

            var rows = Rows.ToDictionary(x => (x.TableText.Table, x.TableText.Row, x.TableText.Column));
            var unknown = imported.Where(x => !rows.ContainsKey((x.Table, x.Row, x.Column))).ToList();
            foreach (var text in imported.Where(x => rows.ContainsKey((x.Table, x.Row, x.Column))))
            {
                rows[(text.Table, text.Row, text.Column)].Text = text.Text;
            }

            if (unknown.Count > 0)
            {
                await ShowMessageAsync("Import", $"{unknown.Count} texts are not translatable cells of this file and were skipped:\n" + string.Join("\n", unknown.Take(20).Select(x => $"{x.Table} row {x.Row} column {x.Column}")));
            }
        }

        private string RowLabel(TableText text, TextColumn column)
        {
            var id = column.IdColumn is { } idColumn
                ? TableCells.ReadText(_tableCsvb, _tableCsvb.Tables.First(x => x.Name == text.Table), text.Row, idColumn)
                : $"row {text.Row}";
            return $"{column.Label} · {id}";
        }

        /// <summary>
        /// Writes the csvb into the ROM kept in memory; "Save ROM as" writes the new .nds.
        /// </summary>
        private async Task ApplyAsync()
        {
            using var stream = new MemoryStream();
            await _writer.WriteFileAsync(stream, _tableCsvb);
            _romService.ReplaceFile(_romPath, stream.ToArray());

            await ShowMessageAsync("Applied", $"{_romPath} was updated in the opened ROM.\nUse \"Save ROM as\" to write a new .nds file.");
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

        /// <param name="Table">table, row and column identify the cell on import</param>
        private sealed record TableTextJson(string Table, int Row, int Column, string Label, string Original, string Text);
    }
}