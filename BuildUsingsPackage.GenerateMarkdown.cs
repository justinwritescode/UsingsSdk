﻿/*
 * BuildUsingsPackage.GenerateMarkdown.cs
 *
 *   Created: 2022-12-01-03:00:37
 *   Modified: 2022-12-01-03:00:38
 *
 *   Author: Justin Chase <justin@justinwritescode.com>
 *
 *   Copyright © 2022-2023 Justin Chase, All Rights Reserved
 *      License: MIT (https://opensource.org/licenses/MIT)
 */

namespace MSBuild.UsingsSdk;

public partial class BuildUsingsPackage
{
    public string GenerateMarkdownReadme() =>
        new StringBuilder()
            .AppendFormat(
                "---{0}title: {1}{0}version: {2}{0}authors: {3}{0}copyright: {4}{0}description: {5}{0}date: {6}{0}---{0}{0}",
                NewLine,
                PackageId,
                Version,
                Authors,
                Copyright,
                Description,
                DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss")
            )
            .AppendLine()
            .AppendLine($"## {PackageId}")
            .AppendLine()
            .AppendLine(Description)
            .AppendLine()
            .AppendLine("### Usings")
            .AppendLine()
            .AppendLine(
                Join(
                    NewLine,
                    XUsings.Select(
                        x => $"- {x.GetIncludeValue()}{FormatIsStatic(x)}{FormatAlias(x)}"
                    )
                )
            )
            .AppendLine()
            .AppendLine("### Package References")
            .AppendLine()
            .AppendLine(Join(NewLine, XPackageReferences.Select(FormatPackageReferenceMarkdown)))
            .AppendLine()
            .AppendLine("### Project References")
            .AppendLine()
            .AppendLine(Join(NewLine, XProjectReferences.Select(x => $"- {x.GetIncludeValue()}")))
            .ToString();
}
