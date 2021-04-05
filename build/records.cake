/*****************************
 * Records
 *****************************/
public record BuildData(
    string Version,
    bool IsMainBranch,
    DirectoryPath ProjectRoot,
    DotNetCoreMSBuildSettings MSBuildSettings,
    DirectoryPath ArtifactsPath,
    DirectoryPath OutputPath
    )
{
    public DirectoryPath NuGetOutputPath { get; } = OutputPath.Combine("nuget");
    public DirectoryPath BinaryOutputPath { get; } = OutputPath.Combine("bin");
    public string GitHubNuGetSource { get; } = System.Environment.GetEnvironmentVariable("GH_PACKAGES_NUGET_SOURCE");
    public string GitHubNuGetApiKey { get; } = System.Environment.GetEnvironmentVariable("GH_PACKAGES_NUGET_APIKEY");
    public string NuGetSource { get; } = System.Environment.GetEnvironmentVariable("NUGET_SOURCE");
    public string NuGetApiKey { get; } = System.Environment.GetEnvironmentVariable("NUGET_APIKEY");
    public ICollection<DirectoryPath> DirectoryPathsToClean = new []{
        ArtifactsPath,
        OutputPath
    };
    public bool ShouldPushGitHubPackages() => !string.IsNullOrWhiteSpace(GitHubNuGetSource)
                                                && !string.IsNullOrWhiteSpace(GitHubNuGetApiKey);
    public bool ShouldPushNuGetPackages() =>    IsMainBranch &&
                                                !string.IsNullOrWhiteSpace(NuGetSource) &&
                                                !string.IsNullOrWhiteSpace(NuGetApiKey);

}

private record ExtensionHelper(Func<string, CakeTaskBuilder> TaskCreate, Func<CakeReport> Run);
