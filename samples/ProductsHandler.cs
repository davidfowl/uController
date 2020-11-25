using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Routing;
using uController;
using static uController.Results;

namespace Samples
{
    [Route("/products")]
    public class ProductsHandler
    {
        [HttpGet]
        public IEnumerable<Product> Get()
        {
            return Enumerable.Empty<Product>();
        }

        [HttpGet("{id}")]
        public IResult Get([FromRoute] int? id)
        {
            if (id is null)
            {
                return NotFound();
            }

            return Ok(new Product(id.Value, "Cat Food", 10.0));
        }

        [HttpPost]
        public IResult Post([FromBody] Product product)
        {
            return Ok(product);
        }

        [HttpPut("{id}")]
        public IResult Put([FromRoute] int? id, [FromBody] Product product)
        {
            if (id is null)
            {
                return NotFound();
            }

            return Ok(product);
        }
    }

    public record Product(int Id, string Name, double Price);
}
