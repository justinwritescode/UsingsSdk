/*
 * BuildUsingsPackage.Properties.cs
 *
 *   Created: 2022-12-01-02:58:50
 *   Modified: 2022-12-01-02:58:50
 *
 *   Author: Justin Chase <justin@justinwritescode.com>
 *
 *   Copyright © 2022-2023 Justin Chase, All Rights Reserved
 *      License: MIT (https://opensource.org/licenses/MIT)
 */

#pragma warning disable
namespace MSBuild.UsingsSdk;
using System.Xml.Serialization;
using static System.IO.Path;
using XElementOrProjectItemInstance = AnyOf<
    System.Xml.Linq.XElement,
    Microsoft.Build.Execution.ProjectItemInstance
>;

public partial class BuildUsingsPackage
{
    [Required]
    public string InputFile { get; set; }
    private string? _version;
    public string? Version
    {
        get => _version ??= "1.0.0";
        set => _version = value;
    }

    [Required]
    public string OutputFile { get; set; } = string.Empty;
    private string? _packageId;
    public string? PackageId
    {
        get =>
            _packageId ??=
                AllProperties.GetPropertyValue("PackageId")
                ?? AllProperties.GetPropertyValue("PackageIdOverride")
                ?? AllProperties.GetPropertyValue("AssemblyName")
                ?? GetFileNameWithoutExtension(InputFile) + ".Usings";
        set => _packageId = value;
    }
    private string PackageReadmeFile => Combine(OutputDirectory, "README.md");

    [Output]
    public string UsingsProjectFile => Combine(OutputDirectory, $"{PackageId}.csproj");
    private string? OutputDirectory => GetDirectoryName(OutputFile);
    private string? PackageLibDirectory =>
        GetDirectoryName(typeof(BuildUsingsPackage).Assembly.Location);
    private string? PackageContentFilesDirectory => Combine(PackageLibDirectory, "../contentFiles");
    private IEnumerable<ProjectTuple?>? _allProjects;
    protected IEnumerable<ProjectTuple?> AllProjects => _allProjects ??= Load(InputFile)!;
    private IEnumerable<ProjectPropertyInstance>? _allProperties;
    protected IEnumerable<ProjectPropertyInstance> AllProperties =>
        _allProperties ??= AllProjects.SelectMany(
            p => p?.ProjectInstance?.Properties ?? Enumerable.Empty<ProjectPropertyInstance>()
        );
    protected string? IconFile =>
        AllProjects
            .SelectMany(
                project =>
                    project?.XDocument
                        .Descendants()
                        .Where(
                            x =>
                                x.GetIncludeValue()
                                    ?.EndsWith(
                                        "icon.png",
                                        StringComparison.CurrentCultureIgnoreCase
                                    ) ?? false
                        )!
            )
            .FirstOrDefault()
            ?.GetAttributeValue("Include")
        ?? AllProjects
            .SelectMany(
                project =>
                    project?.XDocument
                        .Descendants()
                        .Where(
                            x =>
                                x.GetIncludeValue()
                                    ?.EndsWith(
                                        "icon.jpg",
                                        StringComparison.CurrentCultureIgnoreCase
                                    ) ?? false
                        )!
            )
            .FirstOrDefault()
            ?.GetAttributeValue("Include")
        ?? "Icon.png";
    protected string Description =>
        $"This project contains a set of `using` statements and package and project imports for the `{GetFileNameWithoutExtension(InputFile)}` namespace for reuse in other projects";
    protected string Authors => AllProperties.GetPropertyValue("Authors", "No Author Specified");
    protected string Copyright =>
        AllProperties.GetPropertyValue(nameof(Copyright), "No Copyright Specified");
    protected string PackageOutputPath =>
        AllProperties.GetPropertyValue("PackageOutputPath")
        ?? AllProperties.GetPropertyValue("OutputPath")
        ?? AllProperties.GetPropertyValue("OutDir")
        ?? AllProperties.GetPropertyValue("BaseOutputPath")
        ?? Combine(GetDirectoryName(InputFile), "artifacts");

    private XElement[] DefaultPackageReferences = Array.Empty<XElement>(); /*new[]
    {
        new XElement("PackageReference", new XAttribute("Include", "Microsoft.SourceLink.GitHub"), new XAttribute("Version", "[1.1.1,)"), new XAttribute("PrivateAssets", "All")),
        new XElement("PackageReference", new XAttribute("Include", "Microsoft.Build.Tasks.Git"), new XAttribute("Version", "[1.1.1,)"), new XAttribute("PrivateAssets", "All")),
        new XElement("PackageReference", new XAttribute("Include", "GitInfo"), new XAttribute("Version", "[2.2.0,)"), new XAttribute("PrivateAssets", "All"))
    };*/
    private const string EmptyProjectFile = "<Project />";
    private const string? DefaultTargetFramework = null;
    private const string DefaultTargetFrameworks =
        "netstandard1.0;netstandard1.1;netstandard1.3;netstandard2.0;netstandard2.1;netcoreapp3.1;net6.0;net7.0";

    protected string TargetFramework =>
        AllProperties.GetPropertyValue(nameof(TargetFramework), DefaultTargetFramework);
    protected string TargetFrameworks =>
        AllProperties.GetPropertyValue(nameof(TargetFrameworks), DefaultTargetFrameworks);
    public static readonly ComparersImplementation Comparers = new();
    private string NuGetPackagesExistCachePath =>
        Combine(OutputDirectory, "../nugetPackagesExist.cache.json");
    private static Dictionary<string, bool>? _nuGetPackagesExistCache;
    private IDictionary<string, bool> NuGetPackagesExistCache =>
        _nuGetPackagesExistCache ??= File.Exists(NuGetPackagesExistCachePath)
            ? JsonSerializer.Deserialize<Dictionary<string, bool>>(
                File.ReadAllText(NuGetPackagesExistCachePath)
            )!
            : new Dictionary<string, bool>()!;

    private XElement[] XUsings => AllProjects.GetXItems("Using").ToArray();
    private ProjectItemInstance[] Usings => AllProjects.GetItems("Using").ToArray();
    private XElement[] XProjectReferences =>
        AllProjects.GetXItems(ProjectReference).Distinct(Comparers).ToArray();
    private XElement[] XFrameworkReferences =>
        AllProjects.GetXItems(FrameworkReference).Distinct(Comparers).ToArray();
    private XElement[] XPackageReferences =>
        AllProjects.GetXItems(PackageReference).Distinct(Comparers).ToArray();
    private ProjectItemInstance[] ProjectReferences =>
        AllProjects.GetItems(ProjectReference).Distinct(Comparers).ToArray();
    private ProjectItemInstance[] PackageReferences =>
        AllProjects.GetItems(PackageReference).Distinct(Comparers).ToArray();
    private ProjectItemTuple[] ProjectReferenceTuples => XProjectReferences.Join(ProjectReferences);
    private ProjectItemTuple[] PackageReferenceTuples => XPackageReferences.Join(PackageReferences);

    private string PackageIdOverride =>
        AllProperties.GetPropertyValue(nameof(PackageIdOverride), PackageId);
    private string PackageVersionOverride =>
        AllProperties.GetPropertyValue(nameof(PackageVersionOverride), Version);
    private string PackageVersion =>
        AllProperties.GetPropertyValue(nameof(PackageVersion), Version);

    private string MinVerVersionOverride =>
        AllProperties.GetPropertyValue(nameof(MinVerVersionOverride), Version);
    private string FileVersion => AllProperties.GetPropertyValue(nameof(FileVersion), Version);
    private string AssemblyVersion =>
        AllProperties.GetPropertyValue(nameof(AssemblyVersion), Regex.Replace(Version, "-.*", ""));
    private string PackageLicenseExpression =>
        AllProperties.GetPropertyValue(nameof(PackageLicenseExpression), "MIT");
    private string PackageIcon => GetFileName(IconFile);

    private const string CopyLocalLockFileAssemblies = False;
    private const string True = "true";
    private const string False = "false";
    private const string GeneratePackageOnBuild = True;
    private const string IsPackable = True;
    private const string IsNuGetized = True;
    private const string PublishRepositoryUrl = True;
    private const string NoBuild = True;
    private const string IncludeSource = False;
    private const string IncludeBuiltProjectOutputGroup = False;
    private const string IncludeSourceFilesProjectOutputGroup = False;
    private const string IncludeContentFilesProjectOutputGroup = False;
    private const string IncludeBuildOutput = False;
    private const string IncludeSymbols = False;
    private string PackageTags =>
        $"$({nameof(PackageTags)}) using usings namespace nuget package " + PackageId;
    private string Title => PackageId;
    private string Summary => Description;
    private const string ProjectReference = nameof(ProjectReference);
    private const string PackageReference = nameof(PackageReference);
    private const string FrameworkReference = nameof(FrameworkReference);
    private const string EmitNuspec = nameof(EmitNuspec);
}
