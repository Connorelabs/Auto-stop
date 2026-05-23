# CounterStrafe

CounterStrafe is a Windows helper for Counter-Strike 2 that adds configurable counter-strafe input without reading game memory.

It is designed around one goal: make stopping feel sharp without fighting your own hands, your jump movement, or your current weapon context.

## Highlights

- Global `W/A/S/D` monitoring with axis-aware counter-strafe
- Last-input priority, so opposite directions do not conflict with each other
- Manual-input priority, so your own correction can immediately take over
- Jump protection for `Space` jump and mouse-wheel jump
- Weapon-aware suspension through CS2 Game State Integration
- Persistent strength tuning that survives restarts
- Simple file-based config and no hard dependency on local machine-specific paths

## Technical advantages

### No-conflict input arbitration

The helper tracks horizontal and vertical movement as separate axes and always gives priority to the latest real movement input on that axis.

That means:

- `A/D` spam does not replay stale input
- `W/S` does not interfere with `A/D`
- synthetic input is canceled as soon as real input needs to take control

### Manual override protection

If you decide to counter-strafe manually, the helper backs off.

Protection includes:

- immediate cancellation of the active synthetic key on that axis
- suppression of the synthetic release that would otherwise fight your own key
- a configurable manual-priority protection window so the next auto tap does not instantly kick back in

### Jump and bhop protection

The helper can suspend itself when movement is likely to be airborne:

- while `Space` is held
- for a configurable window after `Space` is released
- for a configurable window after mouse-wheel jump input

This is meant to reduce interference with bhop timing, air-strafe, and general movement tech.

### GSI-driven weapon-aware control

When CS2 sends Game State Integration weapon updates, the helper can decide whether counter-strafe should stay on or suspend itself.

By default it suspends for:

- knives
- grenades
- C4

That logic is configurable by weapon type and by exact weapon name.

### Persistent tuning

Your current strength is saved to a runtime state file whenever you adjust it with hotkeys, so the helper restarts with the same feel you were using last time.

## Repository layout

Important files:

- `CounterStrafe.csproj`
- `Program.cs`
- `README.md`
- `config/counterstrafe.json`
- `config/gamestate_integration_counterstrafe.cfg`

Generated at runtime:

- `config/counterstrafe.state.json`

Ignored by git:

- `bin/`
- `obj/`
- `config/counterstrafe.state.json`

## Requirements

- Windows
- .NET 10 SDK if you want to build from source
- Counter-Strike 2 if you want GSI-based weapon-aware suspension

## Build

```powershell
dotnet build
```

## Run

### Run from the project folder

```powershell
dotnet run
```

### Run the built executable

1. Build the project.
2. Open `bin/Debug/net10.0-windows/`.
3. Run `CounterStrafe.exe`.

The build output includes a `config/` folder containing the config files you need.

## Self-test

```powershell
dotnet run -- --self-test
```

## Default controls

- `F6`: decrease strength
- `F7`: increase strength
- `F8`: toggle helper on or off
- `F9`: send a manual `A` tap for testing
- `F10`: exit

Suspend behavior:

- Hold `Shift`: temporary suspend
- Hold `Space`: temporary suspend
- Mouse wheel jump: temporary suspend for the configured jump window

## Config folder behavior

The helper looks for a `config` folder in this order:

1. the folder you launch the helper from
2. the folder containing the executable

Inside that folder:

- `counterstrafe.json` is the editable config
- `counterstrafe.state.json` stores the currently saved strength
- `gamestate_integration_counterstrafe.cfg` is the CS2 GSI template

If `counterstrafe.json` is missing, the helper falls back to built-in defaults.

## Main config

File:

`config/counterstrafe.json`

Example:

```json
{
  "gameStateListenerPrefix": "http://127.0.0.1:3001/",
  "strengthStepMilliseconds": 5,
  "minimumStrengthMilliseconds": 20,
  "maximumStrengthMilliseconds": 120,
  "defaultStrengthMilliseconds": 60,
  "spaceReleaseSuspendMilliseconds": 40,
  "mouseWheelJumpSuspendMilliseconds": 220,
  "manualOverrideSuppressMilliseconds": 80,
  "hotkeys": {
    "decreaseStrength": "F6",
    "increaseStrength": "F7",
    "toggleEnabled": "F8",
    "testTapA": "F9",
    "exit": "F10"
  },
  "weaponSuppress": {
    "types": [
      "knife",
      "grenade",
      "c4"
    ],
    "names": [
      "weapon_knife",
      "weapon_knife_t"
    ]
  }
}
```

## Config reference

### Strength tuning

- `strengthStepMilliseconds`: amount changed by the increase/decrease hotkeys
- `minimumStrengthMilliseconds`: minimum allowed strength
- `maximumStrengthMilliseconds`: maximum allowed strength
- `defaultStrengthMilliseconds`: startup strength if no saved state exists yet

### Protection timing

- `spaceReleaseSuspendMilliseconds`: extra protection after releasing `Space`
- `mouseWheelJumpSuspendMilliseconds`: protection after mouse-wheel jump input
- `manualOverrideSuppressMilliseconds`: manual-priority window after you take over an axis yourself

### GSI listener

- `gameStateListenerPrefix`: local HTTP address used by the helper for CS2 GSI

### Hotkeys

- `decreaseStrength`
- `increaseStrength`
- `toggleEnabled`
- `testTapA`
- `exit`

### Weapon-aware suspension

- `weaponSuppress.types`: suspend by weapon type from GSI
- `weaponSuppress.names`: suspend by exact weapon name from GSI

## Supported hotkey names

You can use:

- `F1` to `F24`
- `A` to `Z`
- `0` to `9`
- `Tab`
- `Enter`
- `Shift`
- `Ctrl`
- `Alt`
- `Space`
- `Esc`
- `Left`, `Right`, `Up`, `Down`
- `Insert`, `Delete`, `Home`, `End`, `PageUp`, `PageDown`

Hotkey names are case-insensitive.

## Saved strength state

File:

`config/counterstrafe.state.json`

This file is created automatically after you adjust strength. It stores the current active strength so the helper can restore it on the next run.

If you want to reset back to the configured default, delete that file and restart the helper.

## CS2 GSI setup

The helper does not read game memory. Weapon-aware behavior uses CS2 Game State Integration instead.

### Step 1: copy the GSI config file

Take:

`config/gamestate_integration_counterstrafe.cfg`

Copy it into the CS2 config folder:

`Steam/steamapps/common/Counter-Strike Global Offensive/game/csgo/cfg/`

### Step 2: restart CS2

Completely close and reopen the game after adding or editing the file.

### Step 3: run the helper and switch weapons

If GSI is working, the helper window will print messages like:

- `Weapon active: weapon_ak47 [Rifle]`
- `Weapon suppress: weapon_knife [Knife]`

That is the signal that the helper is receiving weapon updates and making on/off decisions automatically.

## Troubleshooting

### The helper runs but the game does not react

- If CS2 is running as administrator, start the helper as administrator too.
- Test with `F9` in Notepad first. If Notepad receives the key, Windows input injection is working.

### Weapon detection does not work

- Make sure `gamestate_integration_counterstrafe.cfg` is in the CS2 `game/csgo/cfg/` folder.
- Restart CS2 after adding or editing the file.
- Make sure the helper is listening on the same address as `gameStateListenerPrefix`.

### My hotkeys or settings are not changing

- Edit `config/counterstrafe.json`
- Save the file
- Restart the helper
- Check the helper window to confirm which config folder it loaded

### My saved strength is wrong

- Delete `config/counterstrafe.state.json`
- Restart the helper

## GitHub readiness

This repository is structured so it can be uploaded directly:

- no machine-specific paths in the documentation
- runtime-only state excluded from git
- config templates stored in a dedicated `config/` folder
- build output isolated in `bin/` and `obj/`
- line-ending normalization included through `.gitattributes`

If you want a public release, the only thing still worth choosing yourself is a license.

## Notes

- This tool uses synthetic input and Windows global hooks.
- Behavior can vary depending on permissions, launch mode, and third-party software restrictions.
