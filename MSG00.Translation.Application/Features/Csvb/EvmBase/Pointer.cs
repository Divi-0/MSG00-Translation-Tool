namespace MSG00.Translation.Application.Features.Csvb.EvmBase
{
    public class Pointer
    {
        public required int Value { get; set; }
        public required int Type { get; set; }
        public Offset? Offset { get; set; } = null;
    }
}
