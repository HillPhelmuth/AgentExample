using System.ComponentModel;
using System.Text;
using System.Text.Json;
using AutoGen.Core;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;

namespace AutoGenDotNet.Functions.CodeInterpreter;

/// <summary>
/// Executes the specified python code in a sandbox.
/// </summary>
public partial class CodeInterpreterAutoGenFunctions
{
    private static readonly ILoggerFactory _loggerFactory = LoggerFactory.Create(b => b.AddConsole());
    private static CodeInterpretionPluginOptions options = new();
    private readonly DockerClient _dockerClient = new DockerClientConfiguration(new Uri(options.DockerEndpoint), defaultTimeout: TimeSpan.FromMinutes(2), namedPipeConnectTimeout: TimeSpan.FromMinutes(3)).CreateClient();

    private readonly ILogger<CodeInterpreterAutoGenFunctions> _logger = _loggerFactory.CreateLogger<CodeInterpreterAutoGenFunctions>();

    private const string CodeFilePath = "/var/app/code.py";
    private const string RequirementsFilePath = "/var/app/requirements.txt";
    private const string OutputDirectoryPath = "/var/app/output";

    /// <summary>
    /// Executes the specified python code in a sandbox.
    /// </summary>
    /// <param name="input">The input python code.</param>
    /// <param name="requirement">The requirements for the python code.</param>
    /// <param name="bindings">The input file bindings for the python code.</param>
    /// <returns>The result of the program execution.</returns>
    [Function("ExecutePythonCode", CodeInterpreterConstants.CombinedDescription)]
    public async Task<string> ExecutePythonCode([Description(CodeInterpreterConstants.InputDescription)] string input, [Description(CodeInterpreterConstants.RequirementsDescription)] object requirement = null, [Description(CodeInterpreterConstants.BindingsDescription)] object bindings = null)
    {
        //ArgumentNullException.ThrowIfNull(arguments);

        await PullRequiredImageAsync().ConfigureAwait(false);

        var instanceId = string.Empty;

        var codeFilePath = Path.GetTempFileName();
        var requirementsFilePath = Path.GetTempFileName();
        var outputDirectory = Path.GetTempFileName();

        try
        {
            var pythonCode = input;
            await File.WriteAllTextAsync(codeFilePath, pythonCode);
            //if (arguments.TryGetValue("input", out var pythonCode))
            //{
            //    await File.WriteAllTextAsync(codeFilePath, pythonCode!.ToString());
            //}
            //else
            //{
            //    throw new Exception("The input code is not correctly provided.");
            //}
            if (requirement is not null)
            {
                await File.WriteAllTextAsync(requirementsFilePath, requirement.ToString());
            }
            //if (arguments.TryGetValue("requirements", out var requirements))
            //{
            //    await File.WriteAllTextAsync(requirementsFilePath, requirements?.ToString());
            //}

            var inputFiles = bindings is not null ? bindings.ToString() : string.Empty;

            instanceId = await StartNewSandbox(requirementsFilePath, codeFilePath, outputDirectory, inputFiles!).ConfigureAwait(false);

            _logger.LogInformation($"Preparing Sandbox ({instanceId}:{Environment.NewLine}requirements.txt:{Environment.NewLine}{requirement}{Environment.NewLine}code.py:{Environment.NewLine}{pythonCode}");

            await InstallRequirementsAsync(instanceId).ConfigureAwait(false);

            var result = await ExecuteCodeAsync(instanceId).ConfigureAwait(false);

            PrepareOutputFiles(outputDirectory);

            return result;
        }
        finally
        {
            if (!string.IsNullOrEmpty(instanceId))
            {
                await _dockerClient.Containers.RemoveContainerAsync(instanceId, new ContainerRemoveParameters
                {
                    Force = true
                }).ConfigureAwait(false);
            }

            if (File.Exists(codeFilePath))
            {
                File.Delete(codeFilePath);
            }
            if (File.Exists(requirementsFilePath))
            {
                File.Delete(requirementsFilePath);
            }
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, true);
            }
        }
    }

    private async Task<string> StartNewSandbox(
        string requirementFilePath,
        string codeFilePath,
        string outputDirectoryPath,
        string inputFiles)
    {
        var config = new Config
        {
            Hostname = "localhost",
        };

        if (File.Exists(outputDirectoryPath))
        {
            File.Delete(outputDirectoryPath);
        }

        if (!Directory.Exists(outputDirectoryPath))
        {
            Directory.CreateDirectory(outputDirectoryPath);
        }

        List<string>? inputBindings = new List<string>();

        if (!string.IsNullOrEmpty(inputFiles))
        {
            using MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(inputFiles));
            inputBindings = await JsonSerializer.DeserializeAsync<List<string>>(stream).ConfigureAwait(false);
        }

        inputBindings!.AddRange(new[]
        {
                $"{codeFilePath}:{CodeFilePath}:ro",
                $"{requirementFilePath}:{RequirementsFilePath}:ro",
                $"{outputDirectoryPath}:{OutputDirectoryPath}:rw"
            });


        var containerCreateOptions = new CreateContainerParameters(config)
        {
            Image = options.DockerImage,
            Entrypoint = new[] { "/bin/sh" },
            Tty = true,
            NetworkDisabled = false,
            HostConfig = new HostConfig
            {
                Binds = inputBindings
            }
        };

        _logger.LogDebug("Creating container.");
        _logger.LogTrace(JsonSerializer.Serialize(containerCreateOptions));

        var response = await _dockerClient.Containers.CreateContainerAsync(containerCreateOptions).ConfigureAwait(false);

        _logger.LogDebug($"Starting the container (id: {response.ID}).");
        await _dockerClient.Containers.StartContainerAsync(response.ID, new ContainerStartParameters()).ConfigureAwait(false);

        return response.ID;
    }

    private async Task InstallRequirementsAsync(string containerId)
    {
        _ = await ExecuteInContainer(containerId, $"pip install -r {RequirementsFilePath}");
    }

    private async Task<string> ExecuteCodeAsync(string containerId)
    {
        return await ExecuteInContainer(containerId, $"python {CodeFilePath}").ConfigureAwait(false);
    }

    private async Task<string> ExecuteInContainer(string containerId, string command)
    {
        _logger.LogDebug($"({containerId})# {command}");

        var execContainer = await _dockerClient.Exec.ExecCreateContainerAsync(containerId, new ContainerExecCreateParameters
        {
            AttachStderr = true,
            AttachStdout = true,
            AttachStdin = true,
            Cmd = command.Split(' ', StringSplitOptions.RemoveEmptyEntries),
            Tty = true
        }).ConfigureAwait(false);

        var multiplexedStream = await _dockerClient.Exec.StartAndAttachContainerExecAsync(execContainer.ID, true);

        var output = await multiplexedStream.ReadOutputToEndAsync(CancellationToken.None);

        if (!string.IsNullOrWhiteSpace(output.stderr))
        {
            _logger.LogError($"({containerId}): {output.stderr}");
            throw new Exception(output.stderr);
        }

        _logger.LogDebug($"({containerId}): {output.stdout}");

        return output.stdout;
    }

    private async Task PullRequiredImageAsync()
    {
        try
        {
            _ = await _dockerClient.Images.InspectImageAsync(options.DockerImage);
        }
        catch (DockerImageNotFoundException)
        {
            try
            {
                await _dockerClient.Images.CreateImageAsync(new ImagesCreateParameters { FromImage = options.DockerImage }, new AuthConfig(), new Progress<JSONMessage>());
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, $"Failed to create email for {options.DockerImage}");
            }
        }
    }

    private void PrepareOutputFiles(string outputDirectory)
    {

        if (!Directory.Exists(options.OutputFilePath))
        {
            Directory.CreateDirectory(options.OutputFilePath);
        }

        foreach (var item in Directory.EnumerateFiles(outputDirectory))
        {
            var fileInfo = new FileInfo(item);
            var outputFilePath = Path.Combine(options.OutputFilePath, fileInfo.Name);

            File.Copy(item, outputFilePath, true);
        }
    }
}
