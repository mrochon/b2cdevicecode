using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace B2CDeviceCode.Models
{
    public class RequestStatus
    {
        public string client_id { get; set; }
        public string[] scopes { get; set; }
        public string userCode { get; set; }
        public string journeyName { get; set; }
        public string authResult { get; set; }
        public bool isReady { get; set; }
        public override bool Equals(Object obj)
        {
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
                return false;
            var r = (RequestStatus)obj;
            return (client_id == r.client_id) && (userCode == r.userCode);
            //TODO: what about scopes?
        }
        public override int GetHashCode() { return 0; }
    }
}
