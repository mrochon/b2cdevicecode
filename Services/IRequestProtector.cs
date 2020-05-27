using Microsoft.AspNetCore.DataProtection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace B2CDeviceCode.Services
{
    public interface IRequestProtector: IDataProtector 
    {
    }
}
