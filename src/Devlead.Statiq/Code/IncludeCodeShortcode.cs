using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Devlead.Statiq.Markdown;
using Statiq.Common;

// ReSharper disable ClassNeverInstantiated.Global

namespace Devlead.Statiq.Code
{
    public class IncludeCodeShortcode : SyncShortcode
    {
        private const string Include = nameof(Include);
        private const string Configuration = nameof(Configuration);
        private const string PrependLinkRoot = nameof(PrependLinkRoot);
        
        public override ShortcodeResult Execute(KeyValuePair<string, string>[] args, string content, IDocument document,
            IExecutionContext context)
        {
            var dictionary = args.ToDictionary(Include, Configuration, PrependLinkRoot);
            var prependLinkRoot = dictionary.GetBool(PrependLinkRoot);
            var configuration = dictionary.GetString(Configuration, "advanced");
            var include = dictionary.GetString(Include);
            var overrideCodeLang = args
                .Where(key => StringComparer.OrdinalIgnoreCase.Equals("lang", key.Key))
                .Select(value => value.Value)
                .FirstOrDefault();

            using var writer = new StringWriter();
            document.RenderExternalDocument(
                context,
                writer,
                prependLinkRoot,
                configuration,
                overrideCodeLang: overrideCodeLang,
                isCodeBlock: true,
                filePath: include
            );

            return new ShortcodeResult(
                writer.ToString()
            );
        }
    }
}