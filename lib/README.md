# Local build dependencies

`release/version-public` builds against `public/sts2.dll`, `public/0Harmony.dll`
and `public/GodotSharp.dll` from game 0.107.1. Cache them with:

```powershell
.\devtools\cache-game-references.ps1 -Channel public
```

Keep beta references separately in `beta/`. When the beta game is installed,
`-Channel beta` refreshes that cache and the legacy `lib/sts2.dll` and
`lib/0Harmony.dll` paths still used by main. Switching branches does not replace
either cache. Both cache directories and the legacy DLLs are ignored by Git.
Each cache also contains its source game's `release_info.json`.
