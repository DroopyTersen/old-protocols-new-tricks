var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet(
    "/html-content",
    async (HttpContext context) =>
    {
        context.Response.ContentType = "text/html";
        var html = await File.ReadAllTextAsync("example.html");
        await context.Response.WriteAsync(html);
    }
);

app.MapGet(
    "/text-content",
    async (HttpContext context) =>
    {
        context.Response.ContentType = "text/plain";

        var content =
            "This is a complete text response delivered in one chunk. "
            + "Unlike streaming responses, this entire message is sent to the browser "
            + "at once after the server has prepared the full content. "
            + "This is the traditional HTTP response pattern where you wait for "
            + "the complete response before displaying anything to the user.";

        await context.Response.WriteAsync(content);
    }
);

app.MapGet(
    "/html-stream",
    async (HttpContext context) =>
    {
        context.Response.ContentType = "text/html";

        var htmlContent = await File.ReadAllTextAsync("example.html");
        var htmlLines = htmlContent.Split('\n');

        foreach (var line in htmlLines)
        {
            await context.Response.WriteAsync(line + "\n");
            await context.Response.Body.FlushAsync();
            await Task.Delay(250);
        }
    }
);

app.MapGet(
    "/text-stream",
    async (HttpContext context) =>
    {
        context.Response.ContentType = "text/plain";

        var text =
            "This demonstrates word-by-word streaming of plain text content. "
            + "Each word appears progressively with a small delay, simulating "
            + "real-time content generation. This technique is fundamental to "
            + "creating responsive user interfaces for AI applications.";

        var words = text.Split(' ');

        foreach (var word in words)
        {
            await context.Response.WriteAsync(word + " ");
            await context.Response.Body.FlushAsync();
            await Task.Delay(150);
        }
    }
);

app.MapGet(
    "/llm-stream",
    async (HttpContext context) =>
    {
        context.Response.ContentType = "text/plain";

        var tokens = new[]
        {
            "I",
            "am",
            "a",
            "simulated",
            "AI",
            "language",
            "model",
            "response.",
            "This",
            "demonstrates",
            "how",
            "modern",
            "LLMs",
            "stream",
            "their",
            "output",
            "token",
            "by",
            "token,",
            "creating",
            "the",
            "illusion",
            "of",
            "real-time",
            "thinking.",
            "Each",
            "token",
            "appears",
            "as",
            "soon",
            "as",
            "it's",
            "generated,",
            "rather",
            "than",
            "waiting",
            "for",
            "the",
            "complete",
            "response.",
            "This",
            "approach",
            "significantly",
            "improves",
            "perceived",
            "performance",
            "and",
            "user",
            "engagement.",
        };

        foreach (var token in tokens)
        {
            await context.Response.WriteAsync(token + " ");
            await context.Response.Body.FlushAsync();
            // Vary the delay to simulate realistic token generation timing
            var delay = Random.Shared.Next(50, 200);
            await Task.Delay(delay);
        }
    }
);

app.MapGet(
    "/",
    async (HttpContext context) =>
    {
        context.Response.ContentType = "text/html";
        var html = await File.ReadAllTextAsync("app.html");
        await context.Response.WriteAsync(html);
    }
);

// Log the server URLs when starting
app.Lifetime.ApplicationStarted.Register(() =>
{
    var urls = app.Urls;
    Console.WriteLine($"Server is running on: {string.Join(", ", urls)}");
});

app.Run();
