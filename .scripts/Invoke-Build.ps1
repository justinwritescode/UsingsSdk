<#
    .SYNOPSIS
        Builds a project using the dotnet cli

    .DESCRIPTION
        Builds a project using the dotnet cli
            
    .EXAMPLE
        # Build the project and push the package to all four feeds
        Invoke-Build -Project "C:\Projects\MyProject\MyProject.csproj" -PushLocal -PushGitHub -PushAzure -PushNuGet
    .EXAMPLE
        # Build the project and push the package to the Local feed
        Invoke-Build -Project "C:\Projects\MyProject\MyProject.csproj" -PushLocal
    .EXAMPLE
        # Don't build the project; output the project's targets to the console
        Invoke-Build -Project "C:\Projects\MyProject\MyProject.csproj" -Targets 
    .EXAMPLE
        # Don't build the project; output the project's targets to a file instead
        Invoke-Build -Project "C:\Projects\MyProject\MyProject.csproj" -Targets:MyProject.targets

    .INPUTS
        None

    .OUTPUTS
        None
    
    .COMPONENT
        MSBuild, dotnet cli
#>
function Invoke-Build {
    [CmdletBinding()]
    [Alias("Build", "Dotnet-Build", "ib")]
    param(
        # True if you want to push the build package to the Local feed; false otherwise. Defaults to true.
        [Parameter(ValueFromPipeline = $true,
            ValueFromPipelineByPropertyName = $true,
            HelpMessage = "True if you want to push the build package to the Local feed; false otherwise. Defaults to true.")]
        [Alias("pl", "pshloc", "pushlocl", "pushloc" , "plocal")]
        [switch]$PushLocal = $false,

        # True if you want to push the build package to the GitHub NuGet feed; false otherwise. Defaults to false.
        [Parameter(ValueFromPipeline = $true,
            ValueFromPipelineByPropertyName = $true,
            HelpMessage = "True if you want to push the build package to the GitHub NuGet feed; false otherwise. Defaults to false.")]
        [Alias("pgh", "pshgh", "pushgh")]
        [switch]$PushGitHub = $false,

        # True if you want to push the build package to the Azure Artifacts NuGet feed; false otherwise. Defaults to false.
        [Parameter(ValueFromPipeline = $true,
            ValueFromPipelineByPropertyName = $true,
            HelpMessage = "True if you want to push the build package to the Azure Artifacts NuGet feed; false otherwise. Defaults to false.")]
        [Alias("paz", "pshaz", "pushaz")]
        [switch]$PushAzure = $false,

        # True if you want to push the build package to the NuGet.org feed; false otherwise. Defaults to false.
        [Parameter(ValueFromPipeline = $true,
            ValueFromPipelineByPropertyName = $true,
            HelpMessage = "True if you want to push the build package to the NuGet.org feed; false otherwise. Defaults to false.")]
        [Alias("pn", "png", "pnu", "pushng")]
        [switch]$PushNuGet = $false,

        # True if you want to restore the build package; false otherwise. Defaults to false.
        [Parameter(ValueFromPipeline = $true,
            ValueFromPipelineByPropertyName = $true,
            HelpMessage = "True if you want to restore the build package; false otherwise. Defaults to false.")]
        [Alias("no-restore")]
        [switch]$NoRestore = $false,
    
        # The configuration to build with. Defaults to "Local"
        [Parameter(ValueFromPipeline = $true,
            ValueFromPipelineByPropertyName = $true)]
        [Alias("c")]
        [string]$Configuration = "Local",

        # The verbosity of the build. Defaults to "minimal"
        [Parameter(ValueFromPipeline = $true,
            ValueFromPipelineByPropertyName = $true)]
        [ValidateSet("q", "quiet", "m", "minimal", "n", "normal", "d", "detailed", "diag", "diagnostic")]
        [string]$Verbosity = "minimal",

        # The targets to build. Defaults to "Build"
        [Parameter(ValueFromPipeline = $true,
            ValueFromPipelineByPropertyName = $true)]
        [Alias("t")]
        [string[]]$Target = @("Build"),

        # The version of the built package. Defaults to "0.0.1-Local"
        [Parameter(ValueFromPipeline = $true,
            ValueFromPipelineByPropertyName = $true)]
        [Alias("v")]
        [string]$Version = $null,

        # The version of the built assembly file. Defaults to "0.0.1"
        [Parameter(ValueFromPipeline = $true,
            ValueFromPipelineByPropertyName = $true)]
        [Alias("av", "asmv")]
        [string]$AssemblyVersion = $null,
    
        <# 
            Prints a list of available targets without executing the actual build process. 
            By default, the output is written to the console window. 
            If the path to an output file is provided that will be used instead. 
        #>
        [Parameter(ValueFromPipeline = $true,
            ValueFromPipelineByPropertyName = $true)]
        [Alias("targets", "show-targets", "ts")]
        [string]$PrintTargets = $null,

        # The path to the MSBuild project file to build. If a project file is not specified, MSBuild searches the current working directory for a file that has a file extension that ends in "proj" and uses that file.
        [Parameter(ValueFromPipeline = $true,
            ValueFromPipelineByPropertyName = $true,
            Mandatory = $false,
            Position = 0)]
        [Alias("project", "proj", "p", "pf", "file", "f")]
        [string]
        $ProjectFile,

        # Prints the help text
        [Parameter(ValueFromPipeline = $true,
            ValueFromPipelineByPropertyName = $true,
            ParameterSetName = "Help")]
        [Alias("?")]
        [switch]$Help
    )

    if ($Help) {
        if (@("q", "quiet", "m", "minimal").Contains($verbosity)) {
            Get-Help $MyInvocation.MyCommand.Name;
        }
        elseif (@("detailed", "d").Contains($Verbosity)) {
            Get-Help $MyInvocation.MyCommand.Name -Detailed;
        }
        else {
            Get-Help $MyInvocation.MyCommand.Name -Full;
        }
        return;
    }

    rm -rf ./bin;

    dotnet build `
        $ProjectFile `
        --nologo -v:$Verbosity `
        -c:$Configuration `
    $($PushLocal ? "-t:PushLocal" : "") `
    $($PushGitHub ? "-t:PushGitHub" : "") `
    $($PushAzure ? "-t:PushAzure" : "") `
    $($PushNuGet ? "-t:PushNuGet" : "");
}
