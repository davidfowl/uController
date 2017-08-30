using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Web.Framework;

namespace Samples
{
    public class ProductsHandler
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
