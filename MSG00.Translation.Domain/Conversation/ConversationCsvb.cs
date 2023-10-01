using MSG00.Translation.Domain.Files.Csvb;
using MSG00.Translation.Infrastructure.Domain.Enums;
using System.Collections.ObjectModel;

namespace MSG00.Translation.Infrastructure.Domain.Conversation
{
    public class ConversationCsvb : CsvbFile
    {
        public ConversationCsvb()
        {
            Type = CsvbFileType.MAPI;

            PointerTable = new ObservableCollection<ConversationPointerCharacter>();
        }

        public required byte[] MapiHeaderBytes { get; init; }
        public required byte[] AfterTextSectionBytes { get; init; }
        /// <summary>
        /// actual value - 1 (e.g. 17 - 1 = 16 pointers in file)
        /// </summary>
        public required int CountOfPointersInFile { get; set; }
        public required int FileSizeToTextEnd { get; set; }
        public required int FileSizeWithUnimportantInfo { get; set; }
        private ObservableCollection<ConversationPointerCharacter> _pointerTable;
        public ObservableCollection<ConversationPointerCharacter> PointerTable
        {
            get { return _pointerTable; }
            set { SetProperty(ref _pointerTable, value); }
        }
    }
}
