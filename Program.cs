using OpenAI;
using OpenAI.Chat;
using StreamingDemo;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

// Add configuration for OpenAI API key
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Home page
app.MapGet(
    "/",
    async (HttpContext context) =>
    {
        context.Response.ContentType = "text/html";
        var html = await File.ReadAllTextAsync("app.html");
        await context.Response.WriteAsync(html);
    }
);

// =============================================================================
// 2. The One-Chunk Mindset - Full-page HTML and/or text response
// =============================================================================

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

// =============================================================================
// 3. HTTP Streaming Primer - HTML streamed line-by-line, Plain-text stream, Streaming LLM tokens
// =============================================================================

app.MapGet(
    "/html-stream",
    async (HttpContext context) =>
    {
        PrepStream(context.Response, "text/html; charset=utf-8");

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
    async ctx =>
    {
        // Set content type to html to avoid buffering. Browser or server?
        PrepStream(ctx.Response, "text/html; charset=utf-8");

        var delay = int.TryParse(ctx.Request.Query["delay"], out var ms) ? ms : 120;
        var text = "Streaming plain text feels just like typing…";

        foreach (var word in text.Split(' '))
        {
            await ctx.Response.WriteAsync(word + " ");
            await ctx.Response.Body.FlushAsync();
            await Task.Delay(delay);
        }
    }
);

app.MapGet(
    "/llm-stream",
    async (HttpContext context) =>
    {
        PrepStream(context.Response, "text/html; charset=utf-8");

        // Get OpenAI API key from configuration
        var apiKey = app.Configuration["OpenAI:ApiKey"];
        await context.Response.WriteAsync("<pre>");

        if (string.IsNullOrEmpty(apiKey))
        {
            await context.Response.WriteAsync(
                "Error: OpenAI API key not configured. Please set your API key in appsettings.json"
            );
            return;
        }

        try
        {
            var chatService = new ChatStreamingService(apiKey);
            var prompt = "Count from 1 to 100 in words. as a bulleted list.";

            await chatService.StreamChatAsync(
                prompt,
                async (delta) =>
                {
                    await context.Response.WriteAsync(delta);
                    await context.Response.Body.FlushAsync();
                },
                context.RequestAborted
            );
            await context.Response.WriteAsync("</pre>");
        }
        catch (Exception ex)
        {
            await context.Response.WriteAsync($"Error: {ex.Message}");
        }
    }
);

// =============================================================================
// 4. Advanced Streaming Techniques - SSE to stream arbitrary stuff like logs, data, tool calls, llm events
// =============================================================================

// /sse-ping – heartbeat every second
app.MapGet(
    "/sse-ping",
    async ctx =>
    {
        PrepStream(ctx.Response, "text/event-stream");

        var i = 0;
        while (!ctx.RequestAborted.IsCancellationRequested)
        {
            await ctx.Response.WriteAsync($"data: heartbeat {++i}\n\n");
            await ctx.Response.Body.FlushAsync();
            await Task.Delay(1000);
        }
    }
);

// /sse-workflow – emits log | data | llm
app.MapGet(
    "/sse-workflow",
    async ctx =>
    {
        PrepStream(ctx.Response, "text/event-stream");

        // Get OpenAI API key from configuration
        var apiKey = app.Configuration["OpenAI:ApiKey"];

        if (string.IsNullOrEmpty(apiKey))
        {
            await ctx.Response.WriteAsync("event: error\ndata: OpenAI API key not configured\n\n");
            await ctx.Response.Body.FlushAsync();
            return;
        }

        try
        {
            var svc = new ChatStreamingService(apiKey);

            /* 1️⃣ Generate SQL with a quick-prompt call */
            await ctx.Response.WriteAsync("event: log\ndata: Generating SQL query...\n\n");
            await ctx.Response.Body.FlushAsync();

            await svc.StreamChatAsync(
                prompt: "Write SQL to count users created per month (Postgres).",
                onDelta: async d =>
                {
                    await ctx.Response.WriteAsync($"event: llm\ndata: {d}\n\n");
                    await ctx.Response.Body.FlushAsync();
                },
                cancellationToken: ctx.RequestAborted
            );

            /* Pretend DB round-trip */
            await ctx.Response.WriteAsync("event: log\ndata: Executing SQL on warehouse...\n\n");
            await ctx.Response.Body.FlushAsync();
            await Task.Delay(800);

            var fakeJson = """
            [
              { "month": "2025-03", "users": 734 },
              { "month": "2025-04", "users": 910 },
              { "month": "2025-05", "users": 1021 }
            ]
            """;

            await ctx.Response.WriteAsync($"event: data\ndata: {fakeJson.Trim()}\n\n");
            await ctx.Response.Body.FlushAsync();

            /* 2️⃣ Summarize via system + user message */
            await ctx.Response.WriteAsync("event: log\ndata: Summarizing results with LLM...\n\n");
            await ctx.Response.Body.FlushAsync();

            // Create system and user messages for summary
            var summaryMessages = new ChatMessage[]
            {
                ChatMessage.CreateSystemMessage("You are a data analyst."),
                ChatMessage.CreateUserMessage(
                    $"Given this JSON data:\n{fakeJson}\nWrite a concise insight (~40 tokens)."
                ),
            };

            var summaryOptions = new ChatCompletionOptions { Temperature = 0.7f };

            await svc.StreamChatAsync(
                summaryMessages,
                summaryOptions,
                onDelta: async d =>
                {
                    await ctx.Response.WriteAsync($"event: llm\ndata: {d}\n\n");
                    await ctx.Response.Body.FlushAsync();
                },
                cancellationToken: ctx.RequestAborted
            );

            await ctx.Response.WriteAsync("event: log\ndata: Workflow complete ✅\n\n");
            await ctx.Response.Body.FlushAsync();
        }
        catch (Exception ex)
        {
            await ctx.Response.WriteAsync($"event: error\ndata: {ex.Message}\n\n");
            await ctx.Response.Body.FlushAsync();
        }
    }
);

// Log the server URLs when starting
app.Lifetime.ApplicationStarted.Register(() =>
{
    var urls = app.Urls;
    Console.WriteLine($"Server is running on: {string.Join(", ", urls)}");
});

app.Run();

/*── Helper: makes every response stream immediately ─────────────────────────*/
static void PrepStream(HttpResponse res, string mime)
{
    res.Headers.ContentType = mime;
    res.Headers.CacheControl = "no-cache";
    res.Headers["X-Accel-Buffering"] = "no"; // Nginx/Cloudflare
    res.Headers.Append("Connection", "keep-alive");
}
