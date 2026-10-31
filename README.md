# UniDocVerter

A lightweight Windows desktop application for local conversion of image, audio
and video files using FFmpeg.

## Status

Early development. Supported conversions and the user interface may change.

## Features

- Windows desktop interface
- Russian and English UI; language preference is stored locally
- Choose a media file or drag it into the application
- Accept a file path as a command-line argument
- Detect media streams with FFprobe
- Convert media locally with FFmpeg
- Optional Windows Explorer context-menu entry for the current user
- No application telemetry or account required

## Supported formats

The application currently offers these output formats based on detected streams:

- Images: PNG, JPG, WebP
- Video: GIF, MP4, WebM, MKV
- Audio: MP3, WAV, AAC, Opus, M4A

A listed conversion can still fail if the installed FFmpeg build lacks the
required encoder or if the input is invalid. The application does not convert
office documents or PDF files.

## Requirements

- Windows 10 or later
- .NET 10 SDK to build from source
- FFmpeg and FFprobe available either:
  - beside `UniDocVerter.exe`, or
  - through the system `PATH`

## Build

```powershell
dotnet build
Run from source:

dotnet run

Pass a media file on startup:

dotnet run -- "C:\path\to\media.mp4"

Portable build

Create a self-contained Windows x64 build:

dotnet publish -c Release -r win-x64 --self-contained true -o .\artifacts\portable

FFmpeg and FFprobe are separate third-party components. Before distributing a build that bundles them, review the license terms and notices for the exact FFmpeg build. Do not assume that copying only ffmpeg.exe and ffprobe.exe is sufficient.


Context menu

PowerShell scripts for adding and removing a context-menu entry are in the scripts directory. Review the scripts before running them.


The install script currently points to the local test build at artifacts\portable-test. Adjust the path before using it with a release build.


Privacy

UniDocVerter has no analytics, telemetry, accounts, or application update service. Conversion is performed locally by FFmpeg. The app stores the selected UI language in the current user's local application data.


License

The license for UniDocVerter source code is in the LICENSE file. Third-party components have their own licenses and notices.