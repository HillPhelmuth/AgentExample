using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.CSharp.Scripting.Hosting;
using Microsoft.CodeAnalysis.Scripting;
using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;


namespace AutoGen.DotnetInteractive;

public record ScriptCommand(string Code, ScriptState<object>? State)
{
    public string Code { get; } = Code;
    public ScriptState<object>? State { get; set; } = State;
}

public class ScriptSession
{
    private readonly ScriptOptions _options = ScriptOptions.Default
        .AddReferences(CompileResources.PortableExecutableReferences)
        .AddImports("System", "System.IO", "System.Collections.Generic", "System.Collections", "System.Console", "System.Diagnostics", "System.Dynamic", "System.Linq", "System.Linq.Expressions", "System.Net.Http", "System.Text", "System.Text.Json", "System.Net", "System.Threading.Tasks", "System.Numerics", "Microsoft.CodeAnalysis", "Microsoft.CodeAnalysis.CSharp");
    private readonly List<ScriptCommand> _history = new();
    public async Task<string> CompileAndEval(string code, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await CSharpScript.RunAsync(code, cancellationToken: cancellationToken);
            return "Code Compiled and Evaluated Successfully";
        }
        catch (CompilationErrorException ex)
        {
            return $"Code Failed to Compile or execute.\n\nError:\n\n{ex}";
        }
        
    }
    public async Task<string> EvaluateAsync(string code, CancellationToken token = default)
    {
        var tree = CSharpSyntaxTree.ParseText(code, new CSharpParseOptions(documentationMode:DocumentationMode.None, kind:SourceCodeKind.Script));

        // Find the synthesized Main method that contains the top-level statements
        var root = tree.GetCompilationUnitRoot();
        var members = root.Members;
        var previousState = _history.Count > 0 ? _history[_history.Count - 1].State : null;
        var previousOut = Console.Out;
        var writer = new StringWriter();
        Console.SetOut(writer);
        try
        {
            var updatedState = previousState;
            foreach (var codeString in members.Select(member => member.ToFullString()))
            {
                try
                {
                    updatedState = await EvalStatement(codeString, updatedState);
                    Console.WriteLine($"Evaluated {codeString}.\n\nContinuing script execution.\n\n");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error Evaluating {codeString}.\n\nCancelling Code Execution");
                    break;
                }
            }
            //var newState = await EvalStatement(code, previousState);
        }
        catch (CompilationErrorException ex)
        {
            Console.WriteLine(CSharpObjectFormatter.Instance.FormatException(ex));
        }
        Console.SetOut(previousOut);
        return writer.ToString();
    }

    private async Task<ScriptState<object>> EvalStatement(string code, ScriptState? previousState)
    {
        var newState = previousState == null
            ? await CSharpScript.RunAsync(code, _options)
            : await previousState.ContinueWithAsync(code, _options);
        _history.Add(new ScriptCommand(code, newState));
        if (newState.ReturnValue != null && !string.IsNullOrEmpty(newState.ReturnValue.ToString()))
        {
            Console.WriteLine(CSharpObjectFormatter.Instance.FormatObject(newState.ReturnValue));
        }

        return newState;
    }

    public async Task<object> ReEvaluateAsync(int commandIndex, string newCode)
    {
        if (commandIndex < 0 || commandIndex >= _history.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(commandIndex));
        }

        // Replace the command at the specified index
        _history[commandIndex] = new ScriptCommand(newCode, null);

        // Clear the script state after the replaced command
        for (var i = commandIndex + 1; i < _history.Count; i++)
        {
            _history[i] = new ScriptCommand(_history[i].Code, null);
        }

        // Re-evaluate all commands
        ScriptState<object>? state = null;
        for (var i = 0; i < _history.Count; i++)
        {
            var command = _history[i];
            state = state == null
                ? await CSharpScript.RunAsync(command.Code, _options)
                : await state.ContinueWithAsync(command.Code, _options);
            _history[i] = command with { State = state };
        }

        return state!.ReturnValue;
    }
}

