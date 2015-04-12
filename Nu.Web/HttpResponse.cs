using System.Collections.Generic;

namespace Nu.Web
{
    public struct HttpResponse
    {
        public int Status;
        public string Version;
        public Dictionary<string, string> Headers;
        public int BodySize;
        public byte[] BodyData;
    }
}