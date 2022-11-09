// using Microsoft.EntityFrameworkCore;

public static class TodoEndpoints
{
    public static void MapTodoEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/Todo");
        // group.WithOpenApi();
        group.MapGet("/", async (TodoDbContext db) =>
        {
            return await db.Todos.ToListAsync();
        })
        .WithTags(nameof(Todo))
        .WithName("GetAllTodos");
        //.Produces<List<Todo>>(StatusCodes.Status200OK);

        group.MapGet("/{id}", async (int id, TodoDbContext db) =>
        {
            return await db.Todos.FindAsync(id)
                is Todo model
                    ? Results.Ok(model)
                    : Results.NotFound();
        })
        .WithTags(nameof(Todo))
        .WithName("GetTodoById");
        //.Produces<Todo>(StatusCodes.Status200OK)
        //.Produces(StatusCodes.Status404NotFound);

        group.MapPut("/{id}", async (int id, Todo todo, TodoDbContext db) =>
        {
            var foundModel = await db.Todos.FindAsync(id);

            if (foundModel is null)
            {
                return Results.NotFound();
            }

            db.Update(todo);
            await db.SaveChangesAsync();

            return Results.NoContent();
        })
        .WithTags(nameof(Todo))
        .WithName("UpdateTodo");
        //.Produces(StatusCodes.Status404NotFound)
        //.Produces(StatusCodes.Status204NoContent);

        group.MapPost("/", async (Todo todo, TodoDbContext db) =>
        {
            db.Todos.Add(todo);
            await db.SaveChangesAsync();
            return Results.Created($"/api/Todo/{todo.Id}", todo);
        })
        .WithTags(nameof(Todo))
        .WithName("CreateTodo");
        //.Produces<Todo>(StatusCodes.Status201Created);

        group.MapDelete("/{id}", async (int id, TodoDbContext db) =>
        {
            if (await db.Todos.FindAsync(id) is Todo todo)
            {
                db.Todos.Remove(todo);
                await db.SaveChangesAsync();
                return Results.Ok(todo);
            }

            return Results.NotFound();
        })
        .WithTags(nameof(Todo))
        .WithName("DeleteTodo");
        //.Produces<Todo>(StatusCodes.Status200OK)
        // .Produces(StatusCodes.Status404NotFound);
    }
}

class Todo
{
    public int Id { get; set; }
}

class TodoDbContext
{
    public DbSet<Todo> Todos { get; set; }
    public Task SaveChangesAsync() => Task.CompletedTask;
    public void Update(object o) { }
}

class DbSet<T>
{
    public Task<List<T>> ToListAsync() { return null; }

    public Task<T> FindAsync(object id) { return null; }
    public void Remove(T item) { }
    public void Add(T item) { }
}