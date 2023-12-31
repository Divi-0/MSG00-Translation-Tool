﻿using MSG00.Translation.Domain.EvmBase.Enums;

namespace MSG00.Translation.Domain.EvmBase
{
    public class EvmBaseHeader
    {
        /// <summary>
        /// Unimportant information from header offset 0x08 to 0x24
        /// </summary>
        public required byte[] HeaderInformation { get; set; } = Array.Empty<byte>();
        public required long FileOffsetToPointerTable { get; set; }

        public byte[] GetFullHeaderBytes()
        {
            return EvmBaseHeaderConst.EVM_BASE_BYTES
                .Concat(HeaderInformation)
                .Concat(BitConverter.GetBytes(FileOffsetToPointerTable))
                .ToArray();
        }
    }
}
