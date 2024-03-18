using AutoGen.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoGenDotNet.Models.Middleware;

/// <summary>
/// Represents a middleware for handling interactive conversations.
/// </summary>
public class InteractiveMiddleware : IMiddleware
{
    private readonly StringEventWriter _writer = new();

    /// <summary>
    /// Event that is triggered when a message is output.
    /// </summary>
    public event Action<string>? MessageOutput;

    private void HandleMessageOutput(string message)
    {
        MessageOutput?.Invoke(message);
    }

    /// <summary>
    /// Invokes the middleware asynchronously.
    /// </summary>
    /// <param name="context">The middleware context.</param>
    /// <param name="agent">The agent.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The task representing the middleware invocation.</returns>
    public async Task<IMessage> InvokeAsync(MiddlewareContext context, IAgent agent,
        CancellationToken cancellationToken = default)
    {
        var saved = Console.Out;

        _writer.OnWrite += HandleMessageOutput;
        Console.SetOut(_writer);
        if (agent is IStreamingAgent streamingAgent)
        {
            IMessage? recentUpdate = null;
            await foreach (var message in (await streamingAgent.GenerateStreamingReplyAsync(context.Messages, context.Options, cancellationToken)).WithCancellation(cancellationToken))
            {
                switch (message)
                {
                    case TextMessageUpdate textMessageUpdate:
                        switch (recentUpdate)
                        {
                            case null:
                                // Print from: xxx
                                Console.WriteLine($"from: {textMessageUpdate.From}");
                                recentUpdate = new TextMessage(textMessageUpdate);
                                Console.Write(textMessageUpdate.Content);
                                break;
                            case TextMessage recentTextMessage:
                                // Print the content of the message
                                Console.Write(textMessageUpdate.Content);
                                recentTextMessage.Update(textMessageUpdate);
                                break;
                            default:
                                throw new InvalidOperationException("The recent update is not a TextMessage");
                        }

                        break;
                    case ToolCallMessageUpdate toolCallUpdate when recentUpdate is null:
                        recentUpdate = new ToolCallMessage(toolCallUpdate);
                        break;
                    case ToolCallMessageUpdate toolCallUpdate when recentUpdate is ToolCallMessage recentToolCallMessage:
                        recentToolCallMessage.Update(toolCallUpdate);
                        break;
                    case ToolCallMessageUpdate toolCallUpdate:
                        throw new InvalidOperationException("The recent update is not a ToolCallMessage");
                    case IMessage imessage:
                        recentUpdate = imessage;
                        break;
                    default:
                        throw new InvalidOperationException("The message is not a valid message");
                }
            }
            Console.WriteLine();
            if (recentUpdate is not null && recentUpdate is not TextMessage)
            {
                Console.WriteLine(recentUpdate.FormatMessage());
            }

            return recentUpdate ?? throw new InvalidOperationException("The message is not a valid message");
        }

        var reply = await agent.GenerateReplyAsync(context.Messages, context.Options, cancellationToken);

        var formattedMessages = reply.FormatMessage();

        Console.WriteLine(formattedMessages);
        Console.SetOut(saved);
        return reply;
    }

    /// <summary>
    /// Gets the name of the middleware.
    /// </summary>
    public string? Name => nameof(InteractiveMiddleware);
}
