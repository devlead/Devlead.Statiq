#!/usr/bin/env dotnet
#:sdk Cake.Sdk@6.3.0
#:property IncludeAdditionalFiles=./build/*.cs

/*****************************
 * Setup
 *****************************/
Setup<BuildData>(
    static context => {
        InstallTool("dotnet:https://api.nuget.org/v3/index.json?package=DPI&version=2026.10.16.523");
        InstallTool("dotnet:https://api.nuget.org/v3/index.json?package=GitVersion.Tool&version=6.8.2");

        var assertedVersions = context.GitVersion(new GitVersionSettings
            {
                OutputType = GitVersionOutput.Json
            });

        var buildDate = DateTime.UtcNow;
        var runNumber = GitHubActions.IsRunningOnGitHubActions
                            ? GitHubActions.Environment.Workflow.RunNumber
                            : (short)((buildDate - buildDate.Date).TotalSeconds/3);
        var version = FormattableString
                        .Invariant($"{buildDate:yyyy.M.d}.{runNumber}");

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
            GitHubActions.IsRunningOnGitHubActions,
            GitHubActions.IsRunningOnGitHubActions ? GitHubActions.Environment.Workflow.Ref : null,
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
                .WithProperty("ContinuousIntegrationBuild", GitHubActions.IsRunningOnGitHubActions ? "true" : "false")
                .WithProperty("EmbedUntrackedSources", "true"),
            artifactsPath,
            artifactsPath.Combine(version),
            "Devlead.Statiq.TestWeb",
            [
                "net10.0"
            ]
            );
    }
);

if (BuildSystem.GitHubActions.IsRunningOnGitHubActions)
{
    TaskSetup(context=> System.Console.WriteLine($"::group::{context.Task.Name.Quote()}"));
    TaskTeardown(context=>System.Console.WriteLine("::endgroup::"));
}

/*****************************
 * Tasks
 *****************************/
Task("NuGet-Login")
    .WithCriteria<BuildData>(static (_, data) => data.ShouldLoginNuGet())
    .Does<BuildData>(static async (context, data) =>
    {
        ArgumentException.ThrowIfNullOrEmpty(data.NuGetApiUser);

        context.Information("Logging in to NuGet...");
        data.NuGetApiKey = await GitHubActions.Commands.NuGetLogin(data.NuGetApiUser);
        ArgumentException.ThrowIfNullOrEmpty(data.NuGetApiKey);
    });

Task("Clean")
    .IsDependentOn("NuGet-Login")
    .Does<BuildData>(
        static (context, data) => context.CleanDirectories(data.DirectoryPathsToClean)
    );

Task("Restore")
    .IsDependentOn("Clean")
    .Does<BuildData>(
        static (context, data) => context.DotNetRestore(
            data.ProjectRoot.FullPath,
            new DotNetRestoreSettings {
                MSBuildSettings = data.MSBuildSettings
            }
        )
    );

Task("DPI")
    .IsDependentOn("Restore")
    .Does<BuildData>(
        static (context, data) => Command(
            ["dpi", "dpi.exe"],
            new ProcessArgumentBuilder()
                .Append("nuget")
                .Append("--silent")
                .AppendSwitchQuoted("--output", "table")
                .Append(
                    (
(
                            !string.IsNullOrWhiteSpace(context.EnvironmentVariable("NuGetReportSettings_Endpoint"))
                            &&
                            !string.IsNullOrWhiteSpace(context.EnvironmentVariable("NuGetReportSettings_RuleId"))
                        )
                        ||
                        (
                            !string.IsNullOrWhiteSpace(context.EnvironmentVariable("NuGetReportSettings_SharedKey"))
                            &&
                            !string.IsNullOrWhiteSpace(context.EnvironmentVariable("NuGetReportSettings_WorkspaceId"))
                        )
                    )
                        ? "report"
                        : "analyze"
                    )
                .AppendSwitchQuoted("--buildversion", data.Version)
        )
    );

Task("Build")
    .IsDependentOn("DPI")
    .Does<BuildData>(
        static (context, data) => context.DotNetBuild(
            data.ProjectRoot.FullPath,
            new DotNetBuildSettings {
                NoRestore = true,
                MSBuildSettings = data.MSBuildSettings
            }
        )
    );

Task("Test")
    .IsDependentOn("Build")
    .Does<BuildData>(
        static (context, data) =>
        {
            foreach (var item in data.TestTargetFrameworks)
            {
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
                                                    .AppendSwitchQuoted("--input", data.TestProjectPath.Combine("input").FullPath)
                    }
                );
            }
        }
    );

Task("Default")
    .IsDependentOn("Test");

Task("Pack")
    .IsDependentOn("Test")
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
    );

Task("Upload-Artifacts")
    .IsDependentOn("Pack")
    .WithCriteria<BuildData>(static (_, data) => data.ShouldPushGitHubPackages())
    .Does<BuildData>(
        static (context, data) => GitHubActions
            .Commands
            .UploadArtifact(data.ArtifactsPath, "artifacts")
    );

Task("Push-GitHub-Packages")
    .IsDependentOn("Upload-Artifacts")
    .WithCriteria<BuildData>(static (_, data) => data.ShouldPushGitHubPackages())
    .Does<BuildData>(
        static (context, data) =>
        {
            foreach (var item in context.GetFiles(data.NuGetOutputPath.FullPath + "/*.nupkg"))
            {
                context.DotNetNuGetPush(
                    item.FullPath,
                    new DotNetNuGetPushSettings
                    {
                        Source = data.GitHubNuGetSource,
                        ApiKey = data.GitHubNuGetApiKey
                    }
                );
            }
        }
    );

Task("Push-NuGet-Packages")
    .IsDependentOn("Push-GitHub-Packages")
    .WithCriteria<BuildData>(static (_, data) => data.ShouldPushNuGetPackages())
    .Does<BuildData>(
        static (context, data) =>
        {
            foreach (var item in context.GetFiles(data.NuGetOutputPath.FullPath + "/*.nupkg"))
            {
                context.DotNetNuGetPush(
                    item.FullPath,
                    new DotNetNuGetPushSettings
                    {
                        Source = data.NuGetSource,
                        ApiKey = data.NuGetApiKey
                    }
                );
            }
        }
    );

Task("Create-GitHub-Release")
    .IsDependentOn("Push-NuGet-Packages")
    .WithCriteria<BuildData>(static (_, data) => data.ShouldPushNuGetPackages())
    .Does<BuildData>(
        static (context, data) => context
            .Command(
                new CommandSettings {
                    ToolName = "GitHub CLI",
                    ToolExecutableNames = ["gh.exe", "gh"],
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
    );

Task("GitHub-Actions")
    .IsDependentOn("Create-GitHub-Release");

RunTarget(Argument("target", "Default"));
