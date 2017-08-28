using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Web.Framework;

namespace WebApplication45
{
    public class ProductsHandler : HttpHandler
    {
        [HttpGet]
        public object Get([FromRoute]int? id)
        {
            return new
            {
                id = id.GetValueOrDefault(),
                Name = "David Fowler",
            };
        }
    }
}
