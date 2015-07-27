param (
	[string]$configuration = "Release"
)
. "$(resolve-path .\packages\nspec.*\tools\NSpecRunner.exe)" --formatter=XUnitFormatter bin\$configuration\Winston.Test.dll > results.xml
$wc = New-Object 'System.Net.WebClient'
$wc.UploadFile("https://ci.appveyor.com/api/testresults/xunit/$($env:APPVEYOR_JOB_ID)", (Resolve-Path .\results.xml))