[![Build status](https://ci.appveyor.com/api/projects/status/7mdi9a8ru90qyr2n?svg=true)](https://ci.appveyor.com/project/MattOlenik/winston-g7l7d)

Prototype frictionless package manager for Windows. Developed as an alternative to tools like Chocolatey. It exclusively handles non-installer programs, sticking only to binary distributions or installers that can be cracked open. Winston will modify your shell's PATH variable for you to make newly installed tools available immediately.

#  Features

Winston's main features are:

* Triviality of adding packages, including heuristics that infer package structure and cut down on boilerplate
* Easy to host new package "repos" -- just a JSON file served from literally anywhere (disk, REST service, Dropbox)
* Automatically injects new PATH variable changes into your shell session, removing the need for clumsy BAT files (great care is put into never polluting the PATH, and it can be disabled)
* PATH manipulation works from any process -- useful when invoking from a build tool
* Entirely async API meant for integration or extension

Other planned use cases include a lightweight build tool. There is very little effort required to add a new package and go.

#  Demo

Simple example of installing Microsoft's win32 port of OpenSSH:
![Winston example install](http://i.imgur.com/eN0lUdE.png)

#  Project State

Winston is usable right now, though rough and undocumented. It has a surprising number of tests and the major workflows work well. More tests are needed, along with further refinement of features and workflow.