namespace MSG00.Translation.UI.Models
{
    public class TextHolder : NotifyBase
    {
        public required int PointerValue { get; set; }

        private string _text = string.Empty;
        public string Text
        {
            get => _text;
            set => SetProperty(ref _text, value);
        }
    }
}
