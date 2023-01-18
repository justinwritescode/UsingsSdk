/*
 * CreateUsingsProject.cs
 *
 *   Created: 2022-11-22-03:14:00
 *   Modified: 2022-11-26-02:33:22
 *
 *   Author: Justin Chase <justin@justinwritescode.com>
 *
 *   Copyright © 2022-2023 Justin Chase, All Rights Reserved
 *      License: MIT (https://opensource.org/licenses/MIT)
 */

namespace MSBuild.UsingsSdk;
using static System.String;

public partial class BuildUsingsPackage : MSBTask
{
    private const string Condition = nameof(Condition);

    public virtual IEnumerable<ProjectTuple?>? Load(string? path)
    {
        if (path is null)
        {
            return new[] { null as ProjectTuple? };
        }

        var project = new ProjectInstance(path);
        var xDocumentProject = XDocument.Load(path);
        return new[] { new ProjectTuple(project, xDocumentProject) as ProjectTuple? }
            .Concat(
                xDocumentProject
                    .Descendants("Import")
                    .SelectMany(x => Load(x.GetIncludeValue()).Where(p => p is not null))
            )
            .Where(x => x is not null);
    }

    private XElement[] MakeProperties()
    {
        var allProperties = AllProperties;
        var copiedProperties = new List<XElement>
        {
            new XElement(nameof(Description), Description),
            new XElement(
                nameof(PackageId),
                allProperties.GetPropertyValue(nameof(PackageId), PackageId)
            ),
            // TargetFramework is not null ? new XElement("TargetFramework", TargetFramework) : null,
            new XElement(nameof(TargetFrameworks), new XAttribute(Condition, $"'$({nameof(TargetFrameworks)})' == ''"), TargetFrameworks),
            new XElement(nameof(PackageIdOverride), new XAttribute(Condition, $"'$({nameof(PackageIdOverride)})' == ''"), PackageIdOverride),
            new XElement(nameof(Version), new XAttribute(Condition, $"'$({nameof(Version)})' == ''"), Version),
            new XElement(nameof(PackageVersion), new XAttribute(Condition, $"'$({nameof(PackageVersion)})' == ''"), PackageVersion),
            new XElement(nameof(MinVerVersionOverride), new XAttribute(Condition, $"'$({nameof(MinVerVersionOverride)})' == ''"), MinVerVersionOverride),
            new XElement(nameof(FileVersion), new XAttribute(Condition, $"'$({nameof(FileVersion)})' == ''"), FileVersion),
            new XElement(nameof(AssemblyVersion), new XAttribute(Condition, $"'$({nameof(AssemblyVersion)})' == ''"), AssemblyVersion),
            new XElement(nameof(PackageLicenseExpression), new XAttribute(Condition, $"'$({nameof(PackageLicenseExpression)})' == ''"), PackageLicenseExpression),
            new XElement(nameof(PackageOutputPath), new XAttribute(Condition, $"'$({nameof(PackageOutputPath)})' == ''"), PackageOutputPath),
            new XElement(nameof(PackageIcon), new XAttribute(Condition, $"'$({nameof(PackageIcon)})' == ''"), PackageIcon),
            new XElement(nameof(GeneratePackageOnBuild), new XAttribute(Condition, $"'$({nameof(GeneratePackageOnBuild)})' == ''"), GeneratePackageOnBuild),
            new XElement(nameof(IsPackable), new XAttribute(Condition, $"'$({nameof(IsPackable)})' == ''"), IsPackable),
            new XElement(nameof(IsNuGetized), new XAttribute(Condition, $"'$({nameof(IsNuGetized)})' == ''"), IsNuGetized),
            new XElement(nameof(Title), new XAttribute(Condition, $"'$({nameof(Title)})' == ''"), Title),
            new XElement(nameof(Summary), new XAttribute(Condition, $"'$({nameof(Summary)})' == ''"), Summary),
            new XElement(nameof(Authors), new XAttribute(Condition, $"'$({nameof(Authors)})' == ''"), Authors),
            // new XElement(nameof(Copyright), new XAttribute(Condition, $"'$({nameof(Copyright)})' == ''"), Copyright),
            new XElement(nameof(PublishRepositoryUrl), new XAttribute(Condition, $"'$({nameof(PublishRepositoryUrl)})' == ''"), PublishRepositoryUrl),
            new XElement(nameof(PackageTags), PackageTags),
            new XElement(nameof(IncludeBuiltProjectOutputGroup), IncludeBuiltProjectOutputGroup),
            new XElement(
                nameof(IncludeSourceFilesProjectOutputGroup),
                new XAttribute(Condition, $"'$({nameof(IncludeSourceFilesProjectOutputGroup)})' == ''"),
                IncludeSourceFilesProjectOutputGroup
            ),
            new XElement(
                nameof(IncludeContentFilesProjectOutputGroup),
                new XAttribute(Condition, $"'$({nameof(IncludeContentFilesProjectOutputGroup)})' == ''"),
                IncludeContentFilesProjectOutputGroup
            ),
            new XElement(nameof(NoBuild), new XAttribute(Condition, $"'$({nameof(NoBuild)})' == ''"), NoBuild),
            new XElement(nameof(IncludeSource), new XAttribute(Condition, $"'$({nameof(IncludeSource)})' == ''"), IncludeSource),
            new XElement(nameof(IncludeSymbols), new XAttribute(Condition, $"'$({nameof(IncludeSymbols)})' == ''"), IncludeSymbols),
            new XElement(nameof(IncludeBuildOutput), new XAttribute(Condition, $"'$({nameof(IncludeBuildOutput)})' == ''"), IncludeBuildOutput)
        };
        return copiedProperties.OrderBy(p => p.Name.ToString()).ToArray();
    }

    private static XAttribute[]? GetReferenceAttributes(
        AnyOf<ProjectItemInstance, XElement> @ref,
        bool includeVersion = true
    ) =>
        @ref.IsFirst && @ref.First?.EvaluatedInclude != null
            ? new[] { new XAttribute("Include", @ref.First?.EvaluatedInclude) }
                .Concat(
                    @ref.First?.Metadata
                        .Where(x => x != null && x.EvaluatedValue != null)
                        .Select(x => new XAttribute(x.Name, x.EvaluatedValue))
                        .Where(x => includeVersion || x.Name.LocalName != "Version")
                )
                .Distinct(Comparers)
                .ToArray()
            : @ref.IsSecond && @ref.Second?.GetAttributeValue("Include") != null
                ? new[] { new XAttribute("Include", @ref.Second?.GetAttributeValue("Include")) }
                    .Concat(
                        @ref.Second
                            ?.Attributes()
                            .Where(x => x != null && x.Value != null)
                            .Select(x => new XAttribute(x.Name, x.Value))
                            .Where(x => includeVersion || x.Name.LocalName != "Version")
                    )
                    .Distinct(Comparers)
                    .ToArray()
                : null;

    private static XElement FormatReference(
        ProjectItemTuple @ref,
        string type = "ProjectReference",
        bool includeVersion = true
    ) =>
        new(
            type,
            GetReferenceAttributes(@ref.XItem, includeVersion)
                .Concat(
                    GetReferenceAttributes(@ref.Item, includeVersion) ?? Array.Empty<XAttribute>()
                )
                .Distinct(Comparers)
                .ToArray()
        );

    private static XElement FormatReference(
        XElement @ref,
        string type = "ProjectReference",
        bool includeVersion = true
    ) => new(type, GetReferenceAttributes(@ref).Distinct(Comparers).ToArray());

    private static string FormatIsStatic(AnyOf<MSBC.ProjectItemElement, XElement> x)
    {
        var metadataValue = x.GetAttributeValue("Static");
        if (metadataValue?.Equals("true", StringComparison.InvariantCultureIgnoreCase) ?? false)
        {
            return " *(static)*";
        }
        return Empty;
    }

    private static string GetNuGetUri(XElement @element) =>
        $"https://www.nuget.org/packages/{@element.GetIncludeValue()}";

    private bool GetNuGetPackageExists(XElement @element) =>
        NuGetPackagesExistCache[@element.GetIncludeValue()] =
            NuGetPackagesExistCache.TryGetValue(@element.GetIncludeValue(), out var exists)
            && exists;

    private string? FormatPackageReferenceMarkdown(XElement @element) =>
        GetNuGetPackageExists(@element)
            ? $"- [{@element.GetIncludeValue()}]({GetNuGetUri(@element)})"
            : $"- {@element.GetIncludeValue()}";

    private static string FormatAlias(AnyOf<MSBC.ProjectItemElement, XElement> x)
    {
        var alias = x.IsFirst
            ? x.First.GetMetadataValue("Alias")
            : x.Second.GetAttributeValue("Alias");
        return IsNullOrWhiteSpace(alias) ? Empty : $" (Alias: *{alias}*)";
    }
}
