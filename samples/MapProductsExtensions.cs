namespace Sample;

public static class MapProductsExtensions
{
    public static IEndpointConventionBuilder MapProducts(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("products");
        group.MapGet("/", () => new[] { new Product("Milk", 10) });
        group.MapGet("/{id}", (int id) => new[] { new Product("Milk", 10) });

        return group;
    }
}
