using System.Text.Json.Serialization;
using System.Text.Json;

namespace uController.SourceGenerator.Tests;

// Types shared between the tests and compilation. They *must* be public.

public class TodoService
{

}

public record MyBindAsyncRecord(Uri Uri)
{
    public static ValueTask<MyBindAsyncRecord?> BindAsync(HttpContext context, ParameterInfo parameter)
    {
        Assert.Equal(typeof(MyBindAsyncRecord), parameter.ParameterType);
        Assert.StartsWith("myBindAsyncRecord", parameter.Name);

        if (!Uri.TryCreate(context.Request.Headers.Referer, UriKind.Absolute, out var uri))
        {
            return new(result: null);
        }

        return new(result: new(uri));
    }

    // BindAsync(HttpContext, ParameterInfo) should be preferred over TryParse(string, ...) if there's
    // no [FromRoute] or [FromQuery] attributes.
    public static bool TryParse(string? value, out MyBindAsyncRecord? result) =>
        throw new NotImplementedException();
}

public interface ITodo
{
    public int Id { get; }
    public string? Name { get; }
    public bool IsComplete { get; }
}

public class Todo : ITodo
{
    public int Id { get; set; }
    public string? Name { get; set; } = "Todo";
    public bool IsComplete { get; set; }
}

public class TodoJsonConverter : JsonConverter<ITodo>
{
    public override ITodo? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var todo = new Todo();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            var property = reader.GetString()!;
            reader.Read();

            switch (property.ToLowerInvariant())
            {
                case "id":
                    todo.Id = reader.GetInt32();
                    break;
                case "name":
                    todo.Name = reader.GetString();
                    break;
                case "iscomplete":
                    todo.IsComplete = reader.GetBoolean();
                    break;
                default:
                    break;
            }
        }

        return todo;
    }

    public override void Write(Utf8JsonWriter writer, ITodo value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}