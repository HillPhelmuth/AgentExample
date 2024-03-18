using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Plugins.OpenApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AgentExample.SharedServices.Models;

public static class PluginHelpers
{
    private const string PluginFunctionsJson = "pluginFunctions.json";
    public static KernelPlugin CreatePluginFromYaml(this Kernel kernel, string pluginName)
    {
        var pluginFunctions = ExtractFromAssembly<List<PluginFunctionName>>(PluginFunctionsJson);
        var plugin = pluginFunctions?.FirstOrDefault(p => p.Plugin == pluginName);
        var functions = plugin.Functions.Select(x => KernelFunctionYaml.FromPromptYaml(ExtractFromAssembly<string>($"{x}.yaml")!));
        return KernelPluginFactory.CreateFromFunctions(pluginName, functions: functions);
    }
    public static KernelPlugin ImportPluginFromYaml(this Kernel kernel, string pluginName)
    {
        var plugin = kernel.CreatePluginFromYaml(pluginName);
        kernel.Plugins.Add(plugin);
        return plugin;
    }
    public static async Task<KernelPlugin> ImportPluginFromOpenApi(this Kernel kernel, string pluginName)
    {
        var filename = $"{pluginName}-openapi.json";
        var fileStream = ExtractStreamFromAssembly(filename);
        var plugin = await kernel.ImportPluginFromOpenApiAsync(pluginName, fileStream!, new OpenApiFunctionExecutionParameters { EnableDynamicPayload = true, IgnoreNonCompliantErrors = true});
        return plugin;
    }
    public static T? ExtractFromAssembly<T>(string fileName)
    {
        var stream = ExtractStreamFromAssembly(fileName);
        if (stream is null)
            return default;
        using var reader = new StreamReader(stream);
        object result = reader.ReadToEnd();
        if (typeof(T) == typeof(string))
            return (T)result;
        return JsonSerializer.Deserialize<T>(result.ToString()!);
    }

    private static Stream? ExtractStreamFromAssembly(string fileName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var jsonName = assembly.GetManifestResourceNames()
            .SingleOrDefault(s => s.EndsWith(fileName, StringComparison.OrdinalIgnoreCase)) ?? "";
        var stream = assembly.GetManifestResourceStream(jsonName);
        return stream;
    }
}
internal record PluginFunctionName(string Plugin, string[] Functions);