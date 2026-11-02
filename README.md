# WHAT DID YOU SAY?

A meme client mod for SPT 4.1.6. Gunshots, grenade explosions, and the airdrop plane engine get **+2000% sample gain (21x)** by default.

Includes suppressed gunshots and their tails. Footsteps, voices, music, UI sounds, grenade handling/bounces and smoke hiss keep their normal gain. Direction, distance, occlusion and the game's volume controls still apply. At this gain, loud sounds can clip/distort; 21x sample amplitude does not mean 21x perceived loudness.

## Install

Close the game, then extract `WhatDidYouSay-1.0.0.zip` into your SPT game folder. The DLL goes in `BepInEx/plugins/WhatDidYouSay/`. No server component is needed.

## Settings

F12: **WHAT DID YOU SAY?**. Enable/disable the mod, or change each of the three volume boost sliders separately. 0% = normal; 100% = 2x; 2000% = 21x. Changes apply to active boosted sounds as well as new ones. The config is generated on first launch and is not bundled in the ZIP.

To uninstall, close the game and remove `BepInEx/plugins/WhatDidYouSay/`.

## Build

`dotnet build Client/WhatDidYouSay.csproj -c Release -warnaserror`

Requires local game references at `C:\SPT` (override with `-p:SPTPath=...`). The plugin uses the installed game's audio pool and plane playback hooks. No game assets are redistributed.
