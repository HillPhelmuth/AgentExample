using Tiktoken;
namespace AgentExample.Models;

public class StringHelpers
{
    private static Encoding? _tokenizer;
    public static int GetTokens(string text)
    {
        _tokenizer ??= Encoding.ForModel("gpt-3.5-turbo");
        return _tokenizer.CountTokens(text);
    }
}