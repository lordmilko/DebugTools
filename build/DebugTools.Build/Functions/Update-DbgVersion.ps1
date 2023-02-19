<#
.SYNOPSIS
Increments the version of all components used when building DebugTools

.DESCRIPTION
The Update-DbgVersion cmdlet increments the version of DebugTools by a single build version. The Update-DbgVersion should typically be run when preparing to release a new version. The changes to the DebugTools repo caused by running the Update-DbgVersion cmdlet are typically commited as the "release" of the next DebugTools version. Once pushed to GitHub, the CI system will mark the build and all future builds as "release candidates" until the version is actually released.

If you wish to decrement the build version or change the major, minor or revision version components, you can do so by overwriting the entire version using the Set-DbgVersion cmdlet.

For more information on the version components that may be processed, please see Get-Help Get-DbgVersion

.EXAMPLE
C:\> Update-DbgVersion
Increment the DebugTools build version by 1.

.LINK
Get-DbgVersion
Set-DbgVersion
#>
function Update-DbgVersion
{
    [CmdletBinding()]
    param()

    $version = Get-DbgVersion -ErrorAction SilentlyContinue

    $major = $version.File.Major
    $minor = $version.File.Minor
    $build = $version.File.Build + 1
    $revision = $version.File.Revision

    $newVersion = [Version]"$major.$minor.$build.$revision"

    Set-DbgVersion $newVersion
}