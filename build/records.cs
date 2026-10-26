/*****************************
 * Records
 *****************************/
public record BuildData(
    string Version,
    bool IsMainBranch,
    bool IsRunningOnGitHubActions,
    string? WorkflowRef,
    DirectoryPath ProjectRoot,
    string Configuration,
    DotNetMSBuildSettings MSBuildSettings,
    DirectoryPath ArtifactsPath,
    DirectoryPath OutputPath,
    string TestProjectName,
    string [] TestTargetFrameworks
    )
{
    public DirectoryPath NuGetOutputPath { get; } = OutputPath.Combine("nuget");
    public DirectoryPath BinaryOutputPath { get; } = OutputPath.Combine("bin");
    public DirectoryPath TestProjectPath { get; } = ProjectRoot.Combine(TestProjectName);
    public DirectoryPath IntegrationTestPath { get; } = OutputPath.Combine("integrationtest");
    public string? GitHubNuGetSource { get; } = System.Environment.GetEnvironmentVariable("GH_PACKAGES_NUGET_SOURCE");
    public string? GitHubNuGetApiKey { get; } = System.Environment.GetEnvironmentVariable("GITHUB_TOKEN");
    public string? NuGetSource { get; } = System.Environment.GetEnvironmentVariable("NUGET_SOURCE");
    public string? NuGetApiUser { get; } = System.Environment.GetEnvironmentVariable("NUGET_USER");
    public string? NuGetApiKey { get; set; } = System.Environment.GetEnvironmentVariable("NUGET_APIKEY");
    public ICollection<DirectoryPath> DirectoryPathsToClean { get; } =
    [
        ArtifactsPath,
        OutputPath
    ];

    public bool ShouldLoginNuGet() =>
        IsRunningOnGitHubActions
        && (IsMainBranch
            || (WorkflowRef?.StartsWith("refs/tags/v", StringComparison.Ordinal) ?? false));

    public bool ShouldPushGitHubPackages() => IsMainBranch &&
                                                !string.IsNullOrWhiteSpace(GitHubNuGetSource)
                                                && !string.IsNullOrWhiteSpace(GitHubNuGetApiKey);
    public bool ShouldPushNuGetPackages() =>    IsMainBranch &&
                                                !string.IsNullOrWhiteSpace(NuGetSource) &&
                                                !string.IsNullOrWhiteSpace(NuGetApiKey);

    public bool ShouldReportDPI() =>
        !string.IsNullOrWhiteSpace(System.Environment.GetEnvironmentVariable("NuGetReportSettings_Endpoint"))
        && !string.IsNullOrWhiteSpace(System.Environment.GetEnvironmentVariable("NuGetReportSettings_RuleId"));


}
