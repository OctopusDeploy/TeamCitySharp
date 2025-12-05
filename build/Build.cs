// ReSharper disable RedundantUsingDirective

using System;
using System.IO;
using System.Text.RegularExpressions;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.CI.TeamCity;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.Docker;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.OctoVersion;
using Nuke.Common.Utilities.Collections;
using Serilog;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

class Build : NukeBuild
{
    [Parameter("Whether to auto-detect the branch name - this is okay for a local build, but should not be used under CI.")] readonly bool AutoDetectBranch = IsLocalBuild;

    [Parameter("Branch name for OctoVersion to use to calculate the version number. Can be set via the environment variable OCTOVERSION_CurrentBranch.", Name = "OCTOVERSION_CurrentBranch")]
#pragma warning disable CS0414 // The field 'Build.BranchName' is assigned but its value is never used
    readonly string BranchName = null!; //this is set by nuke
#pragma warning restore CS0414
    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")] readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [OctoVersion(UpdateBuildNumber = true, BranchMember = nameof(BranchName), AutoDetectBranchMember = nameof(AutoDetectBranch), Framework = "net10.0")]
    readonly OctoVersionInfo OctoVersionInfo = null!; //this is set by nuke

    [Solution(GenerateProjects = true)]
    readonly Solution Solution = null!; //this is set by nuke

    AbsolutePath SourceDirectory => RootDirectory / "source";
    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";
    AbsolutePath PublishDirectory => RootDirectory / "publish";

    Target Clean => _ => _
        .Executes(() =>
        {
            SourceDirectory.GlobDirectories("**/bin", "**/obj", "**/TestResults").ForEach(x => x.DeleteDirectory());
            ArtifactsDirectory.CreateOrCleanDirectory();
            PublishDirectory.CreateOrCleanDirectory();
        });

    Target Restore => _ => _
        .DependsOn(Clean)
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(Solution));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .EnableNoRestore()
                .SetVersion(OctoVersionInfo.FullSemVer)
                .SetInformationalVersion(OctoVersionInfo.InformationalVersion));
        });

    Target Test => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetTest(_ => _
                //currently skipping the integration tests as they require a TeamCity server to be running 
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .SetLoggers("trx")
                .SetVerbosity(DotNetVerbosity.normal)
                .EnableNoBuild()
                .EnableNoRestore());
            SourceDirectory.GlobFiles("**/*.trx").ForEach(x => x.CopyToDirectory(ArtifactsDirectory));
        });

    Target Pack => _ => _
        .DependsOn(Compile)
        .DependsOn(Test)
        .Executes(() =>
        {
            DotNetPack(_ => _
                .SetProject(Solution)
                .SetConfiguration(Configuration)
                .SetOutputDirectory(ArtifactsDirectory)
                .EnableNoBuild()
                .AddProperty("Version", OctoVersionInfo.FullSemVer)
            );
        });

    /// Support plugins are available for:
    /// - JetBrains ReSharper        https://nuke.build/resharper
    /// - JetBrains Rider            https://nuke.build/rider
    /// - Microsoft VisualStudio     https://nuke.build/visualstudio
    /// - Microsoft VSCode           https://nuke.build/vscode
    public static int Main() => Execute<Build>(x => x.Pack);
}
