using AgentExample.SharedServices;
using AgentExample.SharedServices.Models;
using AutoGen.Core;
using Microsoft.SemanticKernel;

namespace AutoGenDotNet.Functions.Summarizer
{
    public partial class SummerizeFunctions
    {
        /// <summary>
        /// Summarize given text or any text document
        /// </summary>
        /// <param name="text">Text to summarize</param>
        /// <returns>generated summary</returns>
        [Function]
        public async Task<string> SummarizeText(string text)
        {
            var kernel = Kernel.CreateBuilder().AddOpenAIChatCompletion(TestConfiguration.OpenAI.ModelId, TestConfiguration.OpenAI.ApiKey).Build();
            var plugin = kernel.ImportPluginFromYaml("SummarizePlugin");
            var args = new KernelArguments() { ["input"] = text };
            var result = await kernel.InvokeAsync(plugin["Summarize"], args);
            return result.GetValue<string>()!;
        }
        /// <summary>
        ///  Automatically generate compact notes for any text or text document.
        /// </summary>
        /// <param name="text">Text from which to generate notes</param>
        /// <returns>generated notes</returns>
        [Function]
        public async Task<string> GenerateNotes(string text)
        {
            var kernel = Kernel.CreateBuilder().AddOpenAIChatCompletion(TestConfiguration.OpenAI.ModelId, TestConfiguration.OpenAI.ApiKey).Build();
            var plugin = kernel.ImportPluginFromYaml("SummarizePlugin");
            var args = new KernelArguments() { ["input"] = text };
            var result = await kernel.InvokeAsync(plugin["Notegen"], args);
            return result.GetValue<string>()!;
        }
    }
}
