using AutoGen;
using AutoGen.Core;
using AutoGen.OpenAI;
using AutoGenDotNet.Models.Helpers;
using Azure.AI.OpenAI;

namespace AutoGenDotNet.Models.AgentClasses;

public class InteractiveGptAgent : GPTAgent, IInteractiveAgent
{
    public InteractiveGptAgent(string name, string systemMessage = "You are a helpful AI assistant", OpenAIClient? openAIClient = null, string modelName = "", float temp = 0.7f, int maxTokens = 1024, IDictionary<string, Func<string, Task<string>>>? functionMap = null) : base(name, systemMessage, openAIClient, modelName, temp, maxTokens, functionMap: functionMap)
    {
        Tcs = new TaskCompletionSource<string?>();
    }

    public InteractiveGptAgent(string name, string systemMessage = "You are a helpful AI assistant", ILLMConfig? llmConfig = null, float temp = 0.7f, int maxTokens = 1024,IEnumerable<FunctionDefinition> functionDefinitions = null, IDictionary<string, Func<string, Task<string>>>? functionMap = null) : base(name: name, systemMessage: systemMessage, llmConfig, temp, maxTokens,functionDefinitions, functionMap: functionMap)
    {
        Tcs = new TaskCompletionSource<string?>();
        
    }
    public TaskCompletionSource<string?> Tcs { get; set; }

    /// <summary>
    /// Event that is triggered to request human input.
    /// </summary>
    public event AgentRequestEventHandler? RequestInput;
    public Task<MiddlewareAgent<InteractiveGptAgent>> RegisterHumanInputAsync()
    {
        return Task.FromResult(this.RegisterHumanInput(GetHumanInputAsync));
    }
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
}