param (
	[string]$configuration = "Release"
)

function NSpec {
    . "$(resolve-path .\packages\nspec.*\tools\NSpecRunner.exe)" --formatter=XmlFormatter Winston.Test\bin\$configuration\Winston.Test.dll > nspec_results.xml
    [xml]$res = Get-Content .\nspec_results.xml
    foreach($c in $res.Contexts.Context) {
        $spec = $c.Context.Specs.Spec
        foreach($s in $spec) {
            $name = "$($c.Name) $($s.Name)"
            $status = $s.Status
            Add-AppveyorTest -Name $name -Outcome $status
        }
    }
}

function MSTest {
    mstest /testcontainer:"$(Resolve-Path **\bin\$configuration\Winston.Test.dll)" /resultsfile:TestResults\ci.trx
    [xml]$trx = Get-Content TestResults\ci.trx
    $results = $trx.TestRun.Results.UnitTestResult | select testName,outcome,duration
    foreach($r in $results) {
        Add-AppveyorTest -Name "$r.testName" -Outcome $r.outcome -Duration $r.duration
    }
}

NSpec
MSTest
