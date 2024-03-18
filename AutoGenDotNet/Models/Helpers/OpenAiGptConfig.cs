// Copyright (c) Microsoft Corporation. All rights reserved.
// LLMConfiguration.cs

using AutoGen.OpenAI;

namespace AutoGenDotNet.Models.Helpers;

internal static class OpenAiGptConfig
{
    public static OpenAIConfig GetOpenAIGPT3_5_Turbo(string? apiKey = null)
    {
        var openAIKey = apiKey ?? Environment.GetEnvironmentVariable("OpenAI:ApiKey") ?? throw new Exception("Please set OPENAI_API_KEY environment variable.");
        var modelId = "gpt-3.5-turbo";
        return new OpenAIConfig(openAIKey, modelId);
    }

    public static OpenAIConfig GetOpenAIGPT4(string? apiKey = null)
    {
        var openAIKey = apiKey ?? Environment.GetEnvironmentVariable("OpenAI:ApiKey") ?? throw new Exception("Please set OPENAI_API_KEY environment variable.");
        var modelId = "gpt-4-turbo-preview";

        return new OpenAIConfig(openAIKey, modelId);
    }

    
}
