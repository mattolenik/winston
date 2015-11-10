param (
	[string]$configuration = "Release"
)

$testAssembly="Winston.Test\bin\$configuration\Winston.Test.dll"

& "$(resolve-path .\packages\nspec.*\tools\NSpecRunner.exe)" --formatter=XmlFormatter "$testAssembly" > nspec_results.xml
[xml]$res = Get-Content .\nspec_results.xml
foreach($c in $res.Contexts.Context) {
    $spec = $c.Context.Specs.Spec
    foreach($s in $spec) {
        $name = "$($c.Name) $($s.Name)"
        $status = $s.Status
        Add-AppveyorTest -Name $name -Outcome $status
    }
}

vstest.console /logger:Appveyor "$testAssembly"