param (
	[string]$configuration = "Release"
)
$reporter = @{$true="-appveyor";$false="-verbose"}[$env:CI -eq "True"]
$testAssembly="Winston.Test\bin\$configuration\Winston.Test.dll"
& "$(resolve-path .\packages\xunit.runner.console.*\tools\xunit.console.exe)" $testAssembly $reporter