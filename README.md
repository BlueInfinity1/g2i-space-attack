# Space Attack

A fresh Unity arcade project using Unity primitives, an authored scene, reusable prefabs, and separate configuration assets. All code was written through Codex. No generated or downloaded artwork is used.

## Run in Unity

1. Add this folder in Unity Hub and open it with **Unity 6000.3.11f1**.
2. Open **Assets/SpaceAttack/Scenes/Game.unity** and press **Play**.
3. Click **Start** or press **Enter**. Click the Game view if keyboard input is not focused there.

After this feedback update, stop Play mode and reopen the scene from disk before testing; the scene hierarchy and serialized UI references changed. **Space Attack > Open Game Scene** opens it.

| Control | Action |
| --- | --- |
| A / D or left / right arrows | Move horizontally |
| Hold Space | Fire |
| Enter | Start; restart after game over |
| R | Restart after game over |
| Escape / Settings button | Open settings and pause; Escape / Close resumes |
| F | Toggle reduced effects |
| M | Toggle audio mute |

This iteration is for **Unity Editor testing**. No WebGL build has been made.

## Gameplay

Every stage begins with 41 enemies arranged in rows of 2, 5, 7, 9, 9 and 9. They all die from one player shot. The formation moves slowly between horizontal limits, speeding up with difficulty. Selected enemies attack diagonally at a constant speed and may reverse horizontal direction as the attack continues. They are removed without points if they leave the playfield. Destroying or outlasting the formation advances to the next stage. Attacks become more frequent, with a cap on difficulty and simultaneous attackers.

Enemies can fire only after leaving formation for a diagonal attack; horizontal formation movement does not enable firing. Attacking enemies fire orange projectiles straight down on a six-second cooldown. The initial delay may elapse while an enemy waits in formation, so it can fire as its attack starts. Each enemy's interval is independent of its previous projectiles. Player and enemy weapon configs are separate.

Red, green and yellow enemies award 30, 40 and 60 points in formation. Attacking enemies award 4–10 times their base score according to vertical depth, rounded to tens. The formula is the approved approximation, not a reconstruction of the original game's scoring code. The game leaves scoring explanations for players to discover through white, outlined point popups.

The player starts with three energy points. The bar displays current/max health at a fixed width. Damage grants 1.2 seconds of invulnerability, with alpha alternating between 1 and 0.5 every 120 ms. Disabling blinking uses steady transparency instead. The enemy destruction particles are unchanged. Session best score and preferences survive restarting, but are not saved between Editor sessions.

## Project structure

- `Assets/SpaceAttack/Scenes/Game.unity`: complete authored hierarchy, UI, player and 41 formation slots.
- `Assets/SpaceAttack/Prefabs`: enemies, projectiles and decorative sparks made of Unity cube primitives.
- `Assets/SpaceAttack/Settings`: Game, Player, three Enemy, Weapon, Wave, Scoring and Feedback assets.
- `Assets/SpaceAttack/Scripts/Gameplay`: session, physics, formation, one-hit deaths and scoring.
- `Assets/SpaceAttack/Scripts/Presentation`: UI, camera framing, score popups and optional feedback.
- `Assets/SpaceAttack/Tests`: Edit Mode scoring/health checks and Play Mode integration tests.

`GameConfig` references the smaller config assets. Current score, health, cooldowns and stage state live in components. The settings menu pauses movement, shooting, wave progression and invulnerability timers. Eight sliders control shake, ship hit flash, screen flash, particles, star movement, invulnerability blinking, master volume and sound-effects volume. Changes are runtime preferences; they never edit shared assets. Screen-wide flashes default to zero. F remains a quick reduced-effects preset and M mutes audio.

Key tuning defaults: `Waves.asset` sets formation offsets to -2.5/+2.5 and speed to 0.4 units/s multiplied by capped stage difficulty. Enemy configs allow turns after 0.8 seconds, checking every 0.4 seconds with a 6% chance that grows by 6 percentage points per second up to 40%. `EnemyWeapon.asset` sets a six-second interval, projectile speed 4.8, damage 1 and lifetime four seconds. `Feedback.asset` contains blink timing/alpha and effect/audio defaults.

All four sounds are synthesized in C# and played through one shared 2D AudioSource in `FeedbackController`. Player and enemy laser tones differ. Shared gain defaults to 0.9 master × 0.8 effects = 0.72; individual waveform amplitudes are balanced in the generator code.

Scene, prefab, material and config YAML were authored directly with stable `.meta` GUIDs. `tools/author_assets.py --output TestResults/AuthoringBatch` stages generated files for review. **Do not rerun it directly over Inspector edits without reviewing changes**: it rewrites authored assets and minimal project settings. The scene is not assembled by a runtime bootstrap script. `tools/write_script_metas.py` only supplies stable metadata for new source files.

## Verification and submission records

Run tests through **Window > General > Test Runner**. **Space Attack > Validate Scene** checks the formation, references and materials. Raw test reports and rendered review screens are under `TestResults/`; those generated outputs are ignored by Git. The submitted verification notes live in `docs/verification.txt`.

Your prompts are preserved in `docs/prompts/`. **Your Clockify records are the source of truth for your active time**. Assistant runtimes are not counted against your time. Personal play-testing and the reflection in your own words remain for you to complete. Movement, scoring and firing use the agreed approximations, including timed enemy fire instead of immediate replacement of offscreen bullets.
