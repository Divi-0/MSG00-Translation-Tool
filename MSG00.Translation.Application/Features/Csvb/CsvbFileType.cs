namespace MSG00.Translation.Application.Features.Csvb
{
    public enum CsvbFileType
    {
        MAPI,
        BLOCK,
        EVM_BASE,

        /// <summary>
        /// table database whose first table is named "OG_..." (e.g. App/OG_MapMission.csvb), see docs/table/README.md
        /// </summary>
        OG_TABLE,

        /// <summary>
        /// cut-in lines, tables named "SFS0_..." (Cut/CUTIN.fcmd), see docs/cutin/README.md
        /// </summary>
        CUT_IN
    }
}
