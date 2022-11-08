class Wrapper
{
    public static readonly string RoutePattern = "/foo/{id}";

    public static readonly Func<string, string> HelloDelegate = Hello2;

    public static readonly Func<string, string> HelloDelegate2 = (name) => $"Hello {name}";

    public static string Hello2(string name) => $"Hello {name}";

    public string Hello(int id) => "Hello World";

    public void AddRoutes(IEndpointRouteBuilder routes)
    {
        routes.MapGet("/hello2", Hello);
    }
}
