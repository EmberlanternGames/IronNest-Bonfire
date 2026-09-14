# Bonfire

## Description

This is a difficulty enhancing mod, similar to Subnautica Deathrun, adding a suite of difficulty adjustments to the game.

Current Features:
- Break Guns: Apply a limited range to your guns by limiting the max charges and max angle per shot.
- Caffeine Addict: Adds a strong motivation to make coffee every day. If you have no caffeine or a poor cup of coffee, you cannot sprint and you move slower. A good cup will let you function like a human being and not a zombie. A GREAT cup will give you a speed bonus to your sprint for a certain amount of time (varies by coffee quality).
- Engine-Out: Makes the engine a necessary component of the nest by tying all pressure systems to a functioning engine. No engine means no pressure. No pressure means no loading or aiming.

Feature Ideas:
- Imprecise Movement: The nest is not very accurate at moving long distances. a +-1 degree differential in either direction means you can't be sure where you ended up, only where along the edge of the distance circle you lie. Position reports and re-triangulation is recommended. Intended to pair "well" with Break Guns (and by that I mean synergy in making the game harder).
- Brittle Armor: Impacts of your own shells damage you.

## Requirements

MelonLoader 0.7.3 or newer. Go to https://melonwiki.xyz/ to get the installer and point it at Iron Nest's executable. Then place the mod dll in the `Mods` folder in your Iron Nest install location.

Alternatively, you may also use this with BepInEx - get it from the [bleeding edge page](https://builds.bepinex.dev/projects/bepinex_be), the "BepInEx Unity (IL2CPP) for Windows (x64) games" version - but only if you install the [MLLoader mod](https://www.nexusmods.com/ironnest/mods/26) alongside it and place the mod in `MLLoader/Mods`.

- Note: It will take a while to launch the game when you first load after installing MelonLoader, don't exit if it looks frozen, it's just generating files and will be back shortly. This also happens with game updates.

## Installation

1. Extract the mod into the game folder. If all goes right, the mod file should end up at `[Iron Nest Directory]/Mods/Bonfire.dll`
2. Configure: Check the config file at `[Iron Nest Directory]/UserData/MelonPreferences.cfg` - Enable any Bonfire features you desire and configure them appropriately
3. Test: Load the game and make sure the mod works by testing a feature you activated. Example: If you have BreakGuns enabled, the powder charge delivery system should be limited to [maxCharges] and the elevators for the guns will halt at [maxAngle] degrees.

## Bug Reporting

Please include the following:
- What you were doing that broke things?
- Add `BepInEx/LogOutput.log` with VerboseLogging enabled in the config.

Please submit to the bugs to the Bugs tab in Nexus.

## Mod Compatibility

This mod will overhaul many systems in due time. Some mods may not be compatible. If the mods affect gameplay features that already exist, be warned!

## AI Disclosure

This codebase relies on code techniques pulled from projects that utilized AI (used menu building techniques from vergeslich03's [APNest-Client mod](https://github.com/vergeslich03/APNest-Client/tree/main))

Otherwise, this code was written 100% by a fleshy meatbag. AI was used to assist (acting as a sounding board, providing planning aid, and analyzing/reviewing my code).

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

This project also includes third-party code — see [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) for details.