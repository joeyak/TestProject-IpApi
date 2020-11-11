using System;

namespace IpCommon
{
    public class ServiceProcessRequest
    {
        public string SessionID { get; } = Guid.NewGuid().ToString();
        public string Ip { get; set; }
    }
}
