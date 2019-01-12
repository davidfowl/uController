using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Web.Framework;

namespace Samples
{
    [Route("users")]
    public class UserApi
    {
        public object CurrentUser()
        {
            return new { name = "John Smith" };
        }
    }
}
