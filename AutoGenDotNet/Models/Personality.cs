using System.ComponentModel;

namespace AutoGenDotNet.Models;

public enum Personality
{
    /// <summary>
    /// Just a standard AI bot
    /// </summary>
    [Description("Just a standard AI bot"), Temp(0.9f)]
    Default,

    /// <summary>
    /// Showing a lively, bold, and somewhat impudent spirit.
    /// </summary>
    [Description("Showing a lively, bold, and somewhat impudent spirit."), Temp(1.1f)]
    Sassy,

    /// <summary>
    /// Given to or characterized by joking. Funny.
    /// </summary>
    [Description("Given to or characterized by joking. Funny."), Temp(1.1f)]
    Jokey,

    /// <summary>
    /// Marked by comprehension, empathy, and sympathy.
    /// </summary>
    [Description("Marked by comprehension, empathy, and sympathy."), Temp(0.9f)]
    Understanding,

    /// <summary>
    /// Serving to instruct or inform; conveying instruction, knowledge, or information; enlightening, professorial.
    /// </summary>
    [Description("Serving to instruct or inform; conveying instruction, knowledge, or information; enlightening, professorial."), Temp(0.7f)]
    Instructive,

    /// <summary>
    /// Serious, grave, or solemn in manner, tone, or expression.
    /// </summary>
    [Description("Serious, grave, or solemn in manner, tone, or expression."), Temp(0.9f)]
    Somber,

    /// <summary>
    /// Fond of talking in an easy, familiar, and friendly manner; talkative.
    /// </summary>
    [Description("Fond of talking in an easy, familiar, and friendly manner; talkative."), Temp(1.0f)]
    Chatty,

    /// <summary>
    /// Mentally quick and resourceful; ingenious.
    /// </summary>
    [Description("Mentally quick and resourceful; ingenious."), Temp(1.1f)]
    Clever,

    /// <summary>
    /// Relaxed and tolerant in approach or manner; unhurried and unworried.
    /// </summary>
    [Description("Relaxed and tolerant in approach or manner; unhurried and unworried."), Temp(0.9f)]
    EasyGoing,

    /// <summary>
    /// Surly or ill-tempered; grumbling.
    /// </summary>
    [Description("Surly or ill-tempered; grumbling."), Temp(1.1f)]
    Grumpy,

    /// <summary>
    /// Having or showing a friendly, generous, and considerate nature.
    /// </summary>
    [Description("Having or showing a friendly, generous, and considerate nature."), Temp(0.7f)]
    Kind,

    /// <summary>
    /// Unpleasantly ill-natured. Nasty
    /// </summary>
    [Description("Unpleasantly ill-natured. Nasty"), Temp(1.1f)]
    Mean,

    /// <summary>
    /// Pleasant; agreeable; satisfactory.
    /// </summary>
    [Description("Pleasant; agreeable; satisfactory."), Temp(0.7f)]
    Nice,

    /// <summary>
    /// Having or showing polished manners, civility, and breeding.
    /// </summary>
    [Description("Having or showing polished manners, civility, and breeding."), Temp(0.7f)]
    Polite,

    /// <summary>
    /// Lacking civility or good manners; discourteous; impolite.
    /// </summary>
    [Description("Lacking civility or good manners; discourteous; impolite."), Temp(0.9f)]
    Rude,

    /// <summary>
    /// Easily frightened; timid.
    /// </summary>
    [Description("Easily frightened; timid."), Temp(0.7f)]
    Shy,

    /// <summary>
    /// Excessively proud of one's appearance or achievements.
    /// </summary>
    [Description("Excessively proud of one's appearance or achievements."), Temp(1.1f)]
    Vain,

    /// <summary>
    /// Having or showing good judgment or discernment; wise.
    /// </summary>
    [Description("Having or showing good judgment or discernment; wise."), Temp(0.9f)]
    Wise,

    /// <summary>
    /// Overestimates their abilities or importance and displays a sense of superiority or entitlement towards others.
    /// </summary>
    [Description("Overestimates their abilities or importance and displays a sense of superiority or entitlement towards others."), Temp(1.0f)]
    Arrogant,

    /// <summary>
    /// Speaks in a playful or humorous way, often in a manner that is lighthearted and not serious
    /// </summary>
    [Description("Speaks in a playful or humorous way, often in a manner that is lighthearted and not serious"), Temp(1.1f)]
    Silly,

    /// <summary>
    /// Having or showing a strong desire and determination to succeed.
    /// </summary>
    [Description("Having or showing a strong desire and determination to succeed."), Temp(0.9f)]
    Passionate,

    /// <summary>
    /// Being characterized by a strong influence on others to excel, perform, or to be creative.
    /// </summary>
    [Description("Being characterized by a strong influence on others to excel, perform, or to be creative."), Temp(1.0f)]
    Inspiring,

    /// <summary>
    /// Full of energy and enthusiasm.
    /// </summary>
    [Description("Full of energy and enthusiasm."), Temp(0.9f)]
    Energetic,

    /// <summary>
    /// Curious, eager for knowledge.
    /// </summary>
    [Description("Curious, eager for knowledge."), Temp(0.9f)]
    Inquisitive,

    /// <summary>
    /// Using logical reasoning, systematic and exact.
    /// </summary>
    [Description("Using logical reasoning, systematic and exact."), Temp(0.6f)]
    Analytical,

    /// <summary>
    /// Showing a natural creative skill, appreciating art and beauty.
    /// </summary>
    [Description("Showing a natural creative skill, appreciating art and beauty."), Temp(1.1f)]
    Artistic,

    /// <summary>
    /// Being understanding and sensitive to others' feelings.
    /// </summary>
    [Description("Being understanding and sensitive to others' feelings."), Temp(0.9f)]
    Empathetic,

    /// <summary>
    /// Able to endure waiting, delay, or provocation without becoming annoyed or upset.
    /// </summary>
    [Description("Able to endure waiting, delay, or provocation without becoming annoyed or upset."), Temp(0.9f)]
    Patient,

    /// <summary>
    /// As the character acts
    /// </summary>
    [Description("As the character acts"), Temp(1.0f)]
    Character,

    /// <summary>
    /// A Code Generating bot
    /// </summary>
    [Description("A Code Generating bot"), Temp(0.1f)]
    CodeGenerator
}
public enum ImageOwner { Bot, User }

[AttributeUsage(AttributeTargets.Field)]
public class TempAttribute : Attribute
{
    public float Temperature { get; set; }

    public TempAttribute(float temperature)
    {
        Temperature = temperature;
    }
}