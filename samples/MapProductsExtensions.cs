using Microsoft.AspNetCore.Http.HttpResults;

namespace Sample;

public static class MapProductsExtensions
{
    public static IEndpointConventionBuilder MapProducts(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("products");
        group.MapGet("/", () => TypedResults.Ok(new[] { new Product("Milk", 10) }));
        group.MapGet("/{id}", Results<Ok<Product>, NotFound> (int id) => id switch
        {
            0 => TypedResults.Ok(new Product("Milk", 10)),
            _ => TypedResults.NotFound(),
        });

        return group;
    }
}
