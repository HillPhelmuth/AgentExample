using System.Text.Json;
using AutoGen;
using AutoGen.Core;

namespace AutoGenDotNet;

/// <summary>
/// 
/// </summary>
public partial class CodeRunnerFunction
{
    /// <summary>
    /// 
    /// </summary>
    public struct CodeReviewResult
    {
        public bool HasMultipleCodeBlocks { get; set; }
        public bool IsTopLevelStatement { get; set; }
        public bool IsDotnetCodeBlock { get; set; }
        public bool IsPrintResultToConsole { get; set; }
    }

    /// <summary>
    /// review code block
    /// </summary>
    /// <param name="hasMultipleCodeBlocks">true if there're multipe csharp code blocks</param>
    /// <param name="isTopLevelStatement">true if the code is in top level statement</param>
    /// <param name="isDotnetCodeBlock">true if the code block is csharp code block</param>
    /// <param name="isPrintResultToConsole">true if the code block print out result to console</param>
    [Function]
    public async Task<string> ReviewCodeBlock(
        bool hasMultipleCodeBlocks,
        bool isTopLevelStatement,
        bool isDotnetCodeBlock,
        bool isPrintResultToConsole)
    {
        var obj = new CodeReviewResult
        {
            HasMultipleCodeBlocks = hasMultipleCodeBlocks,
            IsTopLevelStatement = isTopLevelStatement,
            IsDotnetCodeBlock = isDotnetCodeBlock,
            IsPrintResultToConsole = isPrintResultToConsole,
        };

        return JsonSerializer.Serialize(obj);
    }
}