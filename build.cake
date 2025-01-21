// Install .NET Core Global tools.
#tool dotnet:?package=GitVersion.Tool&version=6.1.0

#load "build/records.cake"
#load "build/helpers.cake"

/*****************************
 * Setup
 *****************************/
Setup(
    static context => {

        var assertedVersions = context.GitVersion(new GitVersionSettings
            {
                OutputType = GitVersionOutput.Json
            });

        var gh = context.GitHubActions();
        var version = assertedVersions.FullSemVer;
        var branchName = assertedVersions.BranchName;
        var isMainBranch = StringComparer.OrdinalIgnoreCase.Equals("main", branchName);
        var configuration = context.Argument("configuration", "Release");

        context.Information("Building version {0} (Branch: {1}, IsMain: {2})",
            version,
            branchName,
            isMainBranch);

        var artifactsPath = context
                            .MakeAbsolute(context.Directory("./artifacts"));

        var projectRootPath = context
                                .MakeAbsolute(context.Directory("src"));

        return new BuildData(
            version,
            isMainBranch,
            projectRootPath,
            configuration,
            new DotNetMSBuildSettings()
                .SetConfiguration(configuration)
                .SetVersion(version)
                .WithProperty("Copyright", $"Mattias Karlsson © {DateTime.UtcNow.Year}")
                .WithProperty("Authors", "devlead")
                .WithProperty("Company", "devlead")
                .WithProperty("PackageLicenseExpression", "MIT")
                .WithProperty("PackageTags", "Statiq;Extensions;StaticContent;StaticSite;Blog;BlogEngine")
                .WithProperty("PackageDescription", "Provides helpers and extensions for the static site generator Statiq, i.e. themes from http uri and TabGroup shortcode.")
                .WithProperty("PackageIconUrl", "https://cdn.jsdelivr.net/gh/devlead/Devlead.Console.Template/src/devlead.png")
                .WithProperty("PackageIcon", "devlead.png")
                .WithProperty("PackageProjectUrl", "https://www.devlead.se")
                .WithProperty("RepositoryUrl", "https://github.com/devlead/Devlead.Statiq.git")
                .WithProperty("RepositoryType", "git")
                .WithProperty("ContinuousIntegrationBuild", gh.IsRunningOnGitHubActions ? "true" : "false")
                .WithProperty("EmbedUntrackedSources", "true"),
            artifactsPath,
            artifactsPath.Combine(version),
            "Devlead.Statiq.TestWeb",
            new [] {
                "net8.0"
            }
            );
    }
);

/*****************************
 * Tasks
 *****************************/
Task("Clean")
    .Does<BuildData>(
        static (context, data) => context.CleanDirectories(data.DirectoryPathsToClean)
    )
.Then("Restore")
    .Does<BuildData>(
        static (context, data) => context.DotNetRestore(
            data.ProjectRoot.FullPath,
            new DotNetRestoreSettings {
                MSBuildSettings = data.MSBuildSettings
            }
        )
    )
.Then("DPI")
    .Does<BuildData>(
        static (context, data) => context.DotNetTool(
                "tool",
                new DotNetToolSettings {
                    HandleExitCode = exitCode => true,
                    ArgumentCustomization = args => args
                                                        .Append("run")
                                                        .Append("dpi")
                                                        .Append("nuget")
                                                        .Append("--silent")
                                                        .AppendSwitchQuoted("--output", "table")
                                                        .Append(
                                                            (
                                                                !string.IsNullOrWhiteSpace(context.EnvironmentVariable("NuGetReportSettings_SharedKey"))
                                                                &&
                                                                !string.IsNullOrWhiteSpace(context.EnvironmentVariable("NuGetReportSettings_WorkspaceId"))
                                                            )
                                                                ? "report"
                                                                : "analyze"
                                                            )
                                                        .AppendSwitchQuoted("--buildversion", data.Version)
                }
            )
    )
.Then("Build")
    .Does<BuildData>(
        static (context, data) => context.DotNetBuild(
            data.ProjectRoot.FullPath,
            new DotNetBuildSettings {
                NoRestore = true,
                MSBuildSettings = data.MSBuildSettings
            }
        )
    )
.Then("Test")
    .DoesForEach<BuildData, string>(
        static (data, context) => data.TestTargetFrameworks,
        static (data, item, context) => {
            context.Information("Testing target framework {0}", item);
            context.DotNetRun(
                data.TestProjectPath.FullPath,
                new DotNetRunSettings
                {
                    Configuration = data.Configuration,
                    Framework = item,
                    NoRestore = true,
                    NoBuild = true,
                    ArgumentCustomization = args => args
                                                .Append("--")
                                                .AppendSwitch("-l", "Warning")
                                                .AppendSwitchQuoted("--root", data.IntegrationTestPath.Combine(item).FullPath)
                                                .AppendSwitchQuoted("--input",data.TestProjectPath.Combine("input").FullPath)
                }
            );
        }
    )
    .Default()
.Then("Pack")
    .Does<BuildData>(
        static (context, data) => context.DotNetPack(
            data.ProjectRoot.FullPath,
            new DotNetPackSettings {
                NoBuild = true,
                NoRestore = true,
                OutputDirectory = data.NuGetOutputPath,
                MSBuildSettings = data.MSBuildSettings
            }
        )
    )
.Then("Upload-Artifacts")
    .WithCriteria<BuildData>( (context, data) => data.ShouldPushGitHubPackages())
    .Does<BuildData>(
        static (context, data) => context
            .GitHubActions()
            .Commands
            .UploadArtifact(data.ArtifactsPath, "artifacts")
    )
.Then("Push-GitHub-Packages")
    .WithCriteria<BuildData>( (context, data) => data.ShouldPushGitHubPackages())
    .DoesForEach<BuildData, FilePath>(
        static (data, context)
            => context.GetFiles(data.NuGetOutputPath.FullPath + "/*.nupkg"),
        static (data, item, context)
            => context.DotNetNuGetPush(
                item.FullPath,
            new DotNetNuGetPushSettings
            {
                Source = data.GitHubNuGetSource,
                ApiKey = data.GitHubNuGetApiKey
            }
        )
    )
.Then("Push-NuGet-Packages")
    .WithCriteria<BuildData>( (context, data) => data.ShouldPushNuGetPackages())
    .DoesForEach<BuildData, FilePath>(
        static (data, context)
            => context.GetFiles(data.NuGetOutputPath.FullPath + "/*.nupkg"),
        static (data, item, context)
            => context.DotNetNuGetPush(
                item.FullPath,
                new DotNetNuGetPushSettings
                {
                    Source = data.NuGetSource,
                    ApiKey = data.NuGetApiKey
                }
        )
    )
.Then("Create-GitHub-Release")
    .WithCriteria<BuildData>( (context, data) => data.ShouldPushNuGetPackages())
    .Does<BuildData>(
        static (context, data) => context
            .Command(
                new CommandSettings {
                    ToolName = "GitHub CLI",
                    ToolExecutableNames = new []{ "gh.exe", "gh" },
                    EnvironmentVariables = { { "GH_TOKEN", data.GitHubNuGetApiKey } }
                },
                new ProcessArgumentBuilder()
                    .Append("release")
                    .Append("create")
                    .Append(data.Version)
                    .AppendSwitchQuoted("--title", data.Version)
                    .Append("--generate-notes")
                    .Append(string.Join(
                        ' ',
                        context
                            .GetFiles(data.NuGetOutputPath.FullPath + "/*.nupkg")
                            .Select(path => path.FullPath.Quote())
                        ))

            )
    )
.Then("GitHub-Actions")
.Run();