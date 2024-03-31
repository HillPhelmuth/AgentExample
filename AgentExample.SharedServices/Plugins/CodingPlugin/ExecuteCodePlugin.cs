using AgentExample.SharedServices.Services;
using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgentExample.SharedServices.Plugins.CodingPlugin
{
    public class ExecuteCodePlugin
    {
        private readonly CompilerService _compilerService = new();
        private readonly ScriptService _scriptService = new();
        [KernelFunction, Description("Execute provided c# code. Returns the console output")]
        [return: Description("Console output after execution")]
        public async Task<string> ExecuteCode([Description("C# code to execute")] string input)
        {
            input = input.Replace("```csharp", "").Replace("```", "").TrimStart('\n');
            var result = await _compilerService.SubmitCode(input, CompileResources.PortableExecutableReferences);
            return result;
        }
        [KernelFunction, Description("Execute provided c# code. Returns the script output")]
        public async Task<string> ExecuteScript([Description("C# code to execute")] string input)
        {
            input = input.Replace("```csharp", "").Replace("```", "").TrimStart('\n');
            var result = await _scriptService.EvaluateAsync(input);
            return result;
        }
        [KernelFunction, Description("Compiles code to test it's validity")]
        [return: Description("Success or Error message")]
        public async Task<string> EvaluateCode(string code)
        {
            var result = await _scriptService.CompileAndEval(code.Replace("```csharp", "").Replace("```", "").TrimStart('\n'));
            return result;
        }
    }
}
