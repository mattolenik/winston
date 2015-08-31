function winston {
    winstonapp.exe $args
    if(($LastExitCode -eq 2) -or ($LastExitCode -eq 3)) {
        $env:PATH=mergepathstrings -p
    }
}

Export-ModuleMember -Function 'winston'