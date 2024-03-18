using AutoGen;
using AutoGen.Core;
using AutoGenDotNet.Models.Helpers;

namespace AutoGenDotNet.Models.AgentClasses;

/// <summary>
/// Represents an interactive user proxy agent.
/// </summary>
public class InteractiveUserProxyAgent : InteractiveConversableAgent, IInteractiveAgent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InteractiveUserProxyAgent"/> class.
    /// </summary>
    /// <param name="name">The name of the agent.</param>
    /// <param name="systemMessage">The system message of the agent.</param>
    /// <param name="llmConfig">The conversable agent configuration.</param>
    /// <param name="isTermination">The termination condition.</param>
    /// <param name="humanInputMode">The human input mode.</param>
    /// <param name="functionMap">The function map.</param>
    /// <param name="defaultReply">The default reply.</param>
    public InteractiveUserProxyAgent(string name, string systemMessage = "You are a helpful AI assistant", ConversableAgentConfig? llmConfig = null, Func<IEnumerable<IMessage>, CancellationToken, Task<bool>>? isTermination = null, HumanInputMode humanInputMode = HumanInputMode.AUTO, IDictionary<string, Func<string, Task<string>>>? functionMap = null, string? defaultReply = null) : base(name, systemMessage, llmConfig, isTermination, humanInputMode, functionMap, defaultReply)
    {
        Tcs = new TaskCompletionSource<string?>();
    }

    ///// <inheritdoc />
    //public TaskCompletionSource<string?> Tcs { get; set; }
    public Task<MiddlewareAgent<InteractiveUserProxyAgent>> RegisterHumanInputAsync()
    {
        return Task.FromResult(this.RegisterHumanInput(GetHumanInputAsync));
    }
   
}