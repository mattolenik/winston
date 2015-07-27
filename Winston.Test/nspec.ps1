param (
	[string]$configuration = "Release"
)
. "$(resolve-path ..\packages\nspec.*\tools\NSpecRunner.exe)" bin\$configuration\Winston.Test.dll