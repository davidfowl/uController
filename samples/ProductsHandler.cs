using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using uController;
using static uController.Results;

namespace Samples
{
    [Route("/products")]
    class ProductsHandler
    {
        [HttpGet]
        public IEnumerable<Product> Get()
        {
            return Enumerable.Empty<Product>();
        }

        [HttpGet("{id}")]
        public IResult Get(int? id)
        {
            if (id is null)
            {
                return NotFound();
            }

            return Ok(new Product(id.Value, "Cat Food", 10.0));
        }

        [HttpPost]
        public IResult Post(Product product)
        {
            return Ok(product);
        }

        [HttpPut("{id}")]
        public IResult Put(int? id, Product product)
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
