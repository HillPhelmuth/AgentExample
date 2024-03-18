namespace AutoGenDotNet.Functions.CodeInterpreter;

public class CodeInterpretionPluginOptions
{
    public string DockerEndpoint { get; set; } = "npipe://./pipe/docker_engine";

    public string DockerImage { get; set; } = "python:3-alpine";

    public string OutputFilePath { get; set; } = @"C:\auto-gen\outputs";
}