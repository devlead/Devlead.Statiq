using System;
using System.IO;
using System.Linq;
using Devlead.Statiq.Tabs;
using Statiq.Common;
using Statiq.Markdown;

namespace Devlead.Statiq.Markdown
{
    internal static class MarkdownExtensions
    {
        internal static void RenderExternalDocument(
            this IDocument document,
            IExecutionState context,
            TextWriter writer,
            bool prependLinkRoot,
            string configuration,
            string filePath,
            bool isCodeBlock,
            string overrideCodeLang = null
        )
        {
            if (string.IsNullOrWhiteSpace(filePath) ||
                context.FileSystem.GetFile(document.Source.ChangeFileName(filePath)) is not
                    {Exists: true} externalDocument
            )
            {
                return;
            }

            using var textReader = externalDocument.OpenText();
            var content = textReader
                .ReadToEnd();
            MarkdownHelper.RenderMarkdown(
                document,
                isCodeBlock
                    ? string.Join(
                        Environment.NewLine,
                        string.Concat("```",
                            overrideCodeLang ?? MarkdownLanguages.LanguageLookup[externalDocument.Path.Extension]
                                .FirstOrDefault() ??
                            "text"),
                        content
                            .TrimEnd(),
                        "```"
                    )
                    : content,
                writer,
                prependLinkRoot,
                configuration,
                null
            );
        }
    }
}