using System.Text.Json;
using Gccs.Application.Reports;
using Microsoft.Extensions.Options;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Fonts;

namespace Gccs.Infrastructure.Reports;

public sealed class ReportPdfOptions
{
    public const string SectionName = "ReportPdf";

    public string FontDirectory { get; set; } = string.Empty;
}

public sealed class MigraDocReportPdfRenderer : IReportPdfRenderer
{
    private static readonly object FontResolverLock = new();

    public MigraDocReportPdfRenderer(IOptions<ReportPdfOptions> options)
    {
        EnsureFontResolver(options.Value.FontDirectory);
    }

    public RenderedReportPdf Render(ReportArtifactDetailDto report)
    {
        var document = new Document();
        document.Info.Title = report.Title;
        document.Info.Subject = "FeDril workflow guidance report";
        document.Info.Author = "FeDril";

        var normal = document.Styles[StyleNames.Normal] ?? throw new InvalidOperationException("PDF normal style is unavailable.");
        normal.Font.Name = FeDrilFontResolver.FamilyName;
        normal.Font.Size = 8.5;
        normal.ParagraphFormat.SpaceAfter = Unit.FromPoint(3);

        var heading1 = document.Styles[StyleNames.Heading1] ?? throw new InvalidOperationException("PDF heading style is unavailable.");
        heading1.Font.Name = FeDrilFontResolver.FamilyName;
        heading1.Font.Size = 14;
        heading1.Font.Bold = true;
        heading1.Font.Color = Color.FromRgb(25, 74, 55);
        heading1.ParagraphFormat.SpaceBefore = Unit.FromPoint(9);
        heading1.ParagraphFormat.SpaceAfter = Unit.FromPoint(4);
        heading1.ParagraphFormat.KeepWithNext = true;

        var heading2 = document.Styles[StyleNames.Heading2] ?? throw new InvalidOperationException("PDF heading style is unavailable.");
        heading2.Font.Name = FeDrilFontResolver.FamilyName;
        heading2.Font.Size = 11;
        heading2.Font.Bold = true;
        heading2.Font.Color = Color.FromRgb(25, 74, 55);
        heading2.ParagraphFormat.SpaceBefore = Unit.FromPoint(6);
        heading2.ParagraphFormat.SpaceAfter = Unit.FromPoint(3);
        heading2.ParagraphFormat.KeepWithNext = true;

        var section = document.AddSection();
        section.PageSetup.PageFormat = PageFormat.Letter;
        section.PageSetup.TopMargin = Unit.FromInch(0.7);
        section.PageSetup.BottomMargin = Unit.FromInch(0.5);
        section.PageSetup.HeaderDistance = Unit.FromInch(0.25);
        section.PageSetup.LeftMargin = Unit.FromInch(0.7);
        section.PageSetup.RightMargin = Unit.FromInch(0.7);

        var header = section.Headers.Primary.AddParagraph("FeDril - No-CUI compliance management");
        header.Format.Font.Name = FeDrilFontResolver.FamilyName;
        header.Format.Font.Size = 8;
        header.Format.Font.Color = Colors.DimGray;

        var footer = section.Footers.Primary.AddParagraph();
        footer.Format.Alignment = ParagraphAlignment.Center;
        footer.Format.Font.Name = FeDrilFontResolver.FamilyName;
        footer.Format.Font.Size = 8;
        footer.AddText("Page ");
        footer.AddPageField();
        footer.AddText(" of ");
        footer.AddNumPagesField();

        section.AddParagraph(report.Title, StyleNames.Heading1);
        var metadata = section.AddTable();
        metadata.Borders.Width = Unit.FromPoint(0.4);
        metadata.Borders.Color = Colors.LightGray;
        metadata.AddColumn(Unit.FromInch(1.55));
        metadata.AddColumn(Unit.FromInch(5.45));
        AddMetadataRow(metadata, "Report type", Humanize(report.Type.ToString()));
        AddMetadataRow(metadata, "Status", Humanize(report.Status.ToString()));
        AddMetadataRow(metadata, "Generated", report.GeneratedAt.ToUniversalTime().ToString("yyyy-MM-dd HH:mm 'UTC'"));
        AddMetadataRow(metadata, "Report ID", report.Id.ToString());

        var disclaimer = section.AddParagraph();
        disclaimer.Format.SpaceBefore = Unit.FromPoint(10);
        disclaimer.Format.SpaceAfter = Unit.FromPoint(10);
        disclaimer.Format.Shading.Color = Color.FromRgb(244, 246, 245);
        disclaimer.Format.LeftIndent = Unit.FromPoint(8);
        disclaimer.Format.RightIndent = Unit.FromPoint(8);
        disclaimer.AddFormattedText("Important: ", TextFormat.Bold);
        disclaimer.AddText(report.Disclaimer);

        section.AddParagraph("Report content", StyleNames.Heading1);
        AppendJson(section, report.Snapshot, 0, null, new PdfRenderBudget());

        var renderer = new PdfDocumentRenderer { Document = document };
        renderer.RenderDocument();
        using var stream = new MemoryStream();
        renderer.PdfDocument.Save(stream, closeStream: false);
        return new RenderedReportPdf(stream.ToArray(), "application/pdf");
    }

    private static void AddMetadataRow(Table table, string label, string value)
    {
        var row = table.AddRow();
        row.TopPadding = Unit.FromPoint(3);
        row.BottomPadding = Unit.FromPoint(3);
        row.Cells[0].AddParagraph(label).Format.Font.Bold = true;
        row.Cells[1].AddParagraph(value);
    }

    private static void AppendJson(Section section, JsonElement element, int depth, string? label, PdfRenderBudget budget)
    {
        if (!budget.TryConsumeNode())
        {
            AddValue(section, null, "Additional report content omitted at the PDF safety limit.");
            return;
        }

        if (depth > 8)
        {
            AddValue(section, label, "Additional nested content omitted.");
            return;
        }

        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                if (!string.IsNullOrWhiteSpace(label))
                {
                    section.AddParagraph(Humanize(label), depth < 2 ? StyleNames.Heading1 : StyleNames.Heading2);
                }

                foreach (var property in element.EnumerateObject())
                {
                    if (budget.IsExhausted)
                    {
                        break;
                    }
                    AppendJson(section, property.Value, depth + 1, property.Name, budget);
                }
                break;
            case JsonValueKind.Array:
                if (!string.IsNullOrWhiteSpace(label))
                {
                    section.AddParagraph(Humanize(label), StyleNames.Heading2);
                }

                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    if (budget.IsExhausted || index++ >= 2_000)
                    {
                        AddValue(section, null, "Additional items omitted at the PDF safety limit.");
                        break;
                    }

                    if (item.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
                    {
                        AppendJson(section, item, depth + 1, $"Item {index}", budget);
                    }
                    else
                    {
                        var paragraph = section.AddParagraph();
                        paragraph.Format.LeftIndent = Unit.FromPoint(12);
                        paragraph.AddText($"- {ScalarText(item, budget)}");
                    }
                }
                break;
            default:
                AddValue(section, label, ScalarText(element, budget));
                break;
        }
    }

    private static void AddValue(Section section, string? label, string value)
    {
        var paragraph = section.AddParagraph();
        if (!string.IsNullOrWhiteSpace(label))
        {
            paragraph.AddFormattedText($"{Humanize(label)}: ", TextFormat.Bold);
        }
        paragraph.AddText(value);
    }

    private static string ScalarText(JsonElement element, PdfRenderBudget budget)
    {
        var value = element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.True => "Yes",
            JsonValueKind.False => "No",
            JsonValueKind.Null or JsonValueKind.Undefined => "Not provided",
            _ => element.GetRawText()
        };
        return budget.LimitText(value);
    }

    private static string Humanize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Untitled";
        }

        var characters = new List<char>(value.Length + 8);
        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];
            if (index > 0 && char.IsUpper(character) && !char.IsUpper(value[index - 1]))
            {
                characters.Add(' ');
            }
            characters.Add(character);
        }

        var result = new string(characters.ToArray()).Replace('_', ' ').Trim();
        return char.ToUpperInvariant(result[0]) + result[1..];
    }

    private static void EnsureFontResolver(string configuredDirectory)
    {
        lock (FontResolverLock)
        {
            if (GlobalFontSettings.FontResolver is not null)
            {
                return;
            }

            GlobalFontSettings.FontResolver = FeDrilFontResolver.Create(configuredDirectory);
        }
    }
}

internal sealed class PdfRenderBudget
{
    private const int MaximumNodes = 5_000;
    private const int MaximumCharacters = 500_000;
    private const int MaximumScalarCharacters = 4_000;
    private int nodes;
    private int characters;

    public bool IsExhausted => nodes >= MaximumNodes || characters >= MaximumCharacters;

    public bool TryConsumeNode() => ++nodes <= MaximumNodes;

    public string LimitText(string value)
    {
        var remaining = Math.Max(0, MaximumCharacters - characters);
        var allowed = Math.Min(Math.Min(value.Length, MaximumScalarCharacters), remaining);
        characters += allowed;
        if (allowed == value.Length)
        {
            return value;
        }

        return allowed == 0 ? "Content omitted at the PDF safety limit." : value[..allowed] + "…";
    }
}

internal sealed class FeDrilFontResolver : IFontResolver
{
    public const string FamilyName = "FeDril Sans";
    private const string RegularFace = "FeDrilSans-Regular";
    private const string BoldFace = "FeDrilSans-Bold";
    private readonly byte[] regular;
    private readonly byte[] bold;

    private FeDrilFontResolver(byte[] regular, byte[] bold)
    {
        this.regular = regular;
        this.bold = bold;
    }

    public static FeDrilFontResolver Create(string configuredDirectory)
    {
        var directories = new[]
        {
            configuredDirectory,
            "/usr/share/fonts/truetype/dejavu",
            "/usr/share/fonts/truetype/liberation2",
            "/System/Library/Fonts/Supplemental",
            Environment.GetFolderPath(Environment.SpecialFolder.Fonts)
        }.Where(directory => !string.IsNullOrWhiteSpace(directory)).Distinct(StringComparer.Ordinal).ToArray();

        foreach (var directory in directories)
        {
            var pair = FindFontPair(directory);
            if (pair is not null)
            {
                return new FeDrilFontResolver(File.ReadAllBytes(pair.Value.Regular), File.ReadAllBytes(pair.Value.Bold));
            }
        }

        throw new InvalidOperationException(
            "PDF report generation requires Arial, DejaVu Sans, or Liberation Sans regular and bold TrueType fonts. Configure ReportPdf:FontDirectory.");
    }

    public byte[]? GetFont(string faceName) => faceName == BoldFace ? bold : faceName == RegularFace ? regular : null;

    public FontResolverInfo? ResolveTypeface(string familyName, bool isBold, bool isItalic) =>
        new(isBold ? BoldFace : RegularFace, mustSimulateBold: false, mustSimulateItalic: isItalic);

    private static (string Regular, string Bold)? FindFontPair(string directory)
    {
        var candidates = new[]
        {
            ("Arial.ttf", "Arial Bold.ttf"),
            ("DejaVuSans.ttf", "DejaVuSans-Bold.ttf"),
            ("LiberationSans-Regular.ttf", "LiberationSans-Bold.ttf")
        };
        foreach (var (regular, bold) in candidates)
        {
            var regularPath = Path.Combine(directory, regular);
            var boldPath = Path.Combine(directory, bold);
            if (File.Exists(regularPath) && File.Exists(boldPath))
            {
                return (regularPath, boldPath);
            }
        }

        return null;
    }
}
