using MSG00.Translation.Infrastructure.Domain.Misc;

namespace MSG00.Translation.Infrastructure.Domain.Etc
{
    public class EtcFgHcmHgPointer : NotifyBase
    {
        private string _text = string.Empty;
        public required string Text
        {
            get { return _text; }
            set { SetProperty(ref _text, value); }
        }

        public required string GameObjectReference { get; set; }
    }
}
