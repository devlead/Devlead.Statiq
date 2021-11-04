using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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
            string overrideCodeLang = null,
            [CallerFilePath]
            string callerFilePath = null,
            [CallerLineNumber]
            int? callerLineNumber = null,
            [CallerMemberName]
            string callerMemberName = null
        )
        {
            string GetCallerInfo() => $"{Path.GetFileName(callerFilePath)}:{callerLineNumber}:{callerMemberName}";

            // If no path specified just skip
            if (string.IsNullOrWhiteSpace(filePath))
            {
                context.Logger.LogDebug(document, $"{GetCallerInfo()}: Empty file path specified skipping.");
                return;
            }

            var normalizedFilePath = new NormalizedPath(filePath) switch
            {
                // If relative try find absolute path based on document path
                {IsRelative: true} relativePath => document.Source.ChangeFileName(relativePath) switch
                    {
                        {IsAbsolute: true} changedPath => changedPath,
                        _ => relativePath
                    },

                // If absolute use that
                {IsAbsolute: true} absolutePath => absolutePath,

                // if neither assume invalid
                _ => NormalizedPath.Null
            };

            if (normalizedFilePath.IsNullOrEmpty)
            {
                context.Logger.LogWarning(document, $"{GetCallerInfo()}: Invalid path {filePath} specified skipping.");
                return;
            }

            var externalDocument = normalizedFilePath switch
                {
                    {IsAbsolute: true} file => context.FileSystem.GetFile(normalizedFilePath),
                    _ => null
                } switch
                {
                    {Exists: true} existingFile => existingFile,

                    // If file doesn't exists fallback on input file
                    _ => context.FileSystem.GetInputFile(filePath)

                };

            if (externalDocument?.Exists != true)
            {
                context.Logger.LogWarning(document, $"{GetCallerInfo()}: file path {filePath} not found.");
                return;
            }

            using var textReader = externalDocument.OpenText();
            var content = textReader
                .ReadToEnd();
            MarkdownHelper.RenderMarkdown(
                context,
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