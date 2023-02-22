using System.Diagnostics.CodeAnalysis;
using Spectre.Console.Cli;


namespace Devlead.Statiq.Themes;

public static class UriThemeExtensions
{
    public static Bootstrapper AddThemeFromUri(this Bootstrapper bootstrapper, Uri uri)
    {
        bootstrapper
            .ConfigureFileSystem(
                (fileSystem, settings, serviceCollection) =>
                {
                    FileSystemSetupCommand.Setup(fileSystem);

                    var path = fileSystem.RootPath.Combine("theme").Combine(Path.GetFileNameWithoutExtension(uri.LocalPath));

                    var directory = fileSystem.GetDirectory(path);

                    if (!directory.Exists)
                    {
                        using var clientHandler = new HttpClientHandler();
                        using var client = new HttpClient(clientHandler, false)
                        {
                            Timeout = TimeSpan.FromSeconds(60),
                        };

                        client.DefaultRequestHeaders.Add("User-Agent", $"Mozilla/5.0 AppleWebKit/605.1.15 Chrome/87.0.4272.0 Safari/604.1 Edg/87.0.654.0");
                        using var responseStream = client
                            .GetStreamAsync(uri)
                            .ConfigureAwait(false)
                            .GetAwaiter()
                            .GetResult();

                        using var zipStream =
                            new System.IO.Compression.ZipArchive(responseStream,
                                System.IO.Compression.ZipArchiveMode.Read);

                        foreach (var entry in zipStream.Entries.Where(entry =>
                            !string.IsNullOrWhiteSpace(entry.Name)))
                        {
                            var parentPath = Path.GetDirectoryName(entry.FullName);
                            var fileDir = string.IsNullOrWhiteSpace(parentPath)
                                ? directory
                                : directory.GetDirectory(parentPath);

                            var file = fileDir.GetFile(entry.Name);
                            using var entryStream = entry.Open();
                            using var fileStream = file.Open(true);
                            entryStream.CopyTo(fileStream);
                        }
                    }

                    var themePaths = directory
                        .GetDirectories()
                        .Where(dir => dir.GetDirectory("input").Exists)
                        .Select(dir => dir.Path)
                        .ToArray();

                    var themeManager = serviceCollection.GetRequiredImplementationInstance<ThemeManager>();

                    foreach (var themePath in themePaths)
                    {
                        themeManager.ThemePaths.Add(themePath);
                    }
                }
            );

        return bootstrapper;
    }
}


internal class FileSystemSetupCommand : Command<EmptyCommandSettings>
{
    private static readonly string[] Commands = new[] {
                                                    "pipelines",
                                                    "interactive",
                                                    "deploy",
                                                    "glob",
                                                    "serve"
                                                };
    internal static int Setup(IFileSystem fileSystem)
    {
        var app = new CommandApp();
        app.Configure(
            confgure =>
            {
                confgure.Settings.Registrar.RegisterInstance(fileSystem);
                Array.ForEach(
                    Commands,
                    command => confgure.AddCommand<FileSystemSetupCommand>(command)
                );
            });
        app.SetDefaultCommand<FileSystemSetupCommand>();
        var args = Environment.GetCommandLineArgs()
            .SkipWhile(
                arg => !arg.StartsWith("-") && !Commands.Contains(arg, StringComparer.OrdinalIgnoreCase)
                )
            .ToArray();
        return app.Run(args);
    }

    private IFileSystem FileSystem { get; }

    public FileSystemSetupCommand(IFileSystem fileSystem)
    {
        FileSystem = fileSystem;
    }
    public override int Execute([NotNull] CommandContext context, [NotNull] EmptyCommandSettings settings)
    {
        if (
            (context?.Remaining?.Parsed["root"].FirstOrDefault() ?? context?.Remaining?.Parsed["r"].FirstOrDefault())
                is string { Length: > 0} root
                &&
                new NormalizedPath(root) is { } rootPath 
                &&
                FileSystem.GetDirectory(rootPath.IsAbsolute ? rootPath : new NormalizedPath(Environment.CurrentDirectory).Combine(rootPath))
                    is IDirectory { Exists: true } rootDirectory
        )
        {
            FileSystem.RootPath = rootDirectory.Path;
        }
        return 0;
    }
}