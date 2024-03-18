namespace AutoGenDotNet.Models.AgentClasses;

/// <summary>
/// Represents the event arguments for an agent request.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="AgentRequestEventArgs"/> class.
/// </remarks>
/// <param name="agent">The interactive conversable agent associated with the request.</param>
public class AgentRequestEventArgs(IInteractiveAgent agent) : EventArgs
{
    /// <summary>
    /// Gets the interactive conversable agent associated with the request.
    /// </summary>
    public IInteractiveAgent Agent { get; } = agent;
}