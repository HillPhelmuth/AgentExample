using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Runtime.CompilerServices;
using Microsoft.SemanticKernel.Plugins.OpenApi;
using System.Reflection;
using AgentExample.Plugins.TaxExpertPlugin;
using System.Text;

namespace AgentExample.Services
{
    public class AgentRunnerService(IConfiguration configuration)
    {
        private readonly ChatHistory _chatHistory = [];
        public event Action<string>? SendMessage;
        public event Action? ChatReset;
        private const string AdvisorPromptTemplate = """
                                                     Advise clients on tax filing, taxes and financial matters.
                                                     First, Advise them based on their provided refund/owe status and amount.
                                                     You will need the clients userName first. Use that to get their refund status and amount.
                                                     If you aren't provided with any information you require, politely request it.
                                                     Once you have given clients thier refund information, seek tax expertise to advise them.
                                                     """;
        private static readonly string PluginPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Directory.GetCurrentDirectory(), "Plugins");
        private static readonly List<KernelPlugin> Plugins = [];

        public async IAsyncEnumerable<string> ChatStream(string input, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var kernel = CreateKernel();
            var plugins = await GetPlugins();
            plugins.ForEach(kernel.Plugins.Add);

            await foreach (var p in ExecuteChatStream(input, kernel, cancellationToken)) yield return p;
        }

        private async IAsyncEnumerable<string> ExecuteChatStream(string input, Kernel kernel,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            kernel.FunctionInvoked += FunctionInvokedHandler;
            kernel.FunctionInvoking += FunctionInvokingHandler;
            var settings = new OpenAIPromptExecutionSettings() { ChatSystemPrompt = AdvisorPromptTemplate, ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions, MaxTokens = 512 };
            var chat = kernel.GetRequiredService<IChatCompletionService>();
            _chatHistory.AddSystemMessage(AdvisorPromptTemplate);
            if (!string.IsNullOrWhiteSpace(input))
                _chatHistory.AddUserMessage(input);

            var sb = new StringBuilder();
            await foreach (var update in chat.GetStreamingChatMessageContentsAsync(_chatHistory, settings, kernel, cancellationToken))
            {
                if (update.Content is null) continue;
                sb.Append(update.Content);
                yield return update.Content;

            }
            _chatHistory.AddAssistantMessage(sb.ToString());
        }


        public void Reset()
        {
            _chatHistory.Clear();
            ChatReset?.Invoke();
        }
        private static Dictionary<string, List<string>> _pluginFunctions = [];
        public static async Task<List<KernelPlugin>> GetActivePlugins()
        {
            return Plugins.Count > 0 ? Plugins : await GetPlugins();
        }

        private static async Task<List<KernelPlugin>> GetPlugins()
        {
            var kernel = Kernel.CreateBuilder().Build();
            var refundPlugin = await kernel.ImportPluginFromOpenApiAsync("Refund", Path.Combine(PluginPath, "ExternalServiceExamplePlugin", "openapi.json"));

            var taxExpertPlugin = kernel.ImportPluginFromType<TaxExpert>("TaxExpert");
            var summarizePlugin = kernel.ImportPluginFromPromptDirectory(Path.Combine(PluginPath, "SummarizePlugin"), "Summarizer");
            List<KernelPlugin> plugins = [refundPlugin, taxExpertPlugin, summarizePlugin];
            _pluginFunctions = plugins.ToDictionary(x => x.Name, x => x.Select(y => y.Name).ToList());
            return plugins;
        }

        private static string GetPluginFromFunctionName(string functionName) => _pluginFunctions.Keys.FirstOrDefault(x => _pluginFunctions[x].Contains(functionName)) ?? functionName;

        private static Kernel CreateKernel()
        {
            var kernelBuilder = Kernel.CreateBuilder();
            kernelBuilder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Information));
            // For using OpenAI
            kernelBuilder.AddOpenAIChatCompletion(TestConfiguration.OpenAI!.ChatModelId, TestConfiguration.OpenAI.ApiKey);
            // For using Azure OpenAI
            //kernelBuilder.AddAzureOpenAIChatCompletion(TestConfiguration.AzureOpenAI!.ChatDeploymentName,TestConfiguration.AzureOpenAI.Endpoint, TestConfiguration.AzureOpenAI.ApiKey);
            var kernel = kernelBuilder.Build();
            return kernel;
        }
        private void FunctionInvokingHandler(object? sender, FunctionInvokingEventArgs e)
        {
            var function = e.Function;
            var originalValues = e.Metadata;
            var plugin = GetPluginFromFunctionName(e.Function.Name);
            SendMessage?.Invoke($"<div style=\"font-size:110%\">Consulting <em>{plugin}</em> Agent</div>");
            Console.WriteLine($"Executing {function.Metadata.PluginName}.{function.Name}.\nMetadata:\n{string.Join("\n", originalValues?.Select(x => $"Name: {x.Key}, Value: {x.Value}") ?? new List<string>())}");
        }

        private void FunctionInvokedHandler(object? sender, FunctionInvokedEventArgs e)
        {
            var function = e.Function;
            var originalValues = e.Arguments;
            var result = $"<p>{e.Result}</p>";
            var resultsExpando = $"""

                                  <details>
                                    <summary>See Results</summary>
                                    
                                    <h5>Results</h5>
                                    <p>
                                    {result}
                                    </p>
                                    <br/>
                                  </details>
                                  """;
            SendMessage?.Invoke(resultsExpando);
            Console.WriteLine($"Executed {function.Metadata.PluginName}.{function.Name}.\nVariables:\n{string.Join("\n", originalValues?.Select(x => $"Name: {x.Key}, Value: {x.Value}") ?? new List<string>())}\n\n-----------------Result:\n{e.Result.GetValue<object>()}\n-------------------");
        }
    }
}
