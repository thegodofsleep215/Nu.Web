using System.Collections.Generic;

namespace Nu.Web
{
    public struct HttpRequest
    {
        public string Method;
        public string Url;
        public string Version;
        public Dictionary<string, string> Args;
        public bool Execute;
        public Dictionary<string, string> Headers;
        public int BodySize;
        public byte[] BodyData;
    }
}