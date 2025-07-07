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

## Streaming Challenges & Gotchas

While streaming unlocks powerful UX gains, it also exposes you to quirks that do not surface in the traditional _one-chunk_ model. Below is an outline of the most common issues you will run into and how to mitigate them.

### 1. Stream Cancellation & Resource Leaks

* **Client aborts are silent by default.**  If the browser tab is closed or the user navigates away, your server code will continue to execute unless you explicitly observe `HttpContext.RequestAborted` (ASP.NET) / the equivalent cancellation token.
* **Long-running background tasks** may continue to burn CPU/GPU minutes after the user has disappeared. Always thread the cancellation token through LLM API calls, tool executions, database work and child processes.
* **Retry behaviour** – some load-balancers will retry failed upstream requests causing duplicate work. Make your endpoints idempotent or detect partial progress.

### 2. Buffering & Proxy Layers

* **Framework buffering** – ASP.NET Core enables response buffering when the response size is unknown. Call `HttpResponse.BodyWriter.FlushAsync()` (or `await Response.WriteAsync("\n", flush:true)` in MVC) to force a chunk to flush.
* **Reverse proxies (NGINX/Apache/Caddy)** often buffer by default, negating streaming. Disable via `proxy_buffering off;` or send `X-Accel-Buffering: no`.
* **CDNs & serverless edges** – Cloudflare, Fastly and friends have their own buffering rules and minimum chunk sizes. Test with _curl_ against the public endpoint, not just localhost.

### 3. Browser & Fetch Quirks

* **Safari < 17** does not expose the `ReadableStream` returned by `fetch`, breaking streaming entirely.
* **Older Chromium versions** coalesce small chunks. Prefix tiny messages with a few spaces / newline to reach `~2 KB` when you absolutely need real-time granularity.
* **SSE parsing** – remember every event line must end with `\n\n`. Missing the extra newline will stall the stream.

### 4. Back-pressure & Flow Control

* **TCP/HTTP2 flow control** applies: if the client cannot consume data fast enough, the server will eventually block on `WriteAsync`. Surface this back-pressure to your worker logic to pause expensive work.
* **Large objects in a stream** (e.g. base64 images) can clog the pipe and starve token-level updates. Send them out-of-band or on a separate channel.

### 5. Timeouts & Keep-Alives

* **Load balancers** typically kill idle connections after 60–120 s _even if the socket is open_. Send heartbeat whitespace or comments every < 30 s.
* **Browsers** have their own internal timeout (~5 min in Chrome). Long agentic plans may need periodic `:keep-alive` SSE comments.

### 6. Content-Length, CORS & Miscellaneous

* Omit `Content-Length` or set `Transfer-Encoding: chunked`; otherwise most servers will wait to know the size up-front.
* Pre-flighted CORS requests need the same headers on _every_ chunk for some proxies. Prefer SSE which is CORS-friendly.

---

Keep this list handy when you debug a “Why is my stream not streaming?” report — 90 % of the time it’s one of the issues above.
