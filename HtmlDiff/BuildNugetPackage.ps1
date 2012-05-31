function Pause ($Message="Press any key to continue...")
{
	Write-Host -NoNewLine $Message
	$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
	Write-Host ""
}

$version = Read-Host "Please enter a version number"

$projectFilePath = 'HtmlDiff.csproj'
$expression = "nuget pack $projectFilePath -Version ""$version"" -Prop Configuration=Release"
Invoke-Expression $expression
		
Pause