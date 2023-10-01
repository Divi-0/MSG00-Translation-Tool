﻿using MSG00.Translation.Domain.Evm;
using MSG00.Translation.Domain.Interfaces;
using MSG00.Translation.Infrastructure.Reader.Evm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSG00.Translation.Infrastructure.Services
{
    public class EvmService : IEvmService
    {
        private readonly IEvmReader _evmReader;

        public EvmService(IEvmReader evmReader)
        {
            _evmReader = evmReader;
        }

        public Task<EvmCsvb> GetEvmAsync(Stream stream)
        {
            try
            {
                return _evmReader.ReadFile(stream);
            }
            catch (Exception e)
            {

                throw;
            }
        }

        public Task SaveEvmAsync(Stream stream, EvmCsvb prologueCsvb)
        {
            throw new NotImplementedException();
        }
    }
}