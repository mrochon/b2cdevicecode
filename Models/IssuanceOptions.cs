using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace B2CDeviceCode.Models
{
    public class IssuanceOptions
    {
        public int ExpiresInSeconds { get; set; }
        public int RequestInterval { get; set; }
        public string Message { get; set; }
    }
}
