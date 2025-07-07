# Old protocol, new tricks: Learning vintage HTTP to ship modern AI.

"Tokens per second" sounds impressive, but attention spans are measured in milliseconds. Agentic workflows unlock powerful new capabilities, and interminable wait times. Fortunately, HTTP streaming, a dial-up relic, can incrementally reveal progress.

We'll resurrect HTTP features that have been dormant in our typical React+API+DB stack, and discover how they can enhance modern user experience.

## Agenda

1. **What's the problem?**

   - Tokens-per-second is actually a long time to wait
   - Agentic chains ⇒ from 10s to multiple minutes of latency

2. **The One-Chunk Mindset**

   - How we got here: ASP.NET MVC, AJAX+JSON, SPA loaders
   - .NET Demo - Full-page HTML and/or text response

3. **HTTP Streaming Primer**

   - .NET Demo: HTML streamed line-by-line
   - History sidebar: 1997 HTTP/1.1 chunked transfer & 2009 SSE
   - .NET Demo: Plain-text stream
   - .NET Demo: Streaming LLM tokens to client

4. **Advanced Streaming Techniques**

   - Discuss streaming challenges of sophisticated agentic workflows
     - Multiple LLM calls to merge into single stream
     - Want to stream more than just LLM tokens, progress updates, tool calls, arbitrary data etc...
   - Demo: SSE to stream arbitrary stuff like logs, data, tool calls, llm events

> All concepts will be explained through barebones .NET Core Minimal API examples.

## Summary

This project demonstrates how streaming HTTP—a capability that has existed in browsers and HTTP for decades—can be leveraged to create responsive AI applications. While traditional web approaches loaded entire content in one chunk, modern LLM interactions benefit from progressive, line-by-line streaming delivery.

## Key Concepts

- **Streaming HTTP**: Progressive delivery of content that enables real-time, incremental response rendering
- **Vintage Protocol, Modern Use**: Utilizing long-existing HTTP streaming capabilities for cutting-edge AI applications
- **Progressive Generation**: Showing content as it's generated rather than waiting for complete responses

## Technical Approach

1. **Full Content Block**: Traditional approach loading entire responses at once
2. **Line-by-Line Delivery**: Implementing incremental content delivery with configurable intervals
3. **Built-in Browser Support**: Leveraging existing browser and HTTP streaming capabilities

## Implementation

This minimal API demonstrates:

- HTTP streaming protocols for AI responses
- Progressive content delivery techniques
- Real-time response rendering capabilities

## Getting Started

```bash
dotnet run
```

The API will be available at `https://localhost:7000` (or the port specified in your launch settings).

## Endpoints

- `/stream` - Demonstrates basic streaming response
- `/ai-stream` - Simulates AI response streaming with progressive text delivery

## Why This Matters

Streaming transforms AI response delivery from a static, wait-for-completion model to a dynamic, real-time interaction that feels more responsive and engaging—all using protocols that have been available for decades.
