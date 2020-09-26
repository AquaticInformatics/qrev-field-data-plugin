using System.Collections.Generic;

namespace QRev
{
    public class Config
    {
        public string[] DateTimeFormats { get; set; }
        public Dictionary<string,string> TopEstimateMethods { get; set; }
        public Dictionary<string,string> BottomEstimateMethods { get; set; }
    }
}
