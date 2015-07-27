[xml]$res = Get-Content .\results.xml
foreach($c in $res.Contexts.Context) {
    $spec = $c.Context.Specs.Spec
    foreach($s in $spec) {
        $name = "$($c.Name) $($s.Name)"
        $status = $s.Status
        Add-AppveyorTest -Name "$name" -Outcome "$status"
    }
}