namespace Devlead.Statiq.Tabs;

public static class TabGroupExtensions
{
    public static Bootstrapper AddTabGroupShortCode(
        this Bootstrapper bootstrapper,
        string tabGroupShortcode = "TabGroup",
        string tabGroupCssShortcode = "TabGroupCss"
        )
    {
        bootstrapper.AddShortcode<TabGroupShortcode>(tabGroupShortcode);
        bootstrapper.AddShortcode<TabGroupCssShortcode>(tabGroupCssShortcode);
        
        bootstrapper.ConfigureEngine(engine =>
        {
            engine.Events.Subscribe<BeforePipelinePhaseExecution>(evt =>
            {
                using var resourceStream = typeof(TabGroupShortcode).Assembly.GetManifestResourceStream("Devlead.Statiq.Tabs.TabGroup.css");
                if (resourceStream == null)
                {
                    return;
                }

                var tabGroupCssPath = engine.FileSystem.RootPath
                    .Combine(engine.FileSystem.OutputPath)
                    .Combine("scss")
                    .GetFilePath("TabGroup.css");
                
                var file = engine.FileSystem.GetFile(
                    tabGroupCssPath
                );

                if (file.Exists)
                {
                    return;
                }
                
                using var tabGroupStream =  file.OpenWrite();
                
                resourceStream.CopyTo(tabGroupStream);
            });
        });

        return bootstrapper;
    }
}