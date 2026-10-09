
namespace MSG00.Translation.Application.Features.Csvb.Mapi.PrologueEpilogue
{
    /// <summary>
    /// Pointer whose slot holds text offsets of image names (e.g. "pro_02", "pro_02_obj"); used for
    /// <see cref="ProloguePointerType.LoadImage"/>, <see cref="ProloguePointerType.SwitchImage"/> and <see cref="ProloguePointerType.Unknown0A"/>,
    /// and for <see cref="ProloguePointerType.SceneChange"/> / <see cref="ProloguePointerType.SceneChangeEnd"/> whose slot points into the text table
    /// (in the Epilogues these name two different images, e.g. "Epi_02", "Epilogue_02").
    /// </summary>
    public class ProloguePointerImageReference : ProloguePointer
    {
        public ProloguePointerImageReference()
        {
            Type = ProloguePointerType.LoadImage;
        }

        public List<string> ImageNames { get; set; } = new List<string>();
    }
}
