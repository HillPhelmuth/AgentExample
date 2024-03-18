﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// MiddlewareExtension.cs

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AutoGen.Core;

public static class MiddlewareExtension
{
    /// <summary>
    /// Register a auto reply hook to an agent. The hook will be called before the agent generate the reply.
    /// If the hook return a non-null reply, then that non-null reply will be returned directly without calling the agent.
    /// Otherwise, the agent will generate the reply.
    /// This is useful when you want to override the agent reply in some cases.
    /// </summary>
    /// <param name="agent"></param>
    /// <param name="replyFunc"></param>
    /// <returns></returns>
    /// <exception cref="Exception">throw when agent name is null.</exception>
    public static MiddlewareAgent<TAgent> RegisterReply<TAgent>(
        this TAgent agent,
        Func<IEnumerable<IMessage>, CancellationToken, Task<IMessage?>> replyFunc)
        where TAgent : IAgent
    {
        return agent.RegisterMiddleware(async (messages, options, agent, ct) =>
        {
            var reply = await replyFunc(messages, ct);

            if (reply != null)
            {
                return reply;
            }

            return await agent.GenerateReplyAsync(messages, options, ct);
        });
    }

    /// <summary>
    /// Register a post process hook to an agent. The hook will be called before the agent return the reply and after the agent generate the reply.
    /// This is useful when you want to customize arbitrary behavior before the agent return the reply.
    /// 
    /// One example is <see cref="PrintMessageMiddlewareExtension.RegisterPrintFormatMessageHook{TAgent}(TAgent)" />, which print the formatted message to console before the agent return the reply.
    /// </summary>
    /// <exception cref="Exception">throw when agent name is null.</exception>
    public static MiddlewareAgent<TAgent> RegisterPostProcess<TAgent>(
        this TAgent agent,
        Func<IEnumerable<IMessage>, IMessage, CancellationToken, Task<IMessage>> postprocessFunc)
        where TAgent : IAgent
    {
        return agent.RegisterMiddleware(async (messages, options, agent, ct) =>
        {
            var reply = await agent.GenerateReplyAsync(messages, options, ct);

            return await postprocessFunc(messages, reply, ct);
        });
    }

    /// <summary>
    /// Register a pre process hook to an agent. The hook will be called before the agent generate the reply. This is useful when you want to modify the conversation history before the agent generate the reply.
    /// </summary>
    /// <exception cref="Exception">throw when agent name is null.</exception>
    public static MiddlewareAgent<TAgent> RegisterPreProcess<TAgent>(
        this TAgent agent,
        Func<IEnumerable<IMessage>, CancellationToken, Task<IEnumerable<IMessage>>> preprocessFunc)
        where TAgent : IAgent
    {
        return agent.RegisterMiddleware(async (messages, options, agent, ct) =>
        {
            var newMessages = await preprocessFunc(messages, ct);

            return await agent.GenerateReplyAsync(newMessages, options, ct);
        });
    }

    /// <summary>
    /// Register a middleware to an existing agent and return a new agent with the middleware.
    /// </summary>
    public static MiddlewareAgent<TAgent> RegisterMiddleware<TAgent>(
        this TAgent agent,
        Func<IEnumerable<IMessage>, GenerateReplyOptions?, IAgent, CancellationToken, Task<IMessage>> func,
        string? middlewareName = null)
        where TAgent : IAgent
    {
        if (agent.Name == null)
        {
            throw new Exception("Agent name is null.");
        }


        var middlewareAgent = new MiddlewareAgent<TAgent>(agent);
        middlewareAgent.Use(func, middlewareName);

        return middlewareAgent;
    }



    /// <summary>
    /// Register a middleware to an existing agent and return a new agent with the middleware.
    /// </summary>
    public static MiddlewareAgent<TAgent> RegisterMiddleware<TAgent>(
        this TAgent agent,
        IMiddleware middleware)
        where TAgent : IAgent
    {
        if (agent.Name == null)
        {
            throw new Exception("Agent name is null.");
        }

        var middlewareAgent = new MiddlewareAgent<TAgent>(agent);
        middlewareAgent.Use(middleware);

        return middlewareAgent;
    }

    /// <summary>
    /// Register a middleware to an existing agent and return a new agent with the middleware.
    /// </summary>
    public static MiddlewareAgent<TAgent> RegisterMiddleware<TAgent>(
        this MiddlewareAgent<TAgent> agent,
        Func<IEnumerable<IMessage>, GenerateReplyOptions?, IAgent, CancellationToken, Task<IMessage>> func,
        string? middlewareName = null)
        where TAgent : IAgent
    {
        var copyAgent = new MiddlewareAgent<TAgent>(agent);
        copyAgent.Use(func, middlewareName);

        return copyAgent;
    }

    /// <summary>
    /// Register a middleware to an existing agent and return a new agent with the middleware.
    /// </summary>
    public static MiddlewareAgent<TAgent> RegisterMiddleware<TAgent>(
        this MiddlewareAgent<TAgent> agent,
        IMiddleware middleware)
        where TAgent : IAgent
    {
        var copyAgent = new MiddlewareAgent<TAgent>(agent);
        copyAgent.Use(middleware);

        return copyAgent;
    }
}
