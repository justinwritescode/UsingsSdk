/*
 * XEExtensions.cs
 *
 *   Created: 2022-11-27-05:39:27
 *   Modified: 2022-12-01-03:23:39
 *
 *   Author: Justin Chase <justin@justinwritescode.com>
 *
 *   Copyright © 2022-2023 Justin Chase, All Rights Reserved
 *      License: MIT (https://opensource.org/licenses/MIT)
 */

namespace MSBuild.UsingsSdk;

using System.Linq;
using System.Xml.Linq;
#pragma warning disable
using MSBC = Microsoft.Build.Construction;
using MSBEx = Microsoft.Build.Execution;
using XE = System.Xml.Linq.XElement;
using PII = ProjectItemInstance;
using PIE = MSBC.ProjectItemElement;
using XEOrPII = AnyOf<System.Xml.Linq.XElement, ProjectItemInstance>;

public static class XEExtensions
{
    public static XAttribute? GetAttribute(this XE element, string name) =>
        element.Attributes().FirstOrDefault(x => x.Name.LocalName == name);

    public static string? GetAttributeValue(this PIE element, string name) =>
        GetAttributeValue((AnyOf<PIE, XE>)element, name);

    public static string? GetAttributeValue(this XE element, string name) =>
        GetAttributeValue((AnyOf<PIE, XE>)element, name);

    public static string? GetAttributeValue(this AnyOf<PIE, XE> element, string name) =>
        element.IsFirst
            ? element.First.GetMetadataValue(name)
            : element.Second.GetAttribute(name)?.Value;

    public static string? GetAttributeValue(this XEOrPII element, string name) =>
        element.IsFirst
            ? element.First.GetAttribute(name)?.Value
            : element.Second.GetMetadataValue(name);

    public static XE[] GetItems(this XE element, string name) =>
        element.Descendants(name).ToArray();

    public static XE[] GetXItems(this IEnumerable<ProjectTuple?> projects, string name) =>
        projects
            .SelectMany(x => x?.XDocument.Descendants(name)!)
            .Distinct(BuildUsingsPackage.Comparers)
            .OrderBy(x => x?.GetAttributeValue("Include"))
            .ToArray();

    public static PII[]? GetItems(this IEnumerable<ProjectTuple?> projects, string name) =>
        projects
            .SelectMany(x => x?.ProjectInstance.GetItems(name)!)
            .Distinct(BuildUsingsPackage.Comparers)
            .OrderBy(x => x.GetMetadataValue("Include"))
            .ToArray();

    public static string? GetMetadataValue(this PIE @element, string name) =>
        @element.Metadata
            .FirstOrDefault(
                x => x.Name?.Equals(name, StringComparison.InvariantCultureIgnoreCase) ?? false
            )
            ?.Value;

    public static string GetPropertyValue(
        this IEnumerable<ProjectPropertyInstance> properties,
        string name,
        string? defaultValue = null
    ) =>
        properties
            .FirstOrDefault(
                x => x.Name?.Equals(name, StringComparison.InvariantCultureIgnoreCase) ?? false
            )
            ?.EvaluatedValue
        ?? defaultValue
        ?? string.Empty;

    public static string? GetIncludeValue(this PIE @element) =>
        @element.GetMetadataValue("Include");

    public static string? GetIncludeValue(this PII @element) =>
        @element.GetMetadataValue("Include");

    public static string? GetIncludeValue(this XE @element) =>
        @element.GetAttributeValue("Include");

    public static BuildUsingsPackage.ComparersImplementation ComparersImplementation =
        new BuildUsingsPackage.ComparersImplementation();

    public static ProjectItemTuple[] Join(
        this IEnumerable<XE> XEs,
        IEnumerable<PII> projectItems
    ) =>
        (
            from XE in XEs
            from projectItem in projectItems
            select ComparersImplementation.Equals(XE, projectItem)
                ? new ProjectItemTuple(XE, projectItem)
                : new ProjectItemTuple(XE, null)
        ).ToArray();

    //XEs.Join(
    //    projectItems,
    //    x => (XEOrPII)x,
    //    x => (XEOrPII)x,
    //    (xPackageReference, packageReference) => new ProjectItemTuple(xPackageReference, packageReference),
    //    new BuildUsingsPackage.ComparersImplementation())
    //    .ToArray();
}
