return await Bootstrapper
            .Factory
            .CreateDefault(args)
            .AddThemeFromUri(new Uri("https://github.com/statiqdev/CleanBlog/archive/8543531ff5acfb9db97b88ec7121c693f198f942.zip"))
            .AddWeb()
            .AddTabGroupShortCode()
            .AddIncludeCodeShortCode()
            .RunAsync();