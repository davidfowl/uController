using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using Web.Framework;

namespace Samples
{
    public class ExternalMetadataHandler
    {
        public string Get(string name) => $"Hello {name}";
    }
}
