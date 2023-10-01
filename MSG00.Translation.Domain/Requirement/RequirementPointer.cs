using MSG00.Translation.Infrastructure.Domain.Misc;

namespace MSG00.Translation.Infrastructure.Domain.Requirement
{
    public class RequirementPointer : NotifyBase
    {
        private string _text = string.Empty;
        public required string Text
        {
            get { return _text; }
            set { SetProperty(ref _text, value); }
        }
    }
}
