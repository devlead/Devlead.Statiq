using System.Text;
using Devlead.Statiq.Markdown;
using Statiq.Markdown;

// ReSharper disable ClassNeverInstantiated.Global

namespace Devlead.Statiq.Tabs;

public class TabGroupShortcode : SyncShortcode
{
    private const string Configuration = nameof(Configuration);
    private const string PrependLinkRoot = nameof(PrependLinkRoot);
    public override ShortcodeResult Execute(KeyValuePair<string, string>[] args, string content, IDocument document, IExecutionContext context)
    {
        var dictionary = args.ToDictionary(Configuration, PrependLinkRoot);
        var prependLinkRoot = dictionary.GetBool(PrependLinkRoot);
        var configuration = dictionary.GetString(Configuration, "advanced");

        var contentBuilder = new StringBuilder();

        var deserializer = new YamlDotNet.Serialization.DeserializerBuilder()
            .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.CamelCaseNamingConvention.Instance)
            .Build();

        var tabGroup = deserializer.Deserialize<TabGroup>(content);


        contentBuilder.AppendLine("<div class=\"tab-wrap\">");


        var first = true;
        foreach(var tab in tabGroup.Tabs)
        {
            contentBuilder.AppendLine($"<input type=\"radio\" id=\"{tabGroup.Id}-{tab.Id}\" name=\"{tabGroup.Id}\" class=\"tab\" {(first ? "checked" : string.Empty)}><label for=\"{tabGroup.Id}-{tab.Id}\" >{tab.Name}</label>");
            first = false;
        }

        foreach(var tab in tabGroup.Tabs)
        {
            contentBuilder.AppendLine("<div class=\"tab__content\">");
            contentBuilder.AppendLine();

            using (var writer = new StringWriter())
            {
                if (!string.IsNullOrWhiteSpace(tab.Content))
                {
                    MarkdownHelper.RenderMarkdown(
                        context,
                        document,
                        tab.Content,
                        writer,
                        prependLinkRoot,
                        true,
                        false,
                        configuration,
                        null
                    );
                }

                document.RenderExternalDocument(
                    context,
                    writer,
                    prependLinkRoot,
                    configuration,
                    isCodeBlock: false,
                    filePath: tab.Include
                );

                document.RenderExternalDocument(
                    context,
                    writer,
                    prependLinkRoot,
                    configuration,
                    overrideCodeLang: tab.CodeLang,
                    isCodeBlock: true,
                    filePath: tab.Code
                    );

                contentBuilder.AppendLine(writer.ToString());
            }

            contentBuilder.AppendLine();
            contentBuilder.AppendLine("</div>");
        }

        contentBuilder.AppendLine("</div>");

        return new ShortcodeResult(
            contentBuilder.ToString()
        );
    }
}
