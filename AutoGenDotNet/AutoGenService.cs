using AutoGen;
using AutoGen.OpenAI;
using AutoGenDotNet.Models;
using Microsoft.Extensions.Configuration;
using System.Text;
using AutoGenDotNet.Models.AgentClasses;
using AutoGen.DotnetInteractive;
using AutoGen.Core;
using AutoGen.SemanticKernel.Extension;
using AutoGenDotNet.Models.Helpers;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel;
using AgentExample.SharedServices.Plugins.TaxExpertPlugin;
using AgentExample.SharedServices;
using AgentExample.SharedServices.Models;
using static AutoGenDotNet.PromptConstants;
using Microsoft.Extensions.Logging;
using ConsoleLogger = AgentExample.SharedServices.Models.ConsoleLogger;
using AutoGenDotNet.Services;
using AutoGenDotNet.Functions.ResearchFunctions;
using AutoGen.SemanticKernel;
using Microsoft.Extensions.DependencyInjection;
using Azure.AI.OpenAI;
using AgentExample.SharedServices.Plugins.FormPlugin;

namespace AutoGenDotNet;

/// <summary>
/// Example service using the pre-pre-preview version of .net AutoGen
/// </summary>
public class AutoGenService
{
    // ReSharper disable once ReplaceWithPrimaryConstructorParameter
    private readonly IConfiguration _configuration;

    #region Events
    /// <summary>
    /// Occurs when a message is received.
    /// </summary>
    public event Action<IMessage>? OnMessage;
    /// <summary>
    /// Occurs when a conversation is complete.
    /// </summary>
    public event Action<string>? OnConversationComplete;
    /// <summary>
    /// Occurs when an agent requests input.
    /// </summary>
    public event AgentRequestEventHandler? RequestInput;
    public event Action<FakeForm>? FakeFormUpdate;

    #endregion


    private List<IAgent> _activeAgents = [];
    private Dictionary<string, string> _agentOpeners = [];
    private ILogger<AutoGenService> _logger;
    private List<IMessage> _activeMessages = [];

    private PopulateAndSaveForm _populateAndSaveForm = new();
    //private readonly BingWebSearchService _bingWebSearchService;

    /// <summary>
    /// Example service using the pre-pre-preview version of .net AutoGen
    /// </summary>
    /// <param name="configuration"></param>
    public AutoGenService(IConfiguration configuration/*, BingWebSearchService bingWebSearchService*/)
    {
        _configuration = configuration;
        //_bingWebSearchService = bingWebSearchService;
        var loggerFactory = ConsoleLogger.LoggerFactory;
        _logger = loggerFactory.CreateLogger<AutoGenService>();
    }

    private void SendMessage(IMessage message)
    {
        _logger.LogInformation("SendMessage triggered from {formattedMessage}", message.From);
        _activeMessages.Add(message);
        OnMessage?.Invoke(message);
    }
    private void SendRequestInput(object? agent, AgentRequestEventArgs args)
    {
        Console.WriteLine($"Request Input Event Triggered from {args.Agent.Name}");
        RequestInput?.Invoke(agent, args);
    }
    private Task<bool> Terminate(IEnumerable<IMessage> messages, CancellationToken token)
    {
        var lastMessage = messages.LastOrDefault();
        if (lastMessage is null) return Task.FromResult(false);
        return Task.FromResult(lastMessage.From == "user" && lastMessage.GetContent() == "stop");
    }
    /// <summary>
    /// Generates the agents to use in dynamic group chat.
    /// </summary>
    /// <param name="botModels">Participant Agent Personas</param>
    /// <exception cref="Exception"></exception>
    public async Task GenerateAgents(List<BotModel> botModels)
    {
        _activeMessages.Clear();
        var openAIKey = _configuration["OpenAI:ApiKey"] ?? throw new Exception("Please set 'OpenAI:ApiKey' environment variable.");
        List<ILLMConfig> llmConfig = [new OpenAIConfig(openAIKey, "gpt-3.5-turbo-0125")];
        var userProxyAgent = new InteractiveUserProxyAgent(
            name: "user",
            humanInputMode: HumanInputMode.ALWAYS, isTermination: Terminate);
        userProxyAgent.RequestInput += SendRequestInput;

        foreach (var bot in botModels)
        {
            var config = new ConversableAgentConfig { Temperature = bot.Personality.ToTemperature(), ConfigList = llmConfig };
            var systemMessage = await bot.GenerateBotPrompt();
            var interactiveConversableAgent = new InteractiveConversableAgent(bot.Name, systemMessage, config, humanInputMode: HumanInputMode.NEVER);
            interactiveConversableAgent.RequestInput += SendRequestInput;
            var agent = interactiveConversableAgent.RegisterOutputMessageHook(SendMessage);

            _activeAgents.Add(agent);
            if (!string.IsNullOrEmpty(bot.OpeningPrompt))
            {
                _agentOpeners[bot.Name] = bot.OpeningPrompt;
            }
        }
        var userAgent = userProxyAgent.RegisterHumanInput(userProxyAgent.GetHumanInputAsync!);

        _activeAgents.Add(userAgent);
    }

    /// <summary>
    /// Starts the dynamic group chat with selected agents
    /// </summary>
    /// <param name="input">Open input for group chat</param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="Exception"></exception>
    public async Task StartGroupChat(string input, CancellationToken cancellationToken = default)
    {
        var openAIKey = _configuration["OpenAI:ApiKey"] ?? throw new Exception("Please set OPENAI_API_KEY environment variable.");
        List<ILLMConfig> llmConfig = [new OpenAIConfig(openAIKey, "gpt-3.5-turbo-0125")];
        var config = new ConversableAgentConfig { Temperature = .8f, ConfigList = llmConfig };
        var interactiveAssistantAgent = new InteractiveGptAgent("admin", AdminPrompt, new OpenAIConfig(openAIKey, "gpt-3.5-turbo-0125"), temp: 0.8f);
        interactiveAssistantAgent.RequestInput += SendRequestInput;
        var admin = interactiveAssistantAgent.RegisterOutputMessageHook(SendMessage);

        _activeAgents.Add(admin);
        var groupChat = new GroupChat(_activeAgents, admin);
        foreach (var opener in _agentOpeners)
        {
            var agent = _activeAgents.Find(a => a.Name == opener.Key);
            agent?.AddInitializeMessage(opener.Value, groupChat);
        }
        var groupChatManager = new GroupChatManager(groupChat);
        var chat = await admin.InitiateChatAsync(groupChatManager, input, 30, ct: cancellationToken);
        var sb = new StringBuilder();
        foreach (var item in chat)
        {
            sb.AppendLine(item.FormatMessage());
        }
        OnConversationComplete?.Invoke(sb.ToString());
    }

    /// <summary>
    /// Starts the Software Development Group Chat.
    /// </summary>
    /// <param name="request">The software project request</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <exception cref="Exception"></exception>
    public async Task RunDotnetInteractiveWorkflowGroupChat(string request = "Retrieve the most recent pr from mlnet and save it in result.txt", CancellationToken cancellationToken = default)
    {
        _activeMessages.Clear();

        // setup dotnet interactive
        var workDir = Path.Combine(Path.GetTempPath(), "InteractiveService");
        if (!Directory.Exists(workDir))
            Directory.CreateDirectory(workDir);

        var service = new InteractiveService(workDir);
        var dotnetInteractiveFunctions = new DotnetInteractiveFunction(service);

        var result = Path.Combine(workDir, "result.txt");
        if (File.Exists(result))
            File.Delete(result);

        await service.StartAsync(workDir, cancellationToken);

        // get OpenAI Key and create config
        var openAIKey = _configuration["OpenAI:ApiKey"] ?? throw new Exception("Please set OPENAI_API_KEY environment variable.");
        //var gptConfig = LLMConfigAPI.GetOpenAIConfigList(openAIKey, ["gpt-4-turbo-preview"]);

        var groupAdmin = new InteractiveGptAgent(
            name: "groupAdmin",
            llmConfig: OpenAiGptConfig.GetOpenAIGPT4(openAIKey));

        var userProxyAgent = new InteractiveUserProxyAgent(name: "user", defaultReply: GroupChatExtension.TERMINATE);
        userProxyAgent.RequestInput += SendRequestInput;
        userProxyAgent.RegisterOutputMessageHook(SendMessage);
        var userProxy = userProxyAgent.RegisterHumanInput(userProxyAgent.GetHumanInputAsync!);


        // Create admin agent
        var admin = new InteractiveGptAgent(
            name: "admin",
            systemMessage: ProjectManagerSystemMessage,
            llmConfig: OpenAiGptConfig.GetOpenAIGPT4(openAIKey), temp: 0.1f)
            .RegisterOutputMessageHook(SendMessage);

        // create coder agent
        // The coder agent is a composite agent that contains dotnet coder, code reviewer and nuget agent.
        // The dotnet coder write dotnet code to resolve the task.
        // The code reviewer review the code block from coder's reply.
        // The nuget agent install nuget packages if there's any.
        var coderAgent = new InteractiveGptAgent(
            name: "coder",
            systemMessage: CoderSystemMessage,
            llmConfig: OpenAiGptConfig.GetOpenAIGPT4(openAIKey), temp: 0.4f)
            .RegisterPrintFormatMessageHook().RegisterOutputMessageHook(SendMessage);

        // code reviewer agent will review if code block from coder's reply satisfy the following conditions:
        // - There's only one code block
        // - The code block is csharp code block
        // - The code block is top level statement
        // - The code block is not using declaration

        var codeReviewAgent = new InteractiveGptAgent(
            name: "reviewer",
            systemMessage: ReviewerSystemMessage,
            llmConfig: OpenAiGptConfig.GetOpenAIGPT4(openAIKey), temp: 0)
            .RegisterPrintFormatMessageHook().RegisterOutputMessageHook(SendMessage);

        // create runner agent
        // The runner agent will run the code block from coder's reply.
        // It runs dotnet code using dotnet interactive service hook.
        // It also truncate the output if the output is too long.
        var runner = new InteractiveGptAgent(
            name: "runner",
            llmConfig: OpenAiGptConfig.GetOpenAIGPT4(openAIKey), temp: 0)
            .RegisterDotnetCodeBlockExectionHook(interactiveService: service)
            .RegisterMiddleware(async (msgs, option, agent, ct) =>
            {
                var mostRecentCoderMessage = msgs.LastOrDefault(x => x.From == "coder") ?? throw new Exception("No coder message found");
                return await agent.GenerateReplyAsync([mostRecentCoderMessage], option, ct);
            })
            .RegisterPrintFormatMessageHook().RegisterOutputMessageHook(SendMessage);
        var transitionsByName = GetCodingTransitionsByName(admin, coderAgent, codeReviewAgent, runner, userProxy);

        var workflow = new Graph([.. transitionsByName.Values]);

        // create group chat
        var groupChat = new GroupChat(
            admin: groupAdmin,
            members: [admin, coderAgent, runner, codeReviewAgent, userProxy],
            workflow: workflow);

        // task 1: retrieve the most recent pr from mlnet and save it in result.txt
        var groupChatManager = new GroupChatManager(groupChat);
        var chat = await userProxy.SendAsync(groupChatManager, request, maxRound: 30, ct: cancellationToken);
        //File.Exists(result).Should().BeTrue();
        var sb = new StringBuilder();
        foreach (var item in chat)
        {
            sb.AppendLine(item.FormatMessage());
        }
        OnConversationComplete?.Invoke(sb.ToString());

    }

    /// <summary>
    /// Starts the essay research writer agent group.
    /// </summary>
    /// <param name="topic">The topic of the essay.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The list of messages exchanged in the group chat.</returns>
    public async Task<List<IMessage>> StartEssayResearchWriterAgentGroup(string topic, CancellationToken cancellationToken = default)
    {
        _activeMessages.Clear();
        var openAIKey = _configuration["OpenAI:ApiKey"] ?? throw new Exception("Please set OPENAI_API_KEY environment variable.");

        var groupAdmin = new InteractiveGptAgent(
            name: "groupAdmin",
            llmConfig: OpenAiGptConfig.GetOpenAIGPT3_5_Turbo(openAIKey));

        var userProxyAgent = new InteractiveUserProxyAgent(name: "user", defaultReply: GroupChatExtension.TERMINATE);
        userProxyAgent.RequestInput += SendRequestInput;
        userProxyAgent.RegisterOutputMessageHook(SendMessage);
        var userProxy = userProxyAgent.RegisterHumanInput(userProxyAgent.GetHumanInputAsync!);
        var admin = new InteractiveGptAgent(
                name: "admin",
                systemMessage: WriterEditorPrompt,
                llmConfig: OpenAiGptConfig.GetOpenAIGPT4(openAIKey), temp: 0.1f)
             .RegisterOutputMessageHook(SendMessage)
             .RegisterPostProcess(async (_, reply, _) =>
             {
                 Console.WriteLine($"RegisterPostProcess Triggered for {reply.From}\n\n{reply.GetContent()}");
                 var content = reply.GetContent();

                 if (content.Contains("```essay"))
                     content = $"{content}\n\n {GroupChatExtension.TERMINATE}";

                 return new TextMessage(Role.Assistant, content, from: reply.From);

             });
        //var externalResearch = new ExternalResearchFunctions(_bingWebSearchService);
        //var researchAgent = new InteractiveGptAgent(name:"researcher",
        //    systemMessage: "Use the available tools to search the web, youtube and Medium for information",
        //    llmConfig: OpenAiGptConfig.GetOpenAIGPT3_5_Turbo(openAIKey),
        //    temp: 0.4f,
        //    functionDefinitions: [externalResearch.SearchAndCiteWebFunction, externalResearch.SearchAndCiteYoutubeFunction, externalResearch.ExtractWebSearchQueryFunction])
        //    .RegisterOutputMessageHook(SendMessage); 
        var builder = Kernel.CreateBuilder()
            .AddOpenAIChatCompletion(TestConfiguration.OpenAI.ModelId, TestConfiguration.OpenAI.ApiKey);
        builder.Services.AddBing();
        builder.Services.AddSingleton(_configuration);
        var kernel = builder.Build();
        var settings = new OpenAIPromptExecutionSettings
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
            Temperature = 0.4f
        };
        kernel.Plugins.AddFromType<ExternalResearchFunctions>(serviceProvider: kernel.Services);
        await kernel.ImportPluginFromOpenApi("Medium");
        kernel.FunctionInvoking += (sender, args) =>
        {
            _logger.LogInformation("\n\nFunction {FunctionName} is being invoked\n\n", args.Function.Name);
        };
        kernel.FunctionInvoked += (sender, args) =>
        {
            var result = args.Result.GetValue<string>();
            result = result.Length <= 300 ? result : $"{result[..300]}...";
            _logger.LogInformation("Function {FunctionName} invoked with result:\n\n{result}\n\n", args.Function.Name, result);
        };
        var skAgent = kernel
            .ToSemanticKernelAgent("researcher", "Use the available tools to search the web, youtube and Medium for information", settings);
        var connector = new SemanticKernelChatMessageContentConnector();
        var researchAgent = skAgent
            .RegisterStreamingMiddleware(connector)
            .RegisterMiddleware(connector)
            .RegisterOutputMessageHook(SendMessage).RegisterPreProcess(async (messages, _) =>
            {
                var reply = messages.Last();
                var modified = messages.ToList();
                var content = reply.GetContent();
                if (!content.Contains("```task")) return messages;
                var taskIndex = content.IndexOf("```task");
                content = content[taskIndex..];
                var replace = new TextMessage(Role.Assistant, content, reply.From);
                modified.RemoveAt(modified.Count - 1);
                modified.Add(replace);
                return modified;
            });

        var writerAgent = new InteractiveGptAgent(
                name: "writer",
                systemMessage: EssayWriterPrompt,
                llmConfig: OpenAiGptConfig.GetOpenAIGPT3_5_Turbo(openAIKey), temp: 0.7f)
            .RegisterOutputMessageHook(SendMessage);

        var transitionsByName = GetWritingTransitionsByName2(admin, researchAgent, writerAgent, userProxy);
        var workflow = new Workflow([.. transitionsByName.Values]);

        // create group chat
        var groupChat = new GroupChat(
            admin: groupAdmin,
            members: [admin, researchAgent, writerAgent, userProxy],
            workflow: workflow);

        var groupChatManager = new GroupChatManager(groupChat);
        var msgs = await userProxy.InitiateChatAsync(groupChatManager, $"Write a well researched essay on {topic}", maxRound: 10, ct: cancellationToken);
        var summaryBuilder = new StringBuilder();
        foreach (var msg in msgs)
        {
            summaryBuilder.AppendLine(msg.FormatMessage());
        }
        OnConversationComplete?.Invoke($"## Conversation complete\n\n{summaryBuilder}");
        return msgs.ToList();
    }
    public IAgent CreateSaveProgressAgent()
    {
        var builder = Kernel.CreateBuilder()
            .AddOpenAIChatCompletion(TestConfiguration.OpenAI.ChatModelId, TestConfiguration.OpenAI.ApiKey);
        var kernel = builder.Build();
        kernel.Plugins.AddFromObject(_populateAndSaveForm);
        var settings = new OpenAIPromptExecutionSettings
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
            Temperature = 0.1f,
            ChatSystemPrompt = """
                               Use the tools available to save user information. Your response must always include all information already added to the form AND all information still missing.
                               If all required information has beeen provided respond with "Application information is saved to database." and then stop.
                               """
        };
        kernel.FunctionInvoking += (sender, args) =>
        {
            _logger.LogInformation("\n\nFunction {FunctionName} is being invoked with arguments:{args}\n\n", args.Function.Name, string.Join(", ", args.Arguments.Select(x => $"{x.Key}: {x.Value}")));
        };
        kernel.FunctionInvoked += (sender, args) =>
        {
            var result = args.Result.GetValue<string>();
            result = result.Length <= 300 ? result : $"{result[..300]}...";
            _logger.LogInformation("Function {FunctionName} invoked with result:\n\n{result}\n\n", args.Function.Name, result);
        };
        var skAgent = kernel
            .ToSemanticKernelAgent("application", settings.ChatSystemPrompt, settings);
        var connector = new SemanticKernelChatMessageContentConnector();
        var chatAgent = skAgent
            .RegisterStreamingMiddleware(connector)
            .RegisterMiddleware(connector)
            .RegisterOutputMessageHook(SendMessage)
            .RegisterMiddleware(async (msgs, option, agent, ct) =>
            {
                var lastUserMessage = msgs.Last() ?? throw new Exception("No user message found.");
                var prompt = $"""
                Save progress according to the most recent information provided by user using the tools available.
                Your response must always include all information already added to the form AND all information still missing.
                If all required information has beeen provided respond with "Application information is saved to database." and then stop.
                ```user
                {lastUserMessage.GetContent()}
                ```
                """;
                var message = new TextMessage(lastUserMessage.GetRole().Value, prompt, lastUserMessage.From);
                return await agent.GenerateReplyAsync([message], option, ct);

            });


        return chatAgent;
    }

    public IAgent CreateAssistantAgent()
    {
        var openaiMessageConnector = new OpenAIChatRequestMessageConnector();
        var chatAgent = new InteractiveGptAgent(
            name: "assistant",
            llmConfig: OpenAiGptConfig.GetOpenAIGPT3_5_Turbo(_configuration["OpenAI:ApiKey"]),
            systemMessage: "You create polite prompt to ask user provide missing information. Ask for only one piece of information at a time.")
            .RegisterPreProcess(async (msgs, ct) =>
            {
                var lastReply = msgs.Last() ?? throw new Exception("No reply found.");
                var messages = msgs.ToList();
                var content = lastReply.GetContent();
                if (content?.Contains("Application information is saved to database.") == true)
                {
                    content = $"{content}\n{GroupChatExtension.TERMINATE}";
                }
                var newMsg = new TextMessage(Role.Assistant, content, lastReply.From);
                messages.RemoveAt(messages.Count - 1);
                messages.Add(newMsg);
                return messages;

            }).RegisterOutputMessageHook(SendMessage);

        return chatAgent;
    }

    public IAgent CreateUserAgent()
    {
        var openAIKey = _configuration["OpenAI:ApiKey"] ?? throw new Exception("Please set OPENAI_API_KEY environment variable.");

        var chatAgent = new InteractiveGptAgent(
                name: "user",
                llmConfig: OpenAiGptConfig.GetOpenAIGPT3_5_Turbo(openAIKey),
            systemMessage: """
            You are a user who is filling an application form. Simply provide the information as requested and answer the questions, don't do anything else.
            Be sure to indicate the question you're answering as you answer it. For example, if asked for your name, respond with "My name is John Doe"
            here's some personal information about you:
            - name: John Doe
            - email: MyEmail@gmail.com
            - phone: 123-456-7890
            - address: 1234 Main St Redmont, WA 98052
            - want to receive update? true
            """)
            .RegisterPrintFormatMessageHook().RegisterOutputMessageHook(SendMessage);

        return chatAgent;
    }

    public async Task ExecuteFormBuilderAgentGroup(CancellationToken token = default)
    {
        _populateAndSaveForm = new PopulateAndSaveForm();
        _populateAndSaveForm.FormChanged += (frm) => FakeFormUpdate?.Invoke(frm);
        var applicationAgent = CreateSaveProgressAgent();
        var assistantAgent = CreateAssistantAgent();
        var userAgent = CreateUserAgent();

        var userToApplicationTransition = Transition.Create(userAgent, applicationAgent);
        var applicationToAssistantTransition = Transition.Create(applicationAgent, assistantAgent);
        var assistantToUserTransition = Transition.Create(assistantAgent, userAgent);

        var workflow = new Graph(
            [
                userToApplicationTransition,
                applicationToAssistantTransition,
                assistantToUserTransition,
            ]);

        var groupChat = new GroupChat(
            members: [userAgent, applicationAgent, assistantAgent],
            workflow: workflow);

        var groupChatManager = new GroupChatManager(groupChat);
        var initialMessage = await assistantAgent.SendAsync("Generate a greeting meesage for user and start the conversation by asking what's their name.", ct: token);

        var chatHistory = await userAgent.SendAsync(groupChatManager, [initialMessage], maxRound: 20, ct: token);
        var summaryBuilder = new StringBuilder();
        foreach (var msg in chatHistory)
        {
            summaryBuilder.AppendLine(msg.FormatMessage());
        }
        OnConversationComplete?.Invoke($"## Conversation complete\n\n{summaryBuilder}");

    }
    private static Dictionary<string, Transition> GetWritingTransitionsByName(IAgent admin, IAgent researcher, IAgent writer, IAgent user)
    {
        var transitionsByName = new Dictionary<string, Transition>
        {
            ["adminToResearcher"] = Transition.Create(admin, researcher),
            ["researcherToWriter"] = Transition.Create(researcher, writer),
            ["adminToWriter"] = Transition.Create(admin, writer),
            ["writerToAdmin"] = Transition.Create(writer, admin),
            ["adminToUser"] = Transition.Create(admin, user),
            ["userToAdmin"] = Transition.Create(user, admin)
        };
        return transitionsByName;
    }
    private static Dictionary<string, Transition> GetWritingTransitionsByName2(IAgent admin, IAgent researcher, IAgent writer, IAgent user)
    {
        var transitionsByName = new Dictionary<string, Transition>
        {
            ["userToAdmin"] = Transition.Create(user, admin),
            ["adminToResearcher"] = Transition.Create(admin, researcher),
            ["researcherToAdmin"] = Transition.Create(researcher, admin),
            ["adminToWriter"] = Transition.Create(admin, writer),
            ["writerToAdmin"] = Transition.Create(writer, admin),
            ["adminToUser"] = Transition.Create(admin, user)
        };
        return transitionsByName;
    }
    private static Dictionary<string, Transition> GetCodingTransitionsByName(IAgent admin, IAgent coderAgent, IAgent codeReviewAgent, IAgent runner,
        IAgent userProxy)
    {
        var transitionsByName = new Dictionary<string, Transition>
        {
            // Each task starts here
            ["adminToCoder"] = Transition.Create(admin, coderAgent, async (from, to, messages) =>
            {
                // the last message should be from admin
                var lastMessage = messages.Last();
                if (lastMessage.From != admin.Name)
                {
                    return false;
                }

                return true;
            }),
            ["coderToReviewer"] = Transition.Create(coderAgent, codeReviewAgent),
            ["adminToRunner"] = Transition.Create(admin, runner, async (from, to, messages) =>
            {
                // the last message should be from admin
                var lastMessage = messages.Last();
                if (lastMessage.From != admin.Name)
                {
                    return false;
                }

                // the previous messages should contain a message from coder
                var coderMessage = messages.FirstOrDefault(x => x.From == coderAgent.Name);
                if (coderMessage is null)
                {
                    return false;
                }

                return true;
            }),
            ["runnerToAdmin"] = Transition.Create(runner, admin),
            ["reviewerToAdmin"] = Transition.Create(codeReviewAgent, admin),
            ["adminToUser"] = Transition.Create(admin, userProxy, async (from, to, messages) =>
            {
                // the last message should be from admin
                var lastMessage = messages.Last();
                if (lastMessage.From != admin.Name)
                {
                    return false;
                }

                return true;
            }),
            ["userToAdmin"] = Transition.Create(userProxy, admin)
        };
        return transitionsByName;
    }

}

public class StringEventWriter : StringWriter
{
    public event Action<string>? OnWrite;
    public override void WriteLine(string? value)
    {
        OnWrite?.Invoke(value);
        base.WriteLine(value);
    }
    public override void Write(string? value)
    {
        OnWrite?.Invoke(value);
        base.Write(value);
    }
}