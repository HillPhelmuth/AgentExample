using System.Text.Json;
using AgentExample.SharedServices.Models;
using AutoGenDotNet.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AutoGenDotNet.Services;

/// <summary>
/// Service for performing Bing web searches.
/// </summary>
public class BingWebSearchService
{
    //private readonly IHttpClientFactory _httpClientFactory;
    private readonly HttpClient _httpClient;
    private readonly ILogger<BingWebSearchService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BingWebSearchService"/> class.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    public BingWebSearchService(IConfiguration configuration/*, IHttpClientFactory httpClientFactory,*/ /*ILoggerFactory loggerFactory*/)
    {
        //_httpClientFactory = httpClientFactory;
        var loggerFactory = ConsoleLogger.LoggerFactory;
        _logger = loggerFactory.CreateLogger<BingWebSearchService>();
        var subscriptionKey = configuration["Bing:ApiKey"]!;
        _httpClient = new HttpClient() /*_httpClientFactory.CreateClient()*/;
        _httpClient.BaseAddress = new Uri("https://api.bing.microsoft.com/");
        _httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
    }

    /// <summary>
    /// Performs a Bing web search.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="answerCount">The number of search results to retrieve.</param>
    /// <returns>A list of Bing search results.</returns>
    public async Task<List<BingSearchResult>?> SearchAsync(string query, int answerCount = 10)
    {
        if (answerCount < 3) answerCount = 3;
        _logger.LogInformation("Searching Bing for {query} with answerCount {answerCount}", query, answerCount);
        var response = await _httpClient.GetAsync($"v7.0/search?q={query}&answerCount={answerCount}");
        var content = await response.Content.ReadAsStringAsync();
        var searchResult = JsonSerializer.Deserialize<SearchResult>(content);
        var bingSearchResults = searchResult?.BingSearchResults;
        _logger.LogInformation("Search Bing Results:\n {result}",
            string.Join("\n", bingSearchResults?.Select(x => x.ToString()) ?? []));
        return bingSearchResults;
    }
}
