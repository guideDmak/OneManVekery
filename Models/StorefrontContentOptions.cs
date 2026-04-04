namespace OneManVekery.Models;

public sealed class StorefrontContentOptions
{
    public StorefrontLayoutOptions Layout { get; set; } = new();

    public List<StorefrontFeatureOption> Features { get; set; } = [];

    public List<StorefrontPaymentOption> PaymentOptions { get; set; } = [];

    public List<StorefrontOrderStatusStepOption> OrderStatusSteps { get; set; } = [];

    public StorefrontAboutOptions About { get; set; } = new();

    public StorefrontContactOptions Contact { get; set; } = new();
}

public sealed class StorefrontLayoutOptions
{
    public string BrandTagline { get; set; } = string.Empty;

    public string FooterAddress { get; set; } = string.Empty;

    public string NewsletterTitle { get; set; } = string.Empty;

    public List<string> HelpItems { get; set; } = [];
}

public sealed class StorefrontAboutOptions
{
    public string StoryTitle { get; set; } = string.Empty;

    public List<string> StoryParagraphs { get; set; } = [];

    public string Quote { get; set; } = string.Empty;

    public string QuoteCaption { get; set; } = string.Empty;

    public List<StorefrontFeatureOption> Values { get; set; } = [];

    public List<StorefrontProcessStepOption> Steps { get; set; } = [];
}

public sealed class StorefrontContactOptions
{
    public string HeadingTitle { get; set; } = string.Empty;

    public string HeadingDescription { get; set; } = string.Empty;

    public List<StorefrontContactCardOption> Cards { get; set; } = [];
}

public sealed class StorefrontFeatureOption
{
    public string IconText { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}

public sealed class StorefrontPaymentOption
{
    public string Code { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}

public sealed class StorefrontOrderStatusStepOption
{
    public string Code { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Marker { get; set; } = string.Empty;

    public string CurrentDescription { get; set; } = string.Empty;
}

public sealed class StorefrontProcessStepOption
{
    public string Number { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}

public sealed class StorefrontContactCardOption
{
    public string IconText { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string LineOne { get; set; } = string.Empty;

    public string LineTwo { get; set; } = string.Empty;
}
