using System.Text.Json.Serialization;
using AutoGenDotNet.Models.Helpers;

namespace AutoGenDotNet.Models;

/// <summary>
/// Represents a bot model.
/// </summary>
public record BotModel
{
    /// <summary>
    /// Gets or sets the ID of the bot.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the name of the bot.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the description of the bot.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the personality of the bot.
    /// </summary>
    public Personality Personality { get; set; }

    /// <summary>
    /// Gets or sets the secondary personality of the bot.
    /// </summary>
    public Personality SecondaryPersonality { get; set; }

    /// <summary>
    /// Gets or sets the image of the bot.
    /// </summary>
    [JsonIgnore]
    public byte[]? Image { get; set; }

    /// <summary>
    /// Gets or sets the image as Base64 string of the bot.
    /// </summary>
    [JsonPropertyName("ImageBase64")]
    public string? ImageAsBase64 { get; set; }

    /// <summary>
    /// Gets the image source of the bot.
    /// </summary>
    public string? ImageSrc => !string.IsNullOrEmpty(ImagePath) ? ImagePath : $"data:image/png;base64,{ImageAsBase64}";

    /// <summary>
    /// Gets or sets the image path of the bot.
    /// </summary>
    [JsonPropertyName("ImagePath")]
    public string? ImagePath { get; set; }

    /// <summary>
    /// Gets or sets the image name of the bot.
    /// </summary>
    public string? ImageName { get; set; }

    /// <summary>
    /// Gets or sets the opening prompt of the bot.
    /// </summary>
    public string? OpeningPrompt { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the bot is clean.
    /// </summary>
    public bool CleanBot { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the bot should auto run.
    /// </summary>
    public bool AutoRun { get; set; } = true;

    /// <summary>
    /// Generates a bot character with the specified name and series.
    /// </summary>
    /// <param name="name">The name of the bot character.</param>
    /// <param name="series">The tv series or movie of the bot character.</param>
    /// <returns>A new instance of the <see cref="BotModel"/> class representing the generated bot character.</returns>
    public static BotModel GenerateCharacter(string name, string series)
    {
        return new BotModel
        {
            Name = name,
            Description = series,
            Personality = Personality.Character
        };
    }
    /// <summary>
    /// Converts a pre-made persona to an agent prompt
    /// </summary>
    /// <returns>persona prompt text</returns>
    public async Task<string> GenerateBotPrompt()
    {
       return await PromptBuilder.GeneratePrompt(this);
    }
    /// <summary>
    /// Gets all premade bots.
    /// </summary>
    /// <returns></returns>
    public static IEnumerable<BotModel> GetAllPremadeBots()
    {
        return StaticHelpers.ExtractFromAssembly<List<BotModel>>("PremadePersonas.json") ?? Enumerable.Empty<BotModel>();
    }
    /// <summary>
    /// Get a premade bot by name
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    /// <exception cref="KeyNotFoundException"></exception>
    public static BotModel GetPremadeBot(string name)
    {
        return GetAllPremadeBots().FirstOrDefault(b => b.Name == name) ?? throw new KeyNotFoundException("No Premade bot found with that name");
    }
}
