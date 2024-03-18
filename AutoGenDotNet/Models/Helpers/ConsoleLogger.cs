// Copyright (c) Microsoft. All rights reserved.

using AutoGenDotNet.Models.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AutoGenDotNet.Models.Helpers;

/// <summary>
/// Basic logger printing to console
/// </summary>
public class ConsoleLogger
{
    public static ILogger Logger => LoggerFactory.CreateLogger<object>();

    public static ILoggerFactory LoggerFactory => s_loggerFactory.Value;

    private static readonly Lazy<ILoggerFactory> s_loggerFactory = new(LogBuilder);

    private static ILoggerFactory LogBuilder()
    {
        return Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Debug);
            builder.AddConsole();
            builder.AddFilter("Microsoft", LogLevel.Trace);
            builder.AddFilter("Microsoft", LogLevel.Debug);
            builder.AddFilter("Microsoft", LogLevel.Information);
            builder.AddFilter("Microsoft", LogLevel.Warning);
            builder.AddFilter("Microsoft", LogLevel.Error);

            builder.AddFilter("Microsoft", LogLevel.Warning);
            builder.AddFilter("System", LogLevel.Warning);

            //builder.Add();
        });
    }
}
