using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using Markdig;

namespace AgentExample.Components.Pages
{
    public partial class AwesomeSkReadme : ComponentBase
    {
        [Inject]
        private IHttpClientFactory HttpClientFactory { get; set; } = default!;
        private const string Url = "https://raw.githubusercontent.com/geffzhang/awesome-semantickernel/master/README.md";
        private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();
        private string _markdownString = "";
        protected override async Task OnInitializedAsync()
        {
            var client = HttpClientFactory.CreateClient();
            _markdownString = await client.GetStringAsync(Url);

            await base.OnInitializedAsync();
        }
        private static string ConvertToHtml(string markdown)
        {
            if (string.IsNullOrEmpty(markdown)) return markdown;
            return Markdown.ToHtml(markdown, Pipeline);
        }
    }
}
