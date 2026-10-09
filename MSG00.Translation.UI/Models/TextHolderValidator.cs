using System.Text;
using FluentValidation;
using FluentValidation.Results;

namespace MSG00.Translation.UI.Models;

public class TextHolderValidator : AbstractValidator<TextHolder>
{
    private static Encoding ShiftJisEncoding = Encoding.GetEncoding("Shift-JIS");

    private const int MaxBytesPerLineDisplayableInTextBox = (4 * 19); //4 bytes for japanese symbol * 19 max displayable 
    
    public TextHolderValidator()
    {
        RuleFor(textHolder => textHolder.Text)
            .Custom((text, context) =>
            {
                foreach (var line in text.Split("\n"))
                {
                    var textBytes = ShiftJisEncoding.GetBytes(line);

                    if (textBytes.Length > MaxBytesPerLineDisplayableInTextBox)
                    {
                        context.AddFailure(new ValidationFailure(
                            nameof(TextHolder.Text), 
                            $"Text exceeds the maximum allowed byte length of {MaxBytesPerLineDisplayableInTextBox} bytes when encoded in Shift-JIS.",
                            line));
                    }
                }
            });
    }
}