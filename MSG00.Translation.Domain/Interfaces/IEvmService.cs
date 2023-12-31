﻿

using MSG00.Translation.Domain.EvmBase;

namespace MSG00.Translation.Domain.Interfaces
{
    public interface IEvmService
    {
        Task<EvmBaseCsvb> GetEvmAsync(Stream stream);
        Task SaveEvmAsync(Stream stream, EvmBaseCsvb prologueCsvb);
    }
}
