using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MSG00.Translation.Application.Features.Csvb.Mapi.Conversation;
using MSG00.Translation.Application.Features.Rom;
using MSG00.Translation.UI.Models;
using MSG00.Translation.UI.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MSG00.Translation.UI.ViewModels
{
    public class ConversationCsvbViewModel : CsvbViewModelBase
    {
        private readonly ConversationCsvb _conversationCsvb;
        private readonly IWriter _writer;
        private readonly IRomService _romService;
        private readonly string _romPath;

        public ConversationCsvbViewModel(ConversationCsvb conversationCsvb, IStorageProvider storageProvider, IWriter writer, IRomService romService, string romPath) : base(storageProvider)
        {
            _conversationCsvb = conversationCsvb;
            _writer = writer;
            _romService = romService;
            _romPath = romPath;
            RefreshRows();
        }

        public string RomPath => _romPath;

        private ObservableCollection<ConversationRow> _rows = new();
        public ObservableCollection<ConversationRow> Rows
        {
            get => _rows;
            private set => SetProperty(ref _rows, value);
        }

        private void RefreshRows()
        {
            Rows = new ObservableCollection<ConversationRow>(ConversationRow.Create(_conversationCsvb));
            foreach (var entry in TextEntries)
            {
                entry.CanRemove = _conversationCsvb.CanRemove(entry.Pointer);
                entry.AddBoxCommand = new RelayCommand(() => AddBox(entry));
                entry.SplitCommand = new RelayCommand(() => Split(entry));
                entry.RemoveCommand = new RelayCommand(() => Remove(entry));
            }
        }

        private void AddBox(ConversationTextEntry entry)
        {
            _conversationCsvb.InsertTextAfter(entry.Pointer, string.Empty);
            RefreshRows();
        }

        /// <summary>
        /// Keeps the first lines that fit into the box and moves the rest into a new box after it.
        /// </summary>
        private void Split(ConversationTextEntry entry)
        {
            var lines = entry.Text.Split('\n');
            if (lines.Length <= ConversationTextEntry.MaxLines)
            {
                return;
            }

            entry.Text = string.Join("\n", lines.Take(ConversationTextEntry.MaxLines));
            _conversationCsvb.InsertTextAfter(entry.Pointer, string.Join("\n", lines.Skip(ConversationTextEntry.MaxLines)));
            RefreshRows();
        }

        private void Remove(ConversationTextEntry entry)
        {
            _conversationCsvb.Remove(entry.Pointer);
            RefreshRows();
        }

        private IEnumerable<ConversationTextEntry> TextEntries => Rows.OfType<ConversationTextEntry>();

        /// <summary>
        /// Writes the csvb into the ROM kept in memory; "Save ROM as" writes the new .nds.
        /// </summary>
        protected override async Task SaveAsync()
        {
            using var stream = new MemoryStream();
            await _writer.WriteFileAsync(stream, _conversationCsvb);
            _romService.ReplaceFile(_romPath, stream.ToArray());

            await ShowMessageAsync("Applied", $"{_romPath} was updated in the opened ROM.\nUse \"Save ROM as\" to write a new .nds file.");
        }

        private async Task ExportAsync()
        {
            // speaker is exported for context only, import uses the order of the texts
            string? speaker = null;
            var texts = new List<ConversationTextJson>();
            foreach (var row in Rows)
            {
                switch (row)
                {
                    case ConversationSpeakerRow speakerRow:
                        speaker = speakerRow.Name;
                        break;
                    case ConversationTextEntry text:
                        texts.Add(new ConversationTextJson(speaker, text.Kind, text.Text));
                        break;
                }
            }

            await JsonTextFile.ExportAsync(StorageProvider, $"{Path.GetFileNameWithoutExtension(_romPath)}.json", texts);
        }

        private async Task ImportAsync()
        {
            if (await JsonTextFile.ImportAsync<ConversationTextJson>(StorageProvider) is not { } imported)
            {
                return;
            }

            var entries = TextEntries.ToList();

            if (imported.Length != entries.Count)
            {
                await ShowMessageAsync("Import", $"The file has {imported.Length} texts, this conversation has {entries.Count}. Texts are applied in order as far as possible.");
            }

            foreach (var (entry, text) in entries.Zip(imported))
            {
                entry.Text = text.Text;
            }
        }

        public new ICommand ExportCommand => new AsyncRelayCommand(ExportAsync);
        public new ICommand ImportCommand => new AsyncRelayCommand(ImportAsync);

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

        private sealed record ConversationTextJson(string? Speaker, string Kind, string Text);
    }
}