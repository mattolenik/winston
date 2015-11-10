param (
	[string]$configuration = "Release"
)

$testAssembly="Winston.Test\bin\$configuration\Winston.Test.dll"

echo "Running NSpec tests"
& "$(resolve-path .\packages\nspec.*\tools\NSpecRunner.exe)" --formatter=XmlFormatter "$testAssembly" > nspec_results.xml
[xml]$res = Get-Content .\nspec_results.xml
foreach($c in $res.Contexts.Context) {
    $spec = $c.Context.Specs.Spec
    foreach($s in $spec) {
        $name = "$($c.Name) $($s.Name)"
        $status = $s.Status
        $error = ""
        $stackTrace = ""
        if ($status -eq "Failed") {
            $ex = $s.Exception.InnerText
            # Remove newlines for stacktrace parameter
            $stackTrace = [System.Text.RegularExpressions.Regex]::Replace($ex, "\r\n?|\n", "\\")
            # Use first line of error for message
            $message = $ex.Split([System.Environment]::NewLine)[0]
        }
        Add-AppveyorTest -Name "$name" -Outcome "$status" -FileName "$(Get-Item $testAssembly | select -ExpandProperty Name)" -ErrorMessage "$message" -ErrorStackTrace "$stackTrace"
    }
}

vstest.console /logger:Appveyor "$testAssembly"