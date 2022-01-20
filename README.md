# MinecraftSelectors

Adds Minecraft-like player selectors to SCP:SL to use with commands.

Most commands in RA (Remote Admin) are executed on players. The vanilla commands use a built-in method to parse the
player list; the IDs are separated by dots.<br>
This plugin adds the opportunity to use selectors like in Minecraft. Example: `@a[name=Player]`

# Installation

**You need to install [EXILED](https://github.com/Exiled-Team/EXILED/)** for this plugin to work.

1. Download the **MinecraftSelectors.dll** from the releases page.
2. Launch your server with **EXILED** installed.
3. Place the **MinecraftSelectors.dll** into the _Plugins_ folder (**%appdata%\EXILED\Plugins**)
4. Restart the server.

# Config

| Property                 | Type    | Default | Description                                                          |
|--------------------------|---------|---------|----------------------------------------------------------------------|
| is_enabled               | Boolean | True    | If the plugin should load and Minecraft selectors are to be enabled. |
| allow_advanced_selectors | Boolean | True    | If advanced selectors should be allowed.                             |

# Basics

### _To view help in-game, use the `mcs` command in RA or in the Server Console._

Currently, **there are 3 basic selectors**. All players (**_a_**), random player (**_r_**)
and self (**_s_**). "Self" is the executor of the command.

To use the selectors instead of a player ID list, type '@' and the selector's character. **Example: `@a`**

# Advanced Selectors

Advanced selectors are used to narrow down the list of selected players.<br>
To prepare advanced selectors, type '[]' after the basic selector: `@a[]`<br>
**Between the brackets, type the advanced selectors.** Selectors are **separated by commas**, and have **one or two
sides**.

**A one-sided selector** is a selector with a **true or false** value. To invert the selector, type '!' before it.
Examples: `@r[scp]` - random SCP; `@a[!scp]` - non-SCP players.

**A two-sided selector requires a value.** The property and the value is **separated by '='.** To invert the selector,
use '!=' instead of '=', or type '!' before the property. **Examples:** `id!=2` - inverted; `id=2` - not inverted.

Some two-sided selectors allow ranged values. The minimum and/or the maximum or just a constant value should be set.
**Examples:** `..5` - max 5; `1..` - min 1; `6..9` - between 6 and 9. **The Range Check can also be inverted.**

# List of Advanced Selectors

| Selector    | Aliases | Sides | Range Check Support     | Description                               | Example(s)                |
|-------------|---------|-------|-------------------------|-------------------------------------------|---------------------------|
| playerid    | id      | 2     | Yes                     | If the player's ID in RA equals the value | `@a[id=2..]`, `@r[id!=1]` |
| class       | role, r | 2     | Yes; see values section | If the player is playing as that class    | `@a[r=classd]`, `@a[r=2]` |
| scp         | -       | 1     | No                      | If the player is an SCP                   | `@r[scp]`                 |
| godmode     | god     | 1     | No                      | If the player has godmode on              | `@r[!god]`                |
| noclip      | -       | 1     | No                      | If the player has noclip on               | `@a[noclip]`              |
| verified    | -       | 1     | No                      | If the player's Steam account is verified | `@a[verified]`            |
| team        | -       | 2     | Yes; see values section | If the player's team equals the value     | `@r[team=cdp]`            |
| remoteadmin | ra      | 1     | No                      | If the player is logged into RA           | `@a[!ra]`                 |
| bypass      | -       | 1     | No                      | If the player has bypass mode on          | `@r[!bypass]`             |
| donottrack  | dnt     | 1     | No; see values section  | If the player has DNT enabled             | `@a[dnt]`                 |
| name        | -       | 2     | No; case insensitive    | If the player's name equals the value     | `@a[name=Player]`         |
| namehas     | -       | 2     | No; case insensitive    | If the player's name contains the value   | `@a[namehas=a]`           |

# Values

## Roles:

The type, the name or the ID can be passed as a value. Range Check can be used with ID.

| RoleType       | Name in RA Forceclass | ID |
|----------------|:---------------------:|----|
| Scp173         |        SCP-173        | 0  |
| ClassD         |        Class-D        | 1  |
| Spectator      |       Spectator       | 2  |
| Scp106         |        SCP-106        | 3  |
| NtfSpecialist  |     NTF Specialist    | 4  |
| Scp049         |        SCP-049        | 5  |
| Scientist      |       Scientist       | 6  |
| Scp079         |        SCP-079        | 7  |
| ChaosConscript |    Chaos Conscript    | 8  |
| Scp096         |        SCP-096        | 9  |
| Scp0492        |       SCP-049-2       | 10 |
| NtfSergeant    |      NTF Sergeant     | 11 |
| NtfCaptain     |      NTF Captain      | 12 |
| NtfPrivate     |      NTF Private      | 13 |
| Tutorial       |        Tutorial       | 14 |
| FacilityGuard  |     Facility Guard    | 15 |
| Scp93953       |       SCP-939-53      | 16 |
| Scp93989       |       SCP-939-89      | 17 |
| ChaosRifleman  |     Chaos Rifleman    | 18 |
| ChaosRepressor |    Chaos Repressor    | 19 |
| ChaosMarauder  |     Chaos Marauder    | 20 |

## Teams:

The team type or the ID can be passed as a value. Range Check can be used with ID.

| Team           |       Roles       | ID |
|----------------|:-----------------:|----|
| SCP            |        SCPs       | 0  |
| MTF            | Mobile Task Force | 1  |
| CHI            |  Chaos Insurgency | 2  |
| RSC            |     Scientists    | 3  |
| CDP            |     Class-D's     | 4  |
| RIP            |     Spectators    | 5  |
| TUT            |   Tutorial Class  | 6  |

### Bypass mode: The player can open any door/locker without a required keycard.

### Noclip: The player can fly and phase through objects.

### DNT (Do Not Track): Forbids the server to use Steam ID or IP for cases not related to server security. All data like this should be removed after 24 hours if the player has DNT on.