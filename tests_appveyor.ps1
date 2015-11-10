$testAssembly="Winston.Test\bin\$configuration\Winston.Test.dll"
# Set by AppVeyor
$configuration=$env:CONFIGURATION

function RunNSpec {
    . "$(resolve-path .\packages\nspec.*\tools\NSpecRunner.exe)" --formatter=XmlFormatter $testAssembly > nspec_results.xml
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

function RunMSTest {
    $trxPath = "TestResults\ci.trx"
    If (Test-Path $trxPath) {
        Remove-Item -Force $trxPath
    }
    mstest /testcontainer:"$testAssembly" /resultsfile:"$trxPath"
    [xml]$trx = Get-Content "$trxPath"
    $results = $trx.TestRun.Results.UnitTestResult | select testName,outcome,duration
    foreach($r in $results) {
        Add-AppveyorTest -Name "$r.testName" -Outcome "$r.outcome" -Duration "$r.duration"
    }
}

RunNSpec
RunMSTest
