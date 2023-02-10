namespace Devlead.Statiq.Tabs;

public class TabGroupCssShortcode : SyncShortcode
{
    public override ShortcodeResult Execute(KeyValuePair<string, string>[] args, string content, IDocument document,
        IExecutionContext context)
        => new($"<link href=\"{context.Settings.GetPath(Keys.LinkRoot)}/scss/TabGroup.css\" rel=\"stylesheet\" type=\"text/css\">");
}