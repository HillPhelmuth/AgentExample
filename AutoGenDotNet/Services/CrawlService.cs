using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Abot2.Crawler;
using AbotX2.Crawler;
using AbotX2.Poco;
using AutoGenDotNet.Models;
using AutoGenDotNet.Models.Helpers;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using ReverseMarkdown;
namespace AutoGenDotNet.Services;

/// <summary>
/// Represents a service for crawling web pages.
/// </summary>
public sealed class CrawlService : IDisposable
{
    private readonly CrawlerX _crawlerX;
    private readonly TaskCompletionSource<string> _taskCompletionSource = new();
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<CrawlService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CrawlService"/> class.
    /// </summary>
    /// <param name="loggerFactory">The logger factory.</param>
    public CrawlService(ILoggerFactory? loggerFactory = null)
    {
        _loggerFactory = loggerFactory ?? ConsoleLogger.LoggerFactory;
        _logger = _loggerFactory.CreateLogger<CrawlService>();
        var crawlConfigurationX = new CrawlConfigurationX
        {
            MaxPagesToCrawl = 1,
            MaxCrawlDepth = 0,
        };

        _crawlerX = new CrawlerX(crawlConfigurationX);

        _crawlerX.PageCrawlStarting += ProcessPageCrawlStarting;
        _crawlerX.PageCrawlCompleted += ProcessPageCrawlCompleted;
        _crawlerX.PageCrawlDisallowed += PageCrawlDisallowed;
    }

    /// <summary>
    /// Crawls the specified URL asynchronously.
    /// </summary>
    /// <param name="url">The URL to crawl.</param>
    /// <returns>The crawled content.</returns>
    public async Task<string> CrawlAsync(string url)
    {
        Console.WriteLine($"Crawling url {url}");
        var response = await _crawlerX.CrawlAsync(new Uri(url));
        if (response.ErrorOccurred)
        {
            Console.WriteLine($"Crawl of {url} completed with error: {response.ErrorException.Message}");
            throw new Exception($"Crawl of {url} completed with error: {response.ErrorException.Message}");
        }

        Console.WriteLine($"Crawl of {url} completed without error.");
        return await _taskCompletionSource.Task;
    }

    private void ProcessPageCrawlStarting(object? sender, PageCrawlStartingArgs e)
    {
        var pageToCrawl = e.PageToCrawl;
        var text = $"About to crawl link {pageToCrawl.Uri.AbsoluteUri} which was found on page {pageToCrawl.ParentUri.AbsoluteUri}";
        Console.WriteLine(text);
    }

    private void ProcessPageCrawlCompleted(object? sender, PageCrawlCompletedArgs e)
    {
        var crawledPage = e.CrawledPage;

        if (crawledPage.HttpRequestException != null || crawledPage.HttpResponseMessage.StatusCode != HttpStatusCode.OK)
        {
            Console.WriteLine($"Crawl of page failed {crawledPage.Uri.AbsoluteUri}");
        }
        else
            Console.WriteLine($"Crawl of page succeeded {crawledPage.Uri.AbsoluteUri}");

        var html = crawledPage.Content.Text;
        if (string.IsNullOrEmpty(html))
        {
            Console.WriteLine($"Page had no content {crawledPage.Uri.AbsoluteUri}");
        }

        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        var config = new Config
        {
            // Include the unknown tag completely in the result (default as well)
            UnknownTags = Config.UnknownTagsOption.Drop,
            // generate GitHub flavoured markdown, supported for BR, PRE and table tags
            GithubFlavored = true,
            // will ignore all comments
            RemoveComments = true,
            // remove markdown output for links where appropriate
            SmartHrefHandling = true
        };

        var converter = new Converter(config);
        var htmlBuilder = new StringBuilder();
        foreach (var child in doc.DocumentNode?.DescendantsAndSelf().Where(x => x.Name?.ToLower() == "p" || IsValidHeader(x.Name?.ToLower()) || x.Name == "table") ?? new List<HtmlNode>())
        {
            htmlBuilder.Append(child.OuterHtml);
        }
        var tidyHtml = Cleaner.PreTidy(htmlBuilder.ToString(), true);

        var mkdwnText = converter.Convert(tidyHtml);
        //ToDo Temp for Testing -----------------------------------
        //var id = Guid.NewGuid();
        //File.WriteAllText($"{id}_crawlText.html", tidyHtml);
        //File.WriteAllText($"{id}_crawlText.md", mkdwnText);
        //-------------------------------------------------------
        var cleanUpContent = CleanUpContent(mkdwnText);
        Console.WriteLine($"{crawledPage.Uri}\n----------------\n Crawled for {cleanUpContent.TokenCount()} Tokens");
        _taskCompletionSource.SetResult(cleanUpContent);
    }

    private static bool IsValidHeader(string? tagName)
    {
        if (tagName == null) return false;
        var pattern = new Regex("^h[1-6]$");
        return pattern.IsMatch(tagName);
    }

    private void PageCrawlDisallowed(object? sender, PageCrawlDisallowedArgs e)
    {
        var pageToCrawl = e.PageToCrawl;
        var text = $"Did not crawl page {pageToCrawl.Uri.AbsoluteUri} due to {e.DisallowedReason}";
        throw new Exception(text);
    }

    private string CleanUpContent(string content)
    {
        return content.Replace("\t", " ");
    }
    /// <inheritdoc/>
    public void Dispose()
    {
        _crawlerX.Dispose();
    }
}
public class PageCrawlEventArgs : EventArgs
{
    public string PageContent { get; set; }

    public PageCrawlEventArgs(string pageContent)
    {
        PageContent = pageContent;
    }
}

// Declare a delegate.
public delegate void PageCrawlEventHandler(object? sender, PageCrawlEventArgs args);