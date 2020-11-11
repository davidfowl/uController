using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Routing;
using uController;

namespace Samples
{
    [Route("/products")]
    public class ProductsHandler : HttpHandler
    {
        [HttpGet]
        public IEnumerable<Product> Get()
        {
            return Enumerable.Empty<Product>();
        }

        [HttpGet("{id}")]
        public IResult Get([FromRoute] int id)
        {
            if (id < 0)
            {
                return NotFound();
            }

            return Ok(new Product("Cat Food", 10.0));
        }

        [HttpPost]
        public IResult Post([FromBody] Product product)
        {
            return Ok(product);
        }

        [HttpPut("{id}")]
        public IResult Put([FromRoute] int id, [FromBody] Product product)
        {
            return Ok(product);
        }
    }

    public record Product(string Name, double Price);
}