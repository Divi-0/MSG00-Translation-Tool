namespace MSG00.Translation.Infrastructure.Domain.Epilogue.Enums
{
    public enum EpiloguePointerType
    {
        StaticOffsetBeforeTextOffset = 0x00,
        StaticOffsetAfterImage = 0x01,
        Image = 0x02,
        StartPointer = 0x03,
        TextObjectReference = 0x06,
        Text = 0x08,
        StartOfEndReference = 0x05,
        EndReferenceSub1 = 0x07,
        EndReferenceSub2 = 0x09,
        Unknown1 = 0x0A,
        Unknown2 = 0x0B,
        Unknown3 = 0x0C,
    }
}
