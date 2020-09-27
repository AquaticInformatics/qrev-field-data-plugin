using System.Collections.Generic;

namespace QRev
{
    public class Config
    {
        public Dictionary<string,string> TopEstimateMethods { get; set; }
        public Dictionary<string,string> BottomEstimateMethods { get; set; }
        public Dictionary<string, string> NavigationMethods { get; set; }
        public Dictionary<string, string> DepthReferences { get; set; }
    }
}
