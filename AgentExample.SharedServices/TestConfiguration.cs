// Copyright (c) Microsoft. All rights reserved.

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;

namespace AgentExample.SharedServices;

public sealed class TestConfiguration
{
    private IConfiguration _configRoot;
    private static TestConfiguration? s_instance;

    private TestConfiguration(IConfiguration configRoot)
    {
        this._configRoot = configRoot;
    }

    public static void Initialize(IConfiguration configRoot)
    {
        s_instance = new TestConfiguration(configRoot);
    }

    private static OpenAIConfig? _openAi;
    public static OpenAIConfig? OpenAI
    {
        get => _openAi ?? LoadSection<OpenAIConfig>();
        set => _openAi = value;
    }

    private static AzureOpenAIConfig? _azureOpenAI;
    public static AzureOpenAIConfig? AzureOpenAI
    {
        get => _azureOpenAI ?? LoadSection<AzureOpenAIConfig>();
        set => _azureOpenAI = value;
    }
    private static PineconeConfig? _pinecone;
    public static PineconeConfig? Pinecone
    {
        get => _pinecone ?? LoadSection<PineconeConfig>();
        set => _pinecone = value;
    }

    private static SqliteConfig? _sqlite;

    public static SqliteConfig? Sqlite
    {
        get => _sqlite ?? LoadSection<SqliteConfig>();
        set => _sqlite = value;
    }
    private static QdrantConfig? _qdrant;
    public static QdrantConfig? Qdrant
    {
        get => _qdrant ?? LoadSection<QdrantConfig>();
        set => _qdrant = value;
    }
    private static YouTubeConfig? _youTube;
    public static YouTubeConfig? YouTube
    {
        get => _youTube ?? LoadSection<YouTubeConfig>();
        set => _youTube = value;
    }
    private static T? LoadSection<T>([CallerMemberName] string? caller = null)
    {
        if (s_instance == null)
        {
            throw new InvalidOperationException(
                "TestConfiguration must be initialized with a call to Initialize(IConfigurationRoot) before accessing configuration values.");
        }

        if (string.IsNullOrEmpty(caller))
        {
            throw new ArgumentNullException(nameof(caller));
        }


        return s_instance._configRoot.GetSection(caller).Get<T>();
    }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor.
    public class OpenAIConfig
    {
        public string ModelId { get; set; }
        public string ChatModelId { get; set; }
        public string EmbeddingModelId { get; set; }
        public string ApiKey { get; set; }
        public string ImageModelId { get; set; }
    }

    public class AzureOpenAIConfig
    {
        public string ServiceId { get; set; }
        public string EmbeddingDeploymentName { get; set; }
        public string ChatDeploymentName { get; set; }
        public string Endpoint { get; set; }
        public string ApiKey { get; set; }
        public string ModelId { get; set; }
        public string ChatModelId { get; set; }
        
    }
    public class QdrantConfig
    {
        public string Endpoint { get; set; }
        public string Port { get; set; }
    }

    public class SqliteConfig
    {
        public string ConnectionString { get; set; }
    }
    public class PineconeConfig
    {
        public string ApiKey { get; set; }
        public string Environment { get; set; }
    }
    public class YouTubeConfig
    {
        public string ApiKey { get; set; }
    }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor.
}