$runner = Get-ChildItem -Recurse -Filter NSpecRunner.exe
$testDll = Get-ChildItem -Recurse -Filter Winston.Test.Dll | where { $_.FullName -match "bin\\Debug" }
& $runner.FullName $testDll.FullName