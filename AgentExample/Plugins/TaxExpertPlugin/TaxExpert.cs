﻿using System.ComponentModel;
using AgentExample.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using Microsoft.SemanticKernel.Memory;

namespace AgentExample.Plugins.TaxExpertPlugin;

public class TaxExpert
{
    private static readonly QdrantMemoryStore QdrantStore = new(TestConfiguration.Qdrant!.Endpoint, 1536, ConsoleLogger.LoggerFactory);
    private const string DocsCollectionName = "taxExpertCollection";
    public const string ChatWithTaxExpertSystemPromptTemplate =
        """
        You are an expert tax advisor.
        Use the [Tax CONTEXT] below to answer the user's questions.

        [Tax CONTEXT]
        {{$memory_context}}

        [User INPUT]
        {{$input}}
        """;
    [KernelFunction, Description("Retreive an answer from a Tax Expert using relevant information from tax specific HF Dataset (vjain/tax_embeddings)  to form expert tax responses")]
    [return: Description("A grounded answer to tax-specific questions")]
    public async Task<string> AskTextExpert([Description("Latest user chat query")] string query, [Description("chat history to include as part of the search query")] string? history = null, [Description("Number of most similar items to return from search. Default is 5")] int topN = 5)
    {
        await LoadMemories();
        var kernel = Kernel.CreateBuilder()
            .AddOpenAIChatCompletion(TestConfiguration.OpenAI.ModelId, TestConfiguration.OpenAI.ApiKey)
            .Build();
        var semanticMemory = await GetSemanticTextMemory();
        var memoryItems = await semanticMemory.SearchAsync(DocsCollectionName, $"{query} {history}", topN, 0.78).ToListAsync();
        var memory = string.Join("\n", memoryItems.Select(x => x.Metadata.Text));
        ConsoleLogger.LoggerFactory.CreateLogger<TaxExpert>().LogInformation("Memory:\n {memory}", memory);
        var settings = new OpenAIPromptExecutionSettings { MaxTokens = 512, ChatSystemPrompt = ChatWithTaxExpertSystemPromptTemplate };
        var args = new KernelArguments(settings)
        {
            ["memory_context"] = memory,
            ["input"] = query
        };
        var result = await kernel.InvokePromptAsync(ChatWithTaxExpertSystemPromptTemplate, args);
        return result.GetValue<string>()!;
    }
    private static async Task<bool> CollectionExists()
    {
        var collections = await QdrantStore.GetCollectionsAsync().ToListAsync();
        Console.WriteLine($"Collections: {string.Join(",\n", collections)}");
        return collections.Contains(DocsCollectionName);
    }
    private static Task<ISemanticTextMemory> GetSemanticTextMemory(bool inMemory = false)
    {
        IMemoryStore memoryStore = inMemory ? new VolatileMemoryStore() : new QdrantMemoryStore(TestConfiguration.Qdrant!.Endpoint, 1536, ConsoleLogger.LoggerFactory);
        return Task.FromResult(new MemoryBuilder()
            .WithMemoryStore(memoryStore)
            .WithLoggerFactory(ConsoleLogger.LoggerFactory)
            .WithOpenAITextEmbeddingGeneration(TestConfiguration.OpenAI.EmbeddingModelId, TestConfiguration.OpenAI.ApiKey) //OpenAI
            //.WithAzureOpenAITextEmbeddingGeneration(TestConfiguration.AzureOpenAI.EmbeddingDeploymentName, TestConfiguration.AzureOpenAI.Endpoint, TestConfiguration.AzureOpenAI.ApiKey) //Azure OpenAI
            .Build());
    }
    private static async Task LoadMemories()
    {
        if (!await CollectionExists())
        {
            await LoadExternalData();
        }
    }
    public static async Task LoadExternalData()
    {
        var client = new HttpClient();
        int offset = 4400;
        var memory = await GetSemanticTextMemory();
        while (true)
        {
            var taxItems = await client.GetFromJsonAsync<HuggingFaceDataSet>(
                $"https://datasets-server.huggingface.co/rows?dataset=vjain%2Ftax_embeddings&config=default&split=train&offset={offset}");
            var rowsCount = taxItems?.Rows?.Count;
            offset += rowsCount.GetValueOrDefault();
            if (rowsCount is null or 0) break;
            Console.WriteLine($"Tax items retrieved from huggingface - {rowsCount}");

            var taxMemoryItems = taxItems!.Rows!.Select(x => new TaxItem(x.RowIdx, x.Row!.Text ?? "")).ToList();
           
            foreach (var taxItem in taxMemoryItems)
            {
                var id = await memory.SaveInformationAsync(DocsCollectionName, taxItem.Text,
                    $"HF_TaxItem_{taxItem.RowIndex}");
                Console.WriteLine($"Saved TaxItem {taxItem.RowIndex} (id:{id})");
            }
           
        }
    }
   
}

public record TaxItem(int RowIndex, string Text);