namespace AutoGenDotNet.Models.AgentClasses;

/// <summary>
/// Represents an interactive agent that can request human input and provide responses.
/// </summary>
public interface IInteractiveAgent
{
    /// <summary>
    /// Event that is triggered to request human input.
    /// </summary>
    event AgentRequestEventHandler? RequestInput;

    /// <summary>
    /// Overrides the GetHumanInputAsync method to trigger the event and return the task that will eventually have the result.
    /// </summary>
    /// <returns>The task that will eventually have the human input response.</returns>
    Task<string?> GetHumanInputAsync();

    /// <summary>
    /// Provides the input to the agent.
    /// </summary>
    /// <param name="input">The input provided by the user.</param>
    void ProvideInput(string input);

    /// <summary>
    /// Resets the TaskCompletionSource for the next input.
    /// </summary>
    void ResetForNextInput();
    /// <summary>
    /// Agent Name
    /// </summary>
    string Name { get; }
    TaskCompletionSource<string?> Tcs { get; set; }
}