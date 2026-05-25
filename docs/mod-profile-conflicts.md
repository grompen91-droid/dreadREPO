# Mod profile conflict notes (mk profile)

Runtime log review for Dread audio failures did **not** show Harmony patches from other mods blocking audio loading.

**Mimic_Patcher (`3.0.0-Final`) is unrelated.** Its warning only removes a conflicting **Mimics** `SetupEnemies` patch at startup. It does not touch Dread, NVorbis, or `AudioClipLoader`.

Confirmed audio failure (latest run): NVorbis could not load because `System.Memory.dll` was missing from the plugin folder (only `NVorbis.dll` was deployed).

## Installed mods (15 plugins)

| Mod | Conflict risk with Dread |
|-----|--------------------------|
| Zehs-REPOLib | Low: bundle/assets only |
| randomlygenerated-Mimic_Patcher | Low: removes **Mimics** patches only (see BepInEx log) |
| eth9n-Mimic | Low: works with Mimic_Patcher |
| BULLETBOT-MoreUpgrades | Low |
| Zehs-ExtractionPointConfirmButton | Low |
| DarkSpider-TextUpgradesUIScale | Low |
| HappyCats-DeathMinimap | Low |
| HeroHanex-NoItemSpawnLimit | Low |
| nickklmao-MenuLib / REPOConfig | Low |
| Omniscye-Empress_LateJoin | Low |
| TheRavenNest-UnlimitedOrbs | Low |
| Magic_Wesley-Wesleys_Enemies | Low |
| elytraking-Dread | — |

## Isolation test

To confirm no third-party conflict, duplicate the profile in r2modman with **only** BepInEx + Dread (+ REPOLib if required). If audio still failed before NVorbis loader, the cause was Unity file URLs on Linux, not another mod.

## Dread fix (v1.5.2+ unreleased)

Audio now loads via **NVorbis** direct disk read, then falls back to `UnityWebRequest` on Windows-native installs.
