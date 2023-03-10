/*
 * BuildUsingsPackage.Execute.cs
 *
 *   Created: 2022-12-01-02:52:09
 *   Modified: 2022-12-01-02:52:09
 *
 *   Author: Justin Chase <justin@justinwritescode.com>
 *
 *   Copyright © 2022-2023 Justin Chase, All Rights Reserved
 *      License: MIT (https://opensource.org/licenses/MIT)
 */
#pragma warning disable

namespace MSBuild.UsingsSdk;

using Microsoft.Build.Framework;
using NuGet.Protocol.Plugins;
using XA = System.Xml.Linq.XAttribute;
using XC = System.Xml.Linq.XComment;
using XD = System.Xml.Linq.XDocument;
using XE = System.Xml.Linq.XElement;
using XEOrProjectItemInstance = AnyOf<XElement, Microsoft.Build.Execution.ProjectItemInstance>;

public partial class BuildUsingsPackage
{
    public override bool Execute()
    {
        Log.LogMessage(
            MessageImportance.High,
            $"Executing {ThisAssembly.Project.AssemblyName} version {ThisAssembly.Info.FileVersion} on file {InputFile}..."
        );

        System.Diagnostics.Debugger.Launch();

        Log.LogMessage($"UsingsSdk Version {ThisAssembly.Info.InformationalVersion} executing...");

        ComparersImplementation.Logger = this.Log;

        Log.LogMessage("Found " + AllProjects.Count() + " imported projects to process");

        var xUsings = XUsings;
        var usings = Usings;
        var xProjectReferences = XProjectReferences;
        var xPackageReferences = XPackageReferences;
        var projectReferences = ProjectReferences;
        var packageReferences = PackageReferences;
        var properties = MakeProperties(); //.OrderBy(x => x.Name)).ToArray();
        var projectReferenceTuples = xProjectReferences.Join(projectReferences);
        var packageReferenceTuples = xPackageReferences.Join(packageReferences);
        var usingsTuples = xUsings.Join(usings);

        var usingsFile = new XD(
            new XC("<auto-generated>"),
            new XC(
                """"
                This code was generated by a tool.  Do not modify it.
                Any changes you make will be lost the next time the file is generated.
                If you need to change its contents, change the source file and regenerate it.
                """"
            ),
            new XC("</auto-generated>"),
            new XE(
                "Project",
                // new XE("Import", new XA("Project", "$(MSBuildBinPath)/Sdks/NuGet.Build.Tasks.Pack/buildCrossTargeting/NuGet.Build.Tasks.Pack.targets")),
                new XC("Usings: " + xUsings.Length),
                new XE(
                    "ItemGroup",
                    new XA("Label", "Usings"),
                    new XC("⬇️ Global Usings ⬇️"),
                    xUsings
                        .Select(x => FormatReference(x, "Using"))
                        .Append<XNode>(new XC("⬆️  🫴🏻 💪🏻  ⬆️"))
                ),
                new XE(
                    "ItemGroup",
                    new XA("Label", "Package References"),
                    new XC("📦 ⬇️ Package References ⬇️  📦"),
                    XPackageReferences
                        .Select(x => FormatReference(x, "PackageReference", false))
                        .Append<XNode>(new XC("📦  ⬆️  ⬆️  📦"))
                ),
                new XE(
                    "ItemGroup",
                    new XA("Label", "Project References"),
                    new XC("⬇️ Project References ⬇️"),
                    projectReferenceTuples
                        .Select(x => FormatReference(x, "ProjectReference"))
                        .Append<XNode>(new XC("⬆️    💻    ⬆️"))
                )
            )
        );

        var usingsProjectFile = new XD(
            new XC(
                """"

                    <auto-generated>
                        This code was generated by a tool.  Do not modify it.
                        Any changes you make will be lost the next time the file is generated.
                        If you need to change its contents, change the source file and regenerate it.
                    </auto-generated>

                """"
            ),
            new XE(
                "Project",
                new XA("Sdk", "Microsoft.NET.Sdk"),
                // new XA("DefaultTargets", "Pack"),
                // new XE("Import", new XA("Project", "$(MSBuildBinPath)/Sdks/NuGet.Build.Tasks.Pack/buildCrossTargeting/NuGet.Build.Tasks.Pack.targets")),
                new XC("properties: " + properties.Length),
                new XC("⬇️ Properties ⬇️"),
                new XE("PropertyGroup", properties),
                new XE(
                    "ItemGroup",
                    XPackageReferences
                        .Select(x => FormatReference(x, "PackageReference", true))
                        .Concat(DefaultPackageReferences)
                        .Distinct()
                ),
                new XE(
                    "ItemGroup",
                    new XE(
                        "PackageFile",
                        new XA("Include", "$(MSBuildThilsFileDirectory)README.md"),
                        new XA("Pack", "true"),
                        new XA("PackagePath", "")
                    ),
                    new XE(
                        "PackageFile",
                        new XA("Include", Combine(GetDirectoryName(InputFile), OutputFile)),
                        new XA("Pack", "true"),
                        new XA("PackagePath", "build/%(Filename)%(Extension)")
                    ),
                    new XE(
                        "PackageFile",
                        new XA("Include", IconFile),
                        new XA("Pack", "true"),
                        new XA("PackagePath", GetFileName(IconFile))
                    ),
                    new XE("None", new XA("Remove", "**/$(AssemblyName).*")),
                    new XE("None", new XA("Remove", "**/*.cs"))
                ),
                new XE(
                    "Target",
                    new XA("Name", "GetPackageVersion"),
                    new XE(
                        "PropertyGroup",
                        new XA("Condition", "'$(PackageVersion)' == ''"),
                        new XE(
                            nameof(PackageVersion),
                            new XA($"Condition", $"'$({nameof(PackageVersion)})' == ''"),
                            PackageVersion
                        )
                    )
                ),
                new XE(
                    "Target",
                    new XA("Name", "RemovePackageContents"),
                    new XA("AfterTargets", "GetPackageContents"),
                    new XE(
                        "ItemGroup",
                        new XE(
                            "PackageFile",
                            new XA("Remove", "**/*")
                        ),
                        new XE(
                            "None",
                            new XA("Remove", "**/*")
                        )
                    )
                )
            )
        );

        Log.LogMessage("Properties: " + properties.Length);
        Log.LogMessage("Usings: " + xUsings.Length);
        Log.LogMessage("ProjectReferences: " + xProjectReferences.Length);
        Log.LogMessage("PackageReference: " + xPackageReferences.Length);

        if (Directory.Exists(OutputDirectory))
        {
            Delete(OutputDirectory, true);
        }

        _ = CreateDirectory(OutputDirectory);

        using (var outFile = CreateText(OutputFile))
        {
            usingsFile.Save(outFile);
        }

        using (var outFile = CreateText(UsingsProjectFile))
        {
            usingsProjectFile.Save(outFile);
        }

        using (var outFile = CreateText(PackageReadmeFile))
        {
            outFile.WriteLine(GenerateMarkdownReadme());
        }

        // using (var outFile = File.CreateText(Path.Combine(OutputDirectory, "Directory.Build.props")))
        // {
        //     outFile.WriteLine(EmptyProjectFile);
        // }

        // using (var outFile = File.CreateText(Path.Combine(OutputDirectory, "Directory.Build.targets")))
        // {
        //     outFile.WriteLine(EmptyProjectFile);
        // }

        // Copy(Combine(PackageLibDirectory, "../ContentFiles/global.json"), Combine(OutputDirectory, "global.json"), true);
        Log.LogMessage(usingsFile.ToString());
        Log.LogMessage("Wrote file: " + OutputFile);

        Copy(IconFile, Combine(OutputDirectory, "Icon.png"), true);

        WriteAllText(
            NuGetPackagesExistCachePath,
            JsonSerializer.Serialize(NuGetPackagesExistCache)
        );
        Log.LogMessage(MessageImportance.High, "Done!");
        return true;
    }
}
