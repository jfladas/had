# Hearts across Dimensions — Project Review & Roadmap to v1.0

## Context
Unity 6 (6000.6.7f1), portrait visual novel / dating sim, built for HSLU MEN3–5 (Lukas Bucher &
Angelika Gort). Alpha state: Aleph + Scarlet complete (3 endings each), Mr. Horse ch. 7–13, Ash &
Ethan unwritten. Goal: resume work at hobby pace, finish Mr. Horse and publish a first complete v1.0
on Android / iOS / Web (no fixed date). Review written 2026-10-31; this file is the full review +
the agreed work plan.

---

## 1. How it works
- **Two Unity scenes**: `StartScene` (name entry + "continue" logic, `StartController`) and
  `MainScene` (everything else, `GameController`).
- **Story = graph of ScriptableObjects** in `Assets/Story/Scenes/{0-6,Aleph,Scarlet,MrHorse}` (~440),
  base type `GameScene`:
  - `StoryScene`: sentences (text, alt text for unnamed player "Me", speaker, sprite actions
    SHOW/MOVE/HIDE/NONE, music, sound) + background + `nextScene`.
  - `ChooseScene`: labels → nextScene. Also reused for **menu, log, gallery, chapter select**.
  - `ChapterScene`: title card + checkpoint; the asset name doubles as the PlayerPrefs key. Special
    names drive behaviour (`Home`, `Delete`, `TheEnd`, `Start`, `*Fade*`, `*_P` = energy check).
  - `MinigameScene`: "dimensional axis" timing game, levels 0–7, energy score per route.
- **Routing by naming convention**: the asset's first letter is the route (`S…`, `H…`, else Aleph).
- **Endings**: choices never affect the outcome. One energy check (`A14_P` / `S15_P`, threshold
  6000) decides bad ending vs. good; the last choice picks Ending 2 or 3.
- **Persistence**: PlayerPrefs only (chapter flags + timestamps, per-route score, minigame flags,
  log JSON, name).
- **Content pipeline**: plain-text scripts in `Assets/Story/` (`Speaker:`, `(t):`,
  `CHOICE OPTION n:`, `-MINIGAME-`, …) → `tools/ImportMrHorseRoute.ps1` → hand polish → Scarlet
  audit/clean scripts + `Assets/Editor/*` validators.

## 2. What works well
- A clean, data-driven story graph. No broken references and no dead ends across ~440 assets; only
  4 unreachable scenes (`0_X1*`, `H11_3`). Writers can build branches without code.
- Good feel: typewriter text, crossfading backgrounds and sprites, music crossfades, name entry with
  `{playerName}` + alt text, dialogue log, gallery with locked CGs, chapter select, a "skip demo" entry.
- The minigame is a distinctive hook that is tied into the lore (energy / Sustainer) and themed per route.
- Writing: a consistent, funny, self-deprecating MC voice; distinct supporting casts (James, Ray/Seth/Lu);
  Scarlet is the best-built route (slow burn, the scar setup in `S8_4` pays off in `S12_8`); Mr. Horse has
  the strongest comedic identity (the touch-aversion arc, the "*NEEIGH*" beat).
- Art coverage is strong for the 3 main routes (Scarlet 27 expressions, A1–6/S1–6/H1–3 CGs, cards
  c7–c15 per route; `c14h`, `c15h`, `eh` already drawn for Horse).

## 3. Story review (short)
| Route | State | Strengths | Weaknesses |
|---|---|---|---|
| Prologue 0–6 | complete | mystery build-up, one character per chapter | US/UK spelling drift; Ash's name used even if never learned (`1_X2_1` → `6_3`); Mr. Horse pickable with no "unfinished" warning |
| Aleph | complete | the 3-questions gambit (`A12_9`), betrayal + his POV (`A13_8`), TV confession payoff (`A14_3/7`) | romance rushed (confession in ch. 11); final boss beaten in ~2 lines (`A15_1`); exposition dumps (`A7_3/4`, `A9_5`) |
| Scarlet | complete | slow-burn enemies-to-lovers, vampire reveal, blood scene, both good endings | villain monologue (`S14_1`); weapon conjuring from nowhere; `S15_F1` repeats 28 lines of `S15_2` |
| Mr. Horse | ch. 7–13 → `TBC` | voice, humour, touch arc, `H12_1` | `H10_1` filler; ch. 11 long with little plot; ch. 12 summarises instead of showing; passive MC; contraction slips |
| Ash / Ethan | none | base sprites, cameos, minigame circles prepared | everything else |

Cross-cutting: **nearly all choices are cosmetic** (they rejoin within 1–2 scenes; only CG unlocks and the
final ending pick matter); POV switches are unmarked; tenses are mixed; ~100 typos in total; wrong
speakers (narration attributed to "Me"/Ethan/Aleph); abrupt gore spikes in a light rom-com;
continuity slips (Rosa/Rose, spring→summer, Lu's age, "pitch black" scenes on `white.png`).

## 4. Code / engine issues

### Bugs (fix first)
1. **ScriptableObjects mutated at runtime**: `GameController.DisplayChapterScene` sets
   `nextScene = failScene`, so after one failed check the bad ending sticks for the whole session (and
   is written into the asset in the Editor). `StartController.StartGame` writes the player name into
   the `Me` asset.
2. *(Intended, keep as is: replayed minigames never add points, so a failed run can't be fixed by
   replaying.)*
3. **Resume position saved every tap but never loaded** (`ScoreManager.SaveGameState` +
   `PlayerPrefs.Save()` per tap; nothing reads `GetCurrentSceneName`). Continue always restarts at
   the chapter card, and the per-tap writes are expensive on WebGL.
4. Null-character sprite action in `H8_X1_1` can throw in `BottomBarController.ActCharacter`.
5. Typewriter appends one char at a time, so TMP rich-text tags flash raw; it also throws on empty text.
6. Click-through: global `Input.GetMouseButtonDown` advances the dialogue / fires the minigame even
   when tapping the menu button.
7. The log is capped at ~10 lines and dedupes globally (repeated lines like "…" vanish).
8. Minigame: the tutorial says "triple" (300) but play gives 200; the special-circle sprite is chosen from
   the last checkpoint, not the minigame's route; `Chapter0` key is missing from the reset list.
9. Route detection by first letter is fragile: `Home`/`Start` already start with H/S. (Ash will use
   prefix **C**, Ethan presumably **E**, so there are no collisions, but each new letter still has to be
   added in ~6 places today.)

### Refactors (to make adding Horse / Ash / Ethan cheap)
- **`Route` ScriptableObject** (key, name, chapters, epilogues, gallery entries, energy threshold,
  minigame sprite, chapter-select scene). This replaces the hard-coded A/S/H in `ProgressKeys`,
  `ScoreManager.RouteKeys`, `ChooseController.GetChapterKeyFromText`, `StartController` (40 fields +
  `BuildLegacyContinueCheckpoints`), `GameController` gallery (15 fields + a 15-case switch) and
  `MinigameController.GetSpecialCircle2SpriteForCurrentRoute`.
- Put a `kind` enum on `ChapterScene`/`ChooseScene` instead of magic names; give each `GameScene` an
  explicit `route` field instead of the first letter.
- Merge `SwitchScene` / `SwitchSceneFromSaved`; add one "tap-or-timeout" helper; move minigame
  difficulty into `MinigameScene`; delete dead code (level-based score API, unused fields/methods).
- *(Decided: no story flags / affection for v1.0. Endings stay minigame-driven; only the sticky-fail
  bug gets fixed. No retry, by design.)*
- A single versioned `SaveData` JSON instead of scattered PlayerPrefs, with one write per checkpoint.

## 5. Platform / build issues
**Store blockers**
- Template bundle ID `com.DefaultCompany.com.unity.template.mobile2D`; product name `HaD (Alpha)`.
- iOS: build number 0, no team/signing, icon 1034×1035 (needs 1024×1024, no alpha).
- Android: no upload keystore; APK only (Play needs AAB); target SDK unpinned (≥35).
- WebGL: Brotli without decompression fallback.
- No safe-area handling while rendering outside the safe area.

**Size / perf**
- Web build is 53.6 MB data + 6.1 MB wasm.
- Sprites are 138 MB on disk with no platform overrides or atlases; 2048² UI icons; a 2048²
  fully transparent PNG.
- Music: 29 MB of mp3s set to Decompress On Load at 100% quality → use Streaming at ~60%.
- Unused packages: visualscripting, multiplayer.center (+ app-ui define), collab-proxy, timeline,
  adaptiveperformance, test-framework, 3D/XR/terrain modules, `feature.2d` bundle.
- Leftovers: TMP Examples (6 MB), TutorialInfo, `_Recovery`, missing `SampleScene` in the build
  list, "this is so hot!" placeholder in `MainScene.unity`.

**Repo**
- No git LFS; `.git` is 424 MB; `media/` is 120 MB; `had/.utmp/` (Android intermediates) is tracked.

**UX gaps**
- No text speed, auto-advance, skip-read, text size or multiple save slots.
- Localization is blocked because logic depends on label text ("Back", "Chapter 7").

## 6. Content pipeline issues
- The `tools/*.ps1` scripts were one-off migration helpers (Mr. Horse import, Scarlet audits and
  cleanups). **Decided: delete them** (the future story-authoring workflow will probably change; to be
  decided later). The route-specific editor helpers (`RouteAssetTools`, `MrHorseSceneValidator`) can
  go too once a generic validator exists.
- The Twine file only holds an old prototype of ch. 0–3; there is nothing in it to import.
  **Decided: keep it as an archive** (it stays in `twine/` and is not touched).
- → Keep one **generic graph validator** as an editor menu item, useful whatever the workflow:
  unreachable scenes, null speakers/characters in actions, missing alt text for `{playerName}`,
  "Me:" inside text, null backgrounds. Run it before every build.

---

## 7. Roadmap (hobby pace, no fixed dates)

**Decisions made**
- **v1.0 = Aleph + Scarlet + Mr. Horse (complete)**. Ash (prefix **C**) & Ethan appear as "coming
  soon" in `6_X1` and come later as free updates. Launching this year is nice-to-have, not a deadline.
- **Lukas & Angelika write** Mr. Horse ch. 14–15 + 3 epilogues. Claude wires the assets and validates.
  How the text gets into Unity (new workflow) is decided when the text is ready.
- **Endings stay minigame-driven.** No flags or affection, no retry (by design). Only the sticky-fail
  asset mutation is fixed.
- **Accounts**: Apple Developer ✔, final bundle ID ✔, Mac = MacInCloud pay-as-you-go (Sequoia 15.7,
  Xcode 26.2). Web stays on **itch.io**. Still needed: **Google Play account**. New personal accounts
  must run a **closed test with 12+ testers for 14 continuous days** before production access, so start
  it a few weeks before you want to launch (any build works for it; content updates can follow).
- **iOS workflow to keep paid Mac hours low**: export the Xcode project from Unity on Windows (iOS
  Build Support module) → upload it to MacInCloud → only archive, sign and upload to TestFlight there.
  Doing one test run early catches signing and icon issues long before release.

The milestones below are ordered; each is self-contained, so you can pick them up whenever there's time.

**M1 – Cleanup & foundation (code, no gameplay change)**
*Status (2026-10-31): done except git LFS and bug 3 (resume → moved to M5). Not yet verified in the
editor/playtest.*
- Set up git LFS + untrack `had/.utmp`; delete `tools/*.ps1`, TMP Examples, TutorialInfo, `_Recovery`,
  the missing `SampleScene` entry and unused packages; remove dead code (level-based score API, unused
  fields/methods); remove the "this is so hot!" placeholder.
- Fix bugs 1 and 3–8 (the sticky fail via a local variable, not by writing to the asset; player name
  kept in a runtime variable; resume either implemented or the per-tap save removed; typewriter via
  `maxVisibleCharacters`; UI click-through guard; larger log without global dedupe; minigame
  text/score consistency; `Chapter0` added to the reset list).

**M2 – Data-driven routes (makes Horse endings + C/E routes cheap)**
- A `Route` ScriptableObject (key A/S/H/C/E, chapters, epilogues, gallery entries + Image refs,
  threshold, minigame sprite, chapter-select scene) consumed by `StartController`, `ChooseController`,
  `GameController` gallery, `ProgressKeys`, `ScoreManager` and `MinigameController`.
- A `kind` enum on Chapter/Choose scenes replaces the magic names; an explicit route field replaces
  the first letter.
- Merge `SwitchScene`/`SwitchSceneFromSaved`; add a tap-or-timeout helper; move minigame difficulty
  into `MinigameScene`.
- Generic **story validator** menu item; delete `MrHorseSceneValidator`/`RouteAssetTools` once it covers them.

**M3 – Story fixes in existing content**
- Validator hits + known issues: `H11_3` link, `H8_X1_1` null action, wrong/missing speakers
  (`A7_3`, `A14_6`, `H10_4`, …), "Me:" in text, the missing alt text in `S12_X1_2`, unreachable `0_X1*`.
- Typo/consistency pass (~100 typos; Miss/Ms. Moon, Sun Kingdom, US/UK spelling, POV markers,
  the `S15_F1` duplicate, Rosa/Rose, backgrounds that contradict the text).
- "Coming soon" label for Ash/Ethan in `6_X1`; a hint that Mr. Horse is unfinished until it's done.

**M4 – Mr. Horse completion** (when the text is written)
- Ch. 14–15, energy check (`H15_P` + failScene, matching Scarlet), `HEpilogue1–3`, `HChapterSelect3`,
  minigames 5–7, gallery H4–H6; replace `TBC` with `TheEnd`.
- Art: CGs H4–H6, Samantha/Queen sprites, palace/house backgrounds (`c14h`, `c15h`, `eh` exist already).
- Keep the `H12_X1` trait outcome consistent in the text (no flag exists).

**M5 – UX polish & UI refresh**
- **UI refresh** (the current UI is inconsistent and the UX is rough in places):
  - Walk through every screen first (start, dialogue, choices, menu, log, gallery, chapter select,
    minigame HUD, chapter cards) and list concrete pain points together before redesigning.
  - One consistent style: shared button/panel prefabs, one font family (Ibarra; one text object still
    uses LiberationSans), consistent sizes/spacing/colours, and clear pressed/disabled states
    (choice labels have hover only, which does nothing on touch).
  - Navigation: menu, log, gallery and chapter select are currently ChooseScenes inside the story graph.
    Consider real UI panels with a consistent back/close behaviour.
  - Log: make it scrollable (it's capped at ~800 characters because the box can't scroll).
  - Layout: check the canvas reference resolution (1000×2500, match height) on common phone ratios,
    tablets and desktop web; safe area for notches.
  - Feedback: transitions, a tap-to-continue indicator, a clearer minigame HUD/results screen.
  - Re-export UI sprites at the right sizes (2048² icons, a fully transparent 2048² PNG) as part of the refresh.
- Settings: text speed, auto-advance, skip read text, text size. A real resume position (if not
  done in M1).

**M6 – Release prep**
- Texture overrides + sprite atlases, music set to Streaming; WebGL decompression fallback; real
  bundle ID + product name "Hearts across Dimensions", version numbers, iOS icon 1024² without alpha,
  Android keystore + AAB + target API ≥ 35.
- Store listings, screenshots (`media/MEN5`), privacy policy; Play closed test; TestFlight beta.
- Device testing: low-end Android, an iPhone with a notch, Safari/Chrome mobile web; itch.io page update.

**Later (v1.1+)**: Ash (C) and Ethan (E) routes on the M2 foundation.

## 8. Verification
- The editor validator run reports 0 errors over all routes (refs, reachability, null characters, alt text).
- Playthrough checklist: every route × every ending; fail the energy check, then start a new game in the
  same session and pass (proves the sticky-fail fix);
  continue / resume after killing the app mid-scene; Delete Data fully resets chapter select + gallery.
- Builds: Android AAB on a real device; iOS via Xcode on a notch iPhone; WebGL on itch.io in mobile
  Safari + Chrome; check memory and load time; Web data target < 30 MB.
