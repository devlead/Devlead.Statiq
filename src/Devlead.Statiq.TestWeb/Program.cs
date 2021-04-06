using System;
using Devlead.Statiq.Tabs;
using Devlead.Statiq.Themes;
using Statiq.App;
using Statiq.Web;

await Bootstrapper
    .Factory
    .CreateDefault(args)
    .AddThemeFromUri(new Uri("https://github.com/statiqdev/CleanBlog/archive/ceb5055f3d0f7a330708494ed21eb469cde62ce2.zip"))
    .AddWeb()
    .AddTabGroupShortCode()
    .RunAsync();
