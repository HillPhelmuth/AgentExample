using System.ComponentModel;
using System.Reflection;
using System.Text;
using System.Text.Json;
using AutoGen.Core;
using AutoGenDotNet.Models.Middleware;
using Encoding = Tiktoken.Encoding;

namespace AutoGenDotNet.Models.Helpers;

/// <summary>
/// Provides static helper methods for extracting data from an assembly.
/// </summary>
public static class StaticHelpers
{
    private static Encoding? _tokenizer;
    static StaticHelpers()
    {
        _tokenizer = Encoding.ForModel("gpt-3.5-turbo");
    }
    /// <summary>
    /// Extracts data of type <typeparamref name="T"/> from the assembly.
    /// </summary>
    /// <typeparam name="T">The type of data to extract.</typeparam>
    /// <param name="fileName">The name of the file from which to extract data.</param>
    /// <returns>The extracted data of type <typeparamref name="T"/>.</returns>
    public static async Task<T?> ExtractFromAssemblyAsync<T>(string fileName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var jsonName = assembly.GetManifestResourceNames()
            .SingleOrDefault(s => s.EndsWith(fileName, StringComparison.OrdinalIgnoreCase)) ?? "";
        await using var stream = assembly.GetManifestResourceStream(jsonName);
        using var reader = new StreamReader(stream);
        var result = await reader.ReadToEndAsync();
        return JsonSerializer.Deserialize<T>(result);
    }
    /// <summary>
    /// Extracts data of type <typeparamref name="T"/> from the assembly.
    /// </summary>
    /// <typeparam name="T">The type of data to extract.</typeparam>
    /// <param name="fileName">The name of the file from which to extract data.</param>
    /// <returns>The extracted data of type <typeparamref name="T"/>.</returns>
    public static T? ExtractFromAssembly<T>(string fileName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var jsonName = assembly.GetManifestResourceNames()
            .SingleOrDefault(s => s.EndsWith(fileName, StringComparison.OrdinalIgnoreCase)) ?? "";
        using var stream = assembly.GetManifestResourceStream(jsonName);
        using var reader = new StreamReader(stream);
        object result = reader.ReadToEnd();
        if (typeof(T) == typeof(string))
            return (T)result;
        var json = result.ToString();
        return string.IsNullOrEmpty(json) ? default : JsonSerializer.Deserialize<T>(json);
    }
    /// <summary>
    /// Registers an output message hook for the <see cref="agent"/>.
    /// </summary>
    /// <param name="agent">The <see cref="messageDelegate"/> instance.</param>
    /// <param name="messageDelegate">The delegate to be invoked when a message is received.</param>
    /// <returns>The modified <see cref="IAgent"/> instance.</returns>
    public static MiddlewareAgent<TAgent> RegisterOutputMessageHook<TAgent>(this TAgent agent, Action<IMessage> messageDelegate) where TAgent : IAgent
    {
        
        return agent.RegisterPostProcess((conversation, reply, ct) =>
        {
            messageDelegate.Invoke(reply);
            Console.WriteLine(reply.MessageFormatted().Replace(Separator, "\n\n"));
            return Task.FromResult(reply);
        });
    }
    public static MiddlewareAgent<IAgent> RegisterOutputMessageHook<TAgent>(this MiddlewareAgent<TAgent> agent, Action<IMessage> messageDelegate) where TAgent : IAgent
    {
        
        MiddlewareAgent<IAgent> regAgent = agent.Agent.RegisterPostProcess((conversation, reply, ct) =>
        {
            messageDelegate.Invoke(reply);
            Console.WriteLine(reply.MessageFormatted().Replace(Separator, "\n\n"));
            return Task.FromResult(reply);
        });
        return regAgent; // Compilation error here
    }
    public static MiddlewareAgent<TAgent> RegisterHumanInput<TAgent>(this TAgent agent, Func<Task<string?>> humanInputDelegate) where TAgent : IAgent
    {
        var middleware = new InteractiveMiddleware();
        var middlewareAgent = new MiddlewareAgent<TAgent>(agent);
        middlewareAgent.Use(middleware);
        return middlewareAgent;
    }
    public static MiddlewareAgent<TAgent> RegisterHumanInput<TAgent>(this MiddlewareAgent<TAgent> agent, Func<Task<string>> humanInputDelegate) where TAgent : IAgent
    {
        var middleware = new InteractiveMiddleware();
        var middlewareAgent = new MiddlewareAgent<TAgent>(agent);
        middlewareAgent.Use(middleware);
        return middlewareAgent;
    }
    private const string Separator = "<hr/>";
    private const string LineBreak = "<br/>";
    /// <summary>
    /// Formats the message.
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public static string MessageFormatted(this IMessage message)
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine($"{message.From}:");
        stringBuilder.AppendLine(LineBreak);
        stringBuilder.AppendLine(message.GetContent());
        stringBuilder.AppendLine(Separator);
        return stringBuilder.ToString();
    }
   
    /// <summary>
    /// Get the value of the <typeparamref name="TEnum"/> enum's <see cref="T:System.ComponentModel.DescriptionAttribute" />.
    /// </summary>
    /// <param name="value"></param>
    /// <typeparam name="TEnum"></typeparam>
    /// <returns></returns>
    public static string ToDescription<TEnum>(this TEnum value) where TEnum : Enum
    {
        var type = value.GetType();
        var name = Enum.GetName(type, value);
        var descriptionAttribute = (DescriptionAttribute)type.GetField(name).GetCustomAttributes(typeof(DescriptionAttribute), false)[0];
        return descriptionAttribute?.Description ?? value.ToString();
    }
    /// <summary>
    /// Get the value of the <typeparamref name="TEnum"/> enum's <see cref="T:AutoGenDotNet.Models.TempAttribute" />.
    /// </summary>
    /// <param name="value"></param>
    /// <typeparam name="TEnum"></typeparam>
    /// <returns>a float between 0 and 2</returns>
    /// <exception cref="Exception"></exception>
    public static float ToTemperature<TEnum>(this TEnum value) where TEnum : Enum
    {
        var member = value.GetType().GetMember(value.ToString()).FirstOrDefault();

        if (member != null)
        {
            if (member.GetCustomAttributes(typeof(TempAttribute), false).FirstOrDefault() is TempAttribute attribute)
            {
                return attribute.Temperature;
            }
        }

        throw new Exception($"No TempAttribute found for enum value {value}");
    }
    /// <summary>
    /// Gets the number of tokens in the specified text.
    /// </summary>
    /// <param name="text">The text to count tokens in.</param>
    /// <returns>The number of tokens in the text.</returns>
    public static int TokenCount(this string text)
    {
        _tokenizer ??= Encoding.ForModel("gpt-3.5-turbo");
        return _tokenizer.CountTokens(text);
    }
}