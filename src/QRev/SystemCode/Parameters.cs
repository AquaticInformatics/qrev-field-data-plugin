using System.Collections.Generic;

namespace QRev.SystemCode
{
    public static class Parameters
    {
        public static string DischargeQr => "QR";
        public static string StageHg => "HG";
        public static string RiverSectionArea => "RiverSectionArea";
        public static string RiverSectionWidth => "RiverSectionWidth";
        public static string WaterVelocityWv => "WV";
        public static string WaterTemp => "TW";

        public static IReadOnlyList<string> DischargeSummaryParameterIds =>
            new List<string>
            {
                DischargeQr,
                StageHg,
                RiverSectionArea,
                RiverSectionWidth,
                WaterVelocityWv
            };
    }
}
