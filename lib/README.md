# Local build dependencies

Place `sts2.dll` and `0Harmony.dll` from your Slay the Spire 2 installation in
this directory before building. Use the versions matching your installed game.
These local dependency binaries are ignored by Git.

The current target is **0.113.0**. Copy both DLLs from the game's
`data_sts2_windows_x86_64` directory after updating the game; building against an
old local DLL does not verify compatibility with the updated game.

RitsuLib is restored from NuGet at exactly **0.6.11**. Install the matching
RitsuLib runtime in the game (including its `compat/0.113.0` and `shared`
directories). Do not distribute its DLLs with Sunqian Universe.
