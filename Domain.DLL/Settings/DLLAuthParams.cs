using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Domain.DLL.Settings
{
    public class DLLAuthParams
    {
        public string Key { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public string RoutingPassword { get; set; }
    }
}
