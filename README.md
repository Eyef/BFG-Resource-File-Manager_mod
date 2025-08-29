BFG Resource File Manager MOD
=========================

This is a file manager for DOOM 3 BFG Edition/DOOM 3 (2019) .resources files.
It allows you to see, extract, delete, preview and edit the files inside .resources files.
Also it allows you to create and edit .resource files.

THE PROJECT HAS PRE_BUILD BINARIES (.EXE FILES) HERE: https://github.com/Eyef/BFG-Resource-File-Manager_mod/releases
YOU DONT HAVE TO FOLLOW THE INSTRUCTIONS BELOW. THOSE INSTRUCTIONS ARE FOR DEVELOPERS WHO WANT TO TEST OR CONTRIBUTE TO THE PROJECT.
IF YOU ARE A LINUX USER MAKE SURE TO DOWNLOAD AND INSTALL THE MONO PROJECT: https://www.mono-project.com/download/stable/#download-lin

Differences from the original BFG Resource File Manager 1.0.7
=============================================================

- Added scroll bars to the .bimage preview window, allowing you to see the entire images when this tool is running in windowed mode or on a small screen.
- Added panel with bimage information (image size in pixels, texture type and format)
- Extended idwav player functionality (now displays file duration in seconds, file format, number of channels, sample rate, added simple progress bar)
- Fixed an error that occurred when cancelling file export after selecting a path for saving.
- Added preview of dat font files (48.dat) as a table of characters with their metrics, as well as the number of glyphs in the font, ascender and descender values.
- Added Help - About menu item to the main window of the tool, which opens a small window with the version number of this tool, its author and a link to github.

Build it yourself (BIY)
=======================

Windows:
- Install Microsoft Visual Studio (2017 or later) Community (is free to download and Install)
- Open the ResourceFileEditor.sln
- Select the desired configuration (Debug or Release)
- right click the ResourceFileEditor Project and select build
- go to the bin folder and you will find the binary inside the folder coresponding to the selected profile

Linux:
- Install mono-project
- Install nuget
- with the terminal go to the location of the project (where the .sln is).
- type `nuget install`
- type msbuild /t:build /p:Configuration=<desired configuration (Debug or Release)>

nuget Depedencies Licenses
==========================

- StbImageSharp - Public Domain
- StbImageWriteSharp - Public Domain
- BCnEncoder.Net45 - MIT or Unlicensed
- System.Numerics.Vector - MIT


