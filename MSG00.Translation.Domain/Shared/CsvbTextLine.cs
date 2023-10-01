using MSG00.Translation.Infrastructure.Domain.Misc;

namespace MSG00.Translation.Infrastructure.Domain.Shared
{
    public class CsvbTextLine : NotifyBase
    {
        private string _text = string.Empty;
        /// <summary>
        /// original Text (immutable)
        /// </summary>
        public required string Text
        {
            get => _text;
            init
            {
                SetProperty(ref _text, value);
                MutableText = _text;
            }
        }

        private string _mutableText = string.Empty;
        /// <summary>
        /// Text open for modification
        /// </summary>
        public string MutableText
        {
            get => _mutableText;
            set => SetProperty(ref _mutableText, value);
        }
    }
}
