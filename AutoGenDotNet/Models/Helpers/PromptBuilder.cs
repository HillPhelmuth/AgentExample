using Microsoft.SemanticKernel;

namespace AutoGenDotNet.Models.Helpers;

/// <summary>
/// Represents a builder for generating prompts.
/// </summary>
public class PromptBuilder
{
    private static class Variables
    {
        public const string BotName = "name";
        public const string PersonalityType = "personalityType";
        public const string PersonalityTraits = "personalityTraits";
        public const string Description = "description";
        public const string PersonalityType2 = "personalityType2";
        public const string PersonalityTraits2 = "personalityTraits2";
    }

    /// <summary>
    /// Generates a standard system prompt using the provided bot model.
    /// </summary>
    /// <param name="bot">The bot model.</param>
    /// <returns>The generated prompt.</returns>
    public static async Task<string> GeneratePrompt(BotModel bot)
    {
        var kernel = Kernel.CreateBuilder().Build();
        var kernelArgs = new KernelArguments
        {
            [Variables.BotName] = bot.Name,
            [Variables.PersonalityType] = bot.Personality.ToString(),
            [Variables.PersonalityTraits] = bot.Personality.ToDescription(),
            [Variables.Description] = bot.Description,
            [Variables.PersonalityType2] = bot.SecondaryPersonality.ToString(),
            [Variables.PersonalityTraits2] = bot.SecondaryPersonality.ToDescription()
        };
        var templateText = StaticHelpers.ExtractFromAssembly<string>("StandardSystemPromptTemplate.txt");
        var promptFactory = new KernelPromptTemplateFactory();
        var templateConfig = new PromptTemplateConfig(templateText);
        var prompt = await promptFactory.Create(templateConfig).RenderAsync(kernel, kernelArgs);
        Console.WriteLine($"Prompt Generated:\n{prompt}");
        return prompt;
    }

    /// <summary>
    /// Generates a system prompt using the provided template text and variables.
    /// </summary>
    /// <param name="templateText">The template text.</param>
    /// <param name="variables">The variables.</param>
    /// <returns>The generated prompt.</returns>
    public static async Task<string> GenerateSystemPrompt(string templateText, Dictionary<string, string> variables)
    {
        var kernel = Kernel.CreateBuilder().Build();
        var kernelArgs = new KernelArguments();
        foreach (var variable in variables)
        {
            kernelArgs[variable.Key] = variable.Value;
        }
        var promptFactory = new KernelPromptTemplateFactory();
        var templateConfig = new PromptTemplateConfig(templateText);
        var prompt = await promptFactory.Create(templateConfig).RenderAsync(kernel, kernelArgs);
        Console.WriteLine($"Prompt Generated:\n{prompt}");
        return prompt;
    }
}