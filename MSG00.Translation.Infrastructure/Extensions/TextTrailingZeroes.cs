using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSG00.Translation.Infrastructure.Extensions
{
    internal static class TextTrailingZeroes
    {
        public static byte[] AddTrailingCsvbFileZeroes(this byte[] newTextBytes)
        {
            List<byte> newTextBytesList = new List<byte>(newTextBytes);

            //check if 0x00s are missing for 4 byte entry structure
            int moduloResult = newTextBytesList.Count % 4;

            for (int i = 0; i < (4 - moduloResult); i++)
            {
                newTextBytesList.Add(0x00);
            }

            return newTextBytesList.ToArray();
        }
    }
}
