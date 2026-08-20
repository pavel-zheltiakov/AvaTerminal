# Third-party notices

AvaTerminal builds on the components below. Their licences govern them and are not superseded by
`LICENSE.md`; the licence identifiers here are the ones each package declares in its own metadata.

Nothing in this list restricts commercial use, and none of it requires you to attribute anything in
your own application's user interface.

## Used by the library

| Component | Version | Licence |
| --- | --- | --- |
| [Avalonia](https://github.com/AvaloniaUI/Avalonia) | 12.1.0 | MIT |

That is the whole list, and it is worth being precise about why it is so short. A terminal emulator
is usually assembled from other people's parts — libvterm for the escape sequences, a pty wrapper for
the process, a font shaper for the cells. None of that is here:

- **The VT parser is ours.** CSI, OSC, DCS and SGR, the alternate screen, scrollback, wide characters,
  combining marks and reflow on resize. No libvterm, no xterm.js, no VtNetCore.
- **The pty is the kernel's**, reached through 38 `[LibraryImport]` declarations against `libc` —
  `posix_openpt`, `grantpt`, `unlockpt`, `ptsname_r`, `posix_spawn`, `ioctl`. There is no pty NuGet
  package involved, because a pty is a system call rather than a library.
- **The shell is ours too.** `AvaTerminal` runs no external shell by default: it searches `PATH`
  itself and runs what it finds on a pty. `zsh`, `bash` and `cmd.exe` are all optional.

Avalonia rasterises the glyphs, which is the one thing above the kernel that is not ours: the library
decides which glyph goes in which cell at which pixel, and Avalonia's drawing context turns an outline
into pixels.

## Used by the samples only

| Component | Version | Licence |
| --- | --- | --- |
| [Avalonia.Desktop](https://github.com/AvaloniaUI/Avalonia) | 12.1.0 | MIT |

`Avalonia.Desktop` is referenced by the sample applications and **not** by the library. A control does
not get to decide which windowing backend the application it is dropped into uses.

## Used by the tests only

| Component | Version | Licence |
| --- | --- | --- |
| [Avalonia.Headless](https://github.com/AvaloniaUI/Avalonia) | 12.1.0 | MIT |
| [Avalonia.Headless.XUnit](https://github.com/AvaloniaUI/Avalonia) | 12.1.0 | MIT |
| [Avalonia.Skia](https://github.com/AvaloniaUI/Avalonia) | 12.1.0 | MIT |
| [xUnit.net v3](https://github.com/xunit/xunit) | 3.2.2 | Apache-2.0 |
| [Microsoft.NET.Test.Sdk](https://github.com/microsoft/vstest) | 17.14.1 | MIT |
