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

using System.Linq;
using System.Xml.Serialization;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using static System.IO.Path;
using static System.Text.RegularExpressions.Regex;
using XElementOrProjectItemInstance = AnyOf<
    System.Xml.Linq.XElement,
    Microsoft.Build.Execution.ProjectItemInstance
>;

public partial class BuildUsingsPackage
{
    [Required]
    public string InputFile { get; set; }
    [Required]
    public string OutputFile { get; set; } = string.Empty;
    [Output]
    public string UsingsProjectFile => Combine(OutputDirectory, $"{PackageId}.csproj");

    private IEnumerable<ProjectPropertyInstance>? _allProperties;
    private string? _packageId;
    private string? _version;

    private const string Condition = nameof(Condition);
    private const string CopyLocalLockFileAssemblies = False;
    private const string DefaultTargetFrameworks = "netstandard1.0;netstandard1.1;netstandard1.3;netstandard2.0;netstandard2.1;netcoreapp3.1;net6.0;net7.0";
    private const string EmitNuspec = nameof(EmitNuspec);
    private const string EmptyProjectFile = "<Project />";
    private const string False = "false";
    private const string FrameworkReference = nameof(FrameworkReference);
    private const string GeneratePackageOnBuild = True;
    private const string IncludeBuildOutput = False;
    private const string IncludeBuiltProjectOutputGroup = False;
    private const string IncludeContentFilesProjectOutputGroup = False;
    private const string IncludeCopyLocalFilesOutputGroup = False;
    private const string IncludeSource = False;
    private const string IncludeSourceFilesProjectOutputGroup = False;
    private const string IncludeSymbols = False;
    private const string IsNuGetized = True;
    private const string IsPackable = True;
    private const string NoBuild = True;
    private const string PackageReference = nameof(PackageReference);
    private const string ProjectReference = nameof(ProjectReference);
    private const string PublishRepositoryUrl = True;
    private const string True = "true";
    private const string? DefaultTargetFramework = null;
    private IEnumerable<ProjectTuple?>? _allProjects;
    private ProjectItemInstance[] PackageReferences => AllProjects.GetItems(PackageReference).Distinct(Comparers).ToArray();
    private ProjectItemInstance[] ProjectReferences => AllProjects.GetItems(ProjectReference).Distinct(Comparers).ToArray();
    private ProjectItemInstance[] Usings => AllProjects.GetItems("Using").ToArray();
    private ProjectItemTuple[] PackageReferenceTuples => XPackageReferences.Join(PackageReferences);
    private ProjectItemTuple[] ProjectReferenceTuples => XProjectReferences.Join(ProjectReferences);
    private static Dictionary<string, bool>? _nuGetPackagesExistCache; private IDictionary<string, bool> NuGetPackagesExistCache => _nuGetPackagesExistCache ??= File.Exists(NuGetPackagesExistCachePath) ? JsonSerializer.Deserialize<Dictionary<string, bool>>(File.ReadAllText(NuGetPackagesExistCachePath))! : new Dictionary<string, bool>()!;
    private string AssemblyVersion => AllProperties.GetPropertyValue(nameof(AssemblyVersion), Replace(Version, "-.*", ""));
    private string FileVersion => AllProperties.GetPropertyValue(nameof(FileVersion), Version);
    private string MinVerVersionOverride => AllProperties.GetPropertyValue(nameof(MinVerVersionOverride), Version);
    private string NuGetPackagesExistCachePath => Combine(OutputDirectory, "../nugetPackagesExist.cache.json");
    private string PackageIcon => GetFileName(IconFile);
    private string PackageIdOverride => AllProperties.GetPropertyValue(nameof(PackageIdOverride), PackageId);
    private string PackageLicenseExpression => AllProperties.GetPropertyValue(nameof(PackageLicenseExpression), "MIT");
    private string PackageReadmeFile => Combine(OutputDirectory, "README.md");
    private string PackageTags => $"$({nameof(PackageTags)}) using usings namespace nuget package " + PackageId;
    private string PackageVersion => AllProperties.GetPropertyValue(nameof(PackageVersion), Version);
    private string PackageVersionOverride => AllProperties.GetPropertyValue(nameof(PackageVersionOverride), Version);
    private string Summary => Description;
    private string Title => PackageId;
    private string? OutputDirectory => GetDirectoryName(OutputFile);
    private string? PackageContentFilesDirectory => Combine(PackageLibDirectory, "../contentFiles");
    private string? PackageLibDirectory => GetDirectoryName(typeof(BuildUsingsPackage).Assembly.Location);
    private XElement[] DefaultPackageReferences = Empty<XElement>(); /*new[] { new XElement("PackageReference", new XAttribute("Include", "Microsoft.SourceLink.GitHub"), new XAttribute("Version", "[1.1.1,)"), new XAttribute("PrivateAssets", "All")), new XElement("PackageReference", new XAttribute("Include", "Microsoft.Build.Tasks.Git"), new XAttribute("Version", "[1.1.1,)"), new XAttribute("PrivateAssets", "All")), new XElement("PackageReference", new XAttribute("Include", "GitInfo"), new XAttribute("Version", "[2.2.0,)"), new XAttribute("PrivateAssets", "All")) };*/
    private XElement[] XFrameworkReferences => AllProjects.GetXItems(FrameworkReference).Distinct(Comparers).ToArray();
    private XElement[] XPackageReferences => AllProjects.GetXItems(PackageReference).Distinct(Comparers).ToArray();
    private XElement[] XProjectReferences => AllProjects.GetXItems(ProjectReference).Distinct(Comparers).ToArray();
    private XElement[] XUsings => AllProjects.GetXItems("Using").ToArray();
    protected IEnumerable<ProjectPropertyInstance> AllProperties => _allProperties ??= AllProjects.SelectMany(p => p?.ProjectInstance?.Properties ?? Enumerable.Empty<ProjectPropertyInstance>());
    protected IEnumerable<ProjectTuple?> AllProjects => _allProjects ??= Load(InputFile)!;
    protected string Authors => AllProperties.GetPropertyValue("Authors", "No Author Specified");
    protected string Copyright => AllProperties.GetPropertyValue(nameof(Copyright), "No Copyright Specified");
    protected string Description => $"This project contains a set of `using` statements and package and project imports for the `{GetFileNameWithoutExtension(InputFile)}` namespace for reuse in other projects";
    protected string PackageOutputPath => AllProperties.GetPropertyValue("PackageOutputPath") ?? AllProperties.GetPropertyValue("OutputPath") ?? AllProperties.GetPropertyValue("OutDir") ?? AllProperties.GetPropertyValue("BaseOutputPath") ?? Combine(GetDirectoryName(InputFile), "artifacts");
    protected string TargetFramework => AllProperties.GetPropertyValue(nameof(TargetFramework), DefaultTargetFramework);
    protected string TargetFrameworks => AllProperties.GetPropertyValue(nameof(TargetFrameworks), DefaultTargetFrameworks);
    protected string? IconFile => AllProjects.SelectMany(project => project?.XDocument.Descendants().Where(x => x.GetIncludeValue()?.EndsWith("icon.png", StringComparison.CurrentCultureIgnoreCase) ?? false)!).FirstOrDefault()?.GetAttributeValue("Include") ?? AllProjects.SelectMany(project => project?.XDocument.Descendants().Where(x => x.GetIncludeValue()?.EndsWith("icon.jpg", StringComparison.CurrentCultureIgnoreCase) ?? false)!).FirstOrDefault()?.GetAttributeValue("Include") ?? "Icon.png";
    public static readonly ComparersImplementation Comparers = new();
    public string? PackageId { get => _packageId ??= AllProperties.GetPropertyValue("PackageId") ?? AllProperties.GetPropertyValue("PackageIdOverride") ?? AllProperties.GetPropertyValue("AssemblyName") ?? GetFileNameWithoutExtension(InputFile) + ".Usings"; set => _packageId = value; }
    public string? Version { get => _version ??= "1.0.0"; set => _version = value; }
}
