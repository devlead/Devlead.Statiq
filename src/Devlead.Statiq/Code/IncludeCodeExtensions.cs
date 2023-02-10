namespace Devlead.Statiq.Code;

public static class IncludeCodeExtensions
{
    public static Bootstrapper AddIncludeCodeShortCode(
        this Bootstrapper bootstrapper,
        string includeCodeShortCode = "IncludeCode"
    ) => bootstrapper.AddShortcode<IncludeCodeShortcode>(includeCodeShortCode);

}