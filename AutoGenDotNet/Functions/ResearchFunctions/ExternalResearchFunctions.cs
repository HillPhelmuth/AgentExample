using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using AgentExample.SharedServices;
using AgentExample.SharedServices.Models;
using AgentExample.SharedServices.Services;
using AutoGen.Core;
using AutoGenDotNet.Functions.Summarizer;
using AutoGenDotNet.Services;
using Microsoft.DotNet.Interactive;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Text;
using Kernel = Microsoft.SemanticKernel.Kernel;

namespace AutoGenDotNet.Functions.ResearchFunctions;

/// <summary>
/// Represents a class that performs web search and crawling operations.
/// </summary>
public partial class ExternalResearchFunctions
{
    private readonly BingWebSearchService _bingWebSearchService;
    private readonly SummerizeFunctions _summerizeFunctions;
    /// <summary>
    /// Initializes a new instance of the <see cref="ExternalResearchFunctions"/> class.
    /// </summary>
    /// <param name="bingWebSearchService">The Bing web search service.</param>
    public ExternalResearchFunctions(BingWebSearchService bingWebSearchService)
    {
        _bingWebSearchService = bingWebSearchService;
        _summerizeFunctions = new SummerizeFunctions();
    }
    /// <summary>
    /// Extracts a web search query from a question.
    /// </summary>
    /// <param name="input">The question input.</param>
    /// <returns>The extracted web search query.</returns>
    [KernelFunction, Description("Extract a web search query from a question")]
    [Function]
    public async Task<string> ExtractWebSearchQuery(string input)
    {
        var builder = Kernel.CreateBuilder()
            .AddOpenAIChatCompletion(TestConfiguration.OpenAI.ModelId, TestConfiguration.OpenAI.ApiKey);
        var kernel = builder.Build();
        var extractPlugin = kernel.CreateFunctionFromPrompt("Extract terms for a simple web search query from a question. Include no other content\nquestion:{{$input}}", executionSettings: new OpenAIPromptExecutionSettings { MaxTokens = 128, Temperature = 0.2 });
        var result = await kernel.InvokeAsync(extractPlugin, new KernelArguments() { { "input", input } });
        return result.GetValue<string>() ?? input;
    }
    /// <summary>
    /// Search Web, summarize the content of each result and generate citations.
    /// </summary>
    /// <param name="input">The web search query.</param>
    /// <param name="resultCount">The number of web search results to use.</param>
    /// <returns>A JSON string representing the search and citation results.</returns>
    [KernelFunction, Description("Search Web, summarize the content of each result and generate citations.")]
    [Function]
    public async Task<string> SearchAndCiteWeb([Description("Web search query")] string input, [Description("Number of web search results to use")] int resultCount = 2)
    {
        var results = await _bingWebSearchService!.SearchAsync(input, resultCount) ?? [];
        var scraperTaskList = new List<Task<List<SearchResultItem>>>();
        foreach (var result in results.Take(Math.Min(results.Count, 5)))
        {

            try
            {
                scraperTaskList.Add(ScrapeChunkAndSummarize(result.Url, result.Name, input, result.Snippet));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to scrape text from {result.Url}\n\n{ex.Message}");
            }

        }

        var scrapeResults = await Task.WhenAll(scraperTaskList);
        var searchResultItems = scrapeResults.SelectMany(x => x).ToList();
        var resultItems = new List<SearchResultItem>();
        foreach (var group in searchResultItems.GroupBy(x => x.Url))
        {
            var count = group.Count();
            if (count > 1)
            {
                var index = 1;
                var groupItem = new SearchResultItem(group.Key)
                {
                    Title = group.First().Title,
                    Content = ""
                };
                foreach (var item in group)
                {
                    groupItem.Content += $"{item.Content}\n";
                }
                resultItems.Add(groupItem);
            }
            else
            {
                resultItems.Add(new SearchResultItem(group.Key) { Title = group.First().Title, Content = group.First().Content });
            }
        }
        var searchCiteJson = JsonSerializer.Serialize(resultItems, new JsonSerializerOptions { WriteIndented = true });
        return searchCiteJson;
    }

    /// <summary>
    /// Search Youtube, transcribe and summarize the content of each result and generate citations.
    /// </summary>
    /// <param name="input">The youtube video search query.</param>
    /// <param name="resultCount">The number of web search results to use.</param>
    /// <returns>A JSON string representing the search and citation results.</returns>
    [KernelFunction, Description("Search Youtube, transcribe and summarize the content of each result and generate citations.")]
    [Function]
    public async Task<string> SearchAndCiteYoutube([Description("Youtube search query")] string input, [Description("Number of youtube search results to use")] int resultCount = 2)
    {
        var youtubeService = new YouTubeSearch(TestConfiguration.YouTube.ApiKey);
        var results = await youtubeService.Search(input, resultCount);
        var tasks = results.Select(TranscribeChunkAndSummarize);
        var searchResults = await Task.WhenAll(tasks);
        var searchResultItems = searchResults.SelectMany(x => x).ToList();
        var resultItems = new List<SearchResultItem>();
        foreach (var group in searchResultItems.GroupBy(x => x.Url))
        {
            var count = group.Count();
            if (count > 1)
            {
                var index = 1;
                var groupItem = new SearchResultItem(group.Key)
                {
                    Title = group.First().Title,
                    Content = ""
                };
                foreach (var item in group)
                {
                    groupItem.Content += $"{item.Content}\n";
                }
                resultItems.Add(groupItem);
            }
            else
            {
                resultItems.Add(new SearchResultItem(group.Key) { Title = group.First().Title, Content = group.First().Content });
            }
        }
        var searchCiteJson = JsonSerializer.Serialize(resultItems, new JsonSerializerOptions { WriteIndented = true });
        return searchCiteJson;
    }
    private async Task<List<SearchResultItem>> TranscribeChunkAndSummarize(YouTubeSearchResult searchResult)
    {
        var kernel = Kernel.CreateBuilder().Build();
        var plugin = await kernel.ImportPluginFromOpenApi("Video");
        var args = new KernelArguments() { ["videoId"] = searchResult.Id, ["segment"] = 1, ["includeTimestamps"] = true };
        var result = await kernel.InvokeAsync(plugin["transcribeVideo"], args);
        var transcriptionJson = result.GetValue<RestApiOperationResponse>().Content.ToString();
        var transcription = JsonSerializer.Deserialize<VideoTranscription>(transcriptionJson);
        var totalSegments = transcription?.TotalSegments ?? 1;
        var transcriptionContent = transcription.TranscribedText;
        Console.WriteLine($"Video {searchResult.Description} Section Transcribed: 1 of {totalSegments}");
        if (totalSegments > 1)
        {
            var currentSegment = transcription.CurrentSegment;
            while (currentSegment < totalSegments)
            {
                args["segment"] = currentSegment + 1;
                var segmentResult = await kernel.InvokeAsync(plugin["transcribeVideo"], args);
                var segmentTranscriptionjson = segmentResult.GetValue<RestApiOperationResponse>().Content.ToString();
                var segmentTranscript = JsonSerializer.Deserialize<VideoTranscription>(segmentTranscriptionjson);
                transcriptionContent += $"\n{segmentTranscript?.TranscribedText}";
                //currentSegment = transcription.CurrentSegment;
                currentSegment++;
                Console.WriteLine($"Video {searchResult.Description} Section Transcribed: {currentSegment} of {totalSegments}");
            }
        }
        var segments = await SummarizeResults(searchResult.Title, searchResult.Description, transcriptionContent);
        return segments.Select(x => new SearchResultItem(searchResult.Url) { Title = searchResult.Title, Content = x }).ToList();
    }
    private async Task<List<SearchResultItem>> ScrapeChunkAndSummarize(string url, string title, string input, string summary)
    {

        try
        {
            var crawler = new CrawlService();
            var text = await crawler.CrawlAsync(url);
            var summaryResults = await SummarizeResults(title, input, text);

            return summaryResults.Select(x => new SearchResultItem(url) { Title = title, Content = x }).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to scrape text from {url}\n\n{ex.Message}\n{ex.StackTrace}");
            return [new SearchResultItem(url) { Title = title, Content = summary }];
        }

    }

    private async Task<List<string>> SummarizeResults(string title, string input, string text)
    {
        var tokens = StringHelpers.GetTokens(text);
        var count = tokens / 4096;
        var segments = ChunkIntoSegments(text, Math.Max(count, 1), 4096, title).ToList();
        Console.WriteLine($"Segment count: {segments.Count}");
        var argList = segments.Select(segment => new KernelArguments { ["input"] = segment, ["query"] = input }).ToList();
        var summaryResults = new List<string>();
        foreach (var arg in argList)
        {
            //var result = await _kernel.InvokeAsync(_summarizeWebContent, arg);
            var result = await _summerizeFunctions.SummarizeText(arg["input"].ToString());
            summaryResults.Add(result);
        }

        return summaryResults;
    }
    private static IEnumerable<string> ChunkIntoSegments(string text, int segmentCount, int maxPerSegment = 4096, string description = "", bool ismarkdown = true)
    {
        var total = StringHelpers.GetTokens(text);
        var perSegment = total / segmentCount;
        var totalPerSegment = perSegment > maxPerSegment ? maxPerSegment : perSegment;
        List<string> paragraphs;
        if (ismarkdown)
        {
            var lines = TextChunker.SplitMarkDownLines(text, 200, StringHelpers.GetTokens);
            paragraphs = TextChunker.SplitMarkdownParagraphs(lines, Math.Max(totalPerSegment,200), 0, description, StringHelpers.GetTokens);
        }
        else
        {
            var lines = TextChunker.SplitPlainTextLines(text, 200, StringHelpers.GetTokens);
            paragraphs = TextChunker.SplitPlainTextParagraphs(lines, Math.Max(totalPerSegment, 200), 0, description, StringHelpers.GetTokens);
        }
        return paragraphs.Take(segmentCount);
    }

}
/// <summary>
/// Represents a search result item.
/// </summary>
public record SearchResultItem(string Url)
{
    [JsonPropertyName("content"), JsonPropertyOrder(3)]
    public string? Content { get; set; }
    [JsonPropertyName("title"), JsonPropertyOrder(1)]
    public string? Title { get; set; }
    [JsonPropertyName("url"), JsonPropertyOrder(2)]
    public string Url { get; set; } = Url;
}
public class VideoTranscription
{
    [JsonPropertyName("totalSegments")]
    public int TotalSegments { get; set; }
    [JsonPropertyName("currentSegment")]
    public int CurrentSegment { get; set; }
    [JsonPropertyName("transcribedText")]
    public string TranscribedText { get; set; }
}
