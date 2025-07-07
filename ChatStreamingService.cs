using OpenAI;
using OpenAI.Chat;

namespace StreamingDemo;

// -----------------------------------------------------------------------------
// ChatStreamingService — tiny helper atop OpenAI .NET SDK.
//   • Overload 1: StreamChatAsync(string prompt, …)
//   • Overload 2: StreamChatAsync(ChatRequest request, …)
//
// Both paths enforce:
//   – request.Stream = true
//   – request.Model  = "gpt-4o" (updated to current model)
// -----------------------------------------------------------------------------
public sealed class ChatStreamingService
{
    private readonly ChatClient _chat;
    private const string DefaultModel = "gpt-4o";

    public ChatStreamingService(string apiKey)
    {
        var client = new OpenAIClient(apiKey);
        _chat = client.GetChatClient(DefaultModel);
    }

    /*──── Quick-prompt overload ─────────────────────────────────────────────*/
    public Task StreamChatAsync(
        string prompt,
        Func<string, Task> onDelta,
        CancellationToken cancellationToken = default
    )
    {
        var messages = new[] { ChatMessage.CreateUserMessage(prompt) };
        var options = new ChatCompletionOptions { };

        return StreamChatAsync(messages, options, onDelta, cancellationToken);
    }

    /*──── Full-request overload ─────────────────────────────────────────────*/
    public async Task StreamChatAsync(
        IEnumerable<ChatMessage> messages,
        ChatCompletionOptions options,
        Func<string, Task> onDelta,
        CancellationToken cancellationToken = default
    )
    {
        await foreach (
            var update in _chat.CompleteChatStreamingAsync(messages, options, cancellationToken)
        )
        {
            if (update.ContentUpdate.Count > 0)
            {
                var text = update.ContentUpdate[0].Text;
                if (!string.IsNullOrEmpty(text))
                    await onDelta(text);
            }
        }
    }
}
