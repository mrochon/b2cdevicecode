using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace B2CDeviceCode.Models
{
    public class TokenRequest
    {
        public string scope { get; set; }
        public string client_id { get; set; }
        public string grant_type { get; set; }
        public string device_code { get; set; }
        public string client_info { get; set; }
    }
}
