param (
    [Parameter(Mandatory = $true)]
    [string]$template
)
packer build -force -only=virtualbox-iso "$template"