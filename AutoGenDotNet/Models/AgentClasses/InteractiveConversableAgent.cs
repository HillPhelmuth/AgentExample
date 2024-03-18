using AutoGen;
using AutoGen.Core;
using AutoGen.OpenAI;

namespace AutoGenDotNet.Models.AgentClasses;

/// <summary>
/// Represents an interactive conversable agent that extends the base ConversableAgent class to use event based interaction.
/// </summary>
public class InteractiveConversableAgent : IAgent, IInteractiveAgent
{
    private readonly IAgent? innerAgent;
    private readonly string? defaultReply;
    private readonly HumanInputMode humanInputMode;
    private readonly IDictionary<string, Func<string, Task<string>>>? functionMap;
    private readonly string systemMessage;
    private readonly IEnumerable<FunctionContract>? functions;

    /// <summary>
    /// Initializes a new instance of the InteractiveConversableAgent class with the specified parameters.
    /// </summary>
    /// <param name="name">The name of the agent.</param>
    /// <param name="systemMessage">The system message of the agent. Default is "You are a helpful AI assistant".</param>
    /// <param name="llmConfig">The LLM config of the agent. Default is null.</param>
    /// <param name="isTermination">The termination function of the agent. Default is null.</param>
    /// <param name="humanInputMode">The human input mode of the agent. Default is HumanInputMode.AUTO.</param>
    /// <param name="functionMap">The function map of the agent. Default is null.</param>
    /// <param name="defaultReply">The default reply of the agent. Default is null.</param>
    public InteractiveConversableAgent(string name, string systemMessage = "You are a helpful AI assistant", ConversableAgentConfig? llmConfig = null, Func<IEnumerable<IMessage>, CancellationToken, Task<bool>>? isTermination = null, HumanInputMode humanInputMode = HumanInputMode.AUTO, IDictionary<string, Func<string, Task<string>>>? functionMap = null, string? defaultReply = null)
    {
        this.Name = name;
        this.defaultReply = defaultReply;
        this.functionMap = functionMap;
        this.humanInputMode = humanInputMode;
        this.IsTermination = isTermination;
        this.systemMessage = systemMessage;
        this.innerAgent = llmConfig?.ConfigList != null ? this.CreateInnerAgentFromConfigList(llmConfig) : null;
        this.functions = llmConfig?.FunctionContracts;
        Tcs = new TaskCompletionSource<string?>();
    }
    public Func<IEnumerable<IMessage>, CancellationToken, Task<bool>>? IsTermination { get; }
    /// <summary>
    /// Event that is triggered to request human input.
    /// </summary>
    public event AgentRequestEventHandler? RequestInput;

    /// <summary>
    /// Overrides the GetHumanInputAsync method to trigger the event and return the task that will eventually have the result.
    /// </summary>
    /// <returns>The task that will eventually have the human input response.</returns>
    public async Task<string?> GetHumanInputAsync()
    {
        // Trigger the event to request input
        RequestInput?.Invoke(this, new AgentRequestEventArgs(this));

        // Return the task that will eventually have the result
        var response = await Tcs.Task;
        ResetForNextInput();
        return response;
    }
    
    /// <summary>
    /// Provides the input to the agent.
    /// </summary>
    /// <param name="input">The input provided by the user.</param>
    public void ProvideInput(string input)
    {
        Tcs.TrySetResult(input);
    }

    /// <summary>
    /// Resets the TaskCompletionSource for the next input.
    /// </summary>
    public void ResetForNextInput()
    {
        Tcs = new TaskCompletionSource<string?>();
    }
    private IAgent? CreateInnerAgentFromConfigList(ConversableAgentConfig config)
    {
        IAgent? agent = null;
        foreach (var llmConfig in config.ConfigList ?? Enumerable.Empty<ILLMConfig>())
        {
            agent = agent switch
            {
                null => llmConfig switch
                {
                    AzureOpenAIConfig azureConfig => new GPTAgent(this.Name!, this.systemMessage, azureConfig, temperature: config.Temperature ?? 0),
                    OpenAIConfig openAIConfig => new GPTAgent(this.Name!, this.systemMessage, openAIConfig, temperature: config.Temperature ?? 0),
                    _ => throw new ArgumentException($"Unsupported config type {llmConfig.GetType()}"),
                },
                IAgent innerAgent => innerAgent.RegisterReply(async (messages, cancellationToken) =>
                {
                    return await innerAgent.GenerateReplyAsync(messages, cancellationToken: cancellationToken);
                }),
            };
        }

        return agent;
    }
    /// <inheritdoc />
    public TaskCompletionSource<string?> Tcs { get; set; }

    public async Task<IMessage> GenerateReplyAsync(IEnumerable<IMessage> messages, GenerateReplyOptions? options = null,
        CancellationToken cancellationToken = new CancellationToken())
    {
        if (!messages.Any(m => m.IsSystemMessage()))
        {
            var systemMessage = new TextMessage(Role.System, this.systemMessage, from: this.Name);
            messages = new[] { systemMessage }.Concat(messages);
        }

        // process order: function_call -> human_input -> inner_agent -> default_reply -> self_execute
        // first in, last out

        // process default reply
        MiddlewareAgent agent;
        if (this.innerAgent != null)
        {
            agent = innerAgent.RegisterMiddleware(async (msgs, option, agent, ct) =>
            {
                var updatedMessages = msgs.Select(m =>
                {
                    if (m.From == this.Name)
                    {
                        m.From = this.innerAgent.Name;
                        return m;
                    }
                    else
                    {
                        return m;
                    }
                });

                return await agent.GenerateReplyAsync(updatedMessages, option, ct);
            });
        }
        else
        {
            agent = new MiddlewareAgent<DefaultReplyAgent>(new DefaultReplyAgent(this.Name!, this.defaultReply ?? "Default reply is not set. Please pass a default reply to assistant agent"));
        }

        // process human input
        var humanInputMiddleware = new HumanInputMiddleware(mode: this.humanInputMode, isTermination: this.IsTermination, getInputAsync:this.GetHumanInputAsync!);
        agent.Use(humanInputMiddleware);

        // process function call
        var functionCallMiddleware = new FunctionCallMiddleware(functions: this.functions, functionMap: this.functionMap);
        agent.Use(functionCallMiddleware);

        return await agent.GenerateReplyAsync(messages, options, cancellationToken);
    }

    public string Name { get; }
}