# The Mutant Hero Game

**The Mutant Hero Game** is a turn-based deck-building roguelike built with Unity. Choose one of three mutant heroes, build a combat deck, traverse a branching map, and survive three acts filled with enemies, bosses, shops, rest sites, and elite encounters.


<img width="888" height="505" alt="image" src="https://github.com/user-attachments/assets/b0153a22-1223-4868-93c8-13590d3a8f5c" />


## Gameplay

- Choose between three mutant heroes with distinct starting decks and perks.
- Play cards using mana, combine attacks and defensive abilities, and defeat enemies before the end of the turn.
- Build powerful combos, apply status effects, and upgrade or refine cards during a run.
- Navigate a procedurally generated map across three acts.
- Encounter normal enemies, elite enemies, bosses, shops, rest points, and mystery events.
- Earn gold and card rewards, purchase items, remove cards, upgrade equipment, and make strategic choices.
- Save and resume runs from the main menu.

## Heroes

| Hero | Health | Playstyle |
|---|---:|---|
| **C-161005** | 80 | Strength-focused mutant with a monstrous mutated fist. Uses blood-cost abilities and aggressive close-range attacks. |
| **N-160805** | 75 | Toxin specialist that applies poison and other debuffs to control the battlefield. |
| **H-010405** | 75 | Fast experimental subject with razor-sharp claws who relies on rapid attacks, consecutive strikes, and combo chains. |

Each hero starts with 3 mana and a unique starting perk:

- **C-161005:** 10% chance to heal 2 HP after defeating an enemy.
- **N-160805:** 15% chance to deal 5 damage to a random enemy when attacked.
- **H-010405:** 10% chance to gain 2 Combo when playing an Attack card.

## Combat Systems

- Turn-based card play and enemy turns
- Mana regeneration and card draws
- Attack, block, healing, poison, weakness, exposed, armor, armor-break, strength, and combo effects
- Manual and automatic targeting
- Card exhaustion and discard piles
- Card upgrades and combat copies
- Enemy move patterns and boss encounters
- Victory rewards, defeat handling, and run results
- Lucky-wheel rewards and randomized card selections

## Run Progression

A run progresses through multiple scenes:

1. **Menu**
2. **Hero selection**
3. **Story**
4. **Map**
5. **Combat**
6. **Rewards and route selection**
7. **Ending or continued progression**

The run system tracks health, gold, potions, current act, stage, map points, card upgrades, completed encounters, and save checkpoints. Progress can be saved between supported scenes.

## Project Structure

```text
Assets/
├── Data/
│   ├── Acts/
│   ├── Cards/
│   ├── Encounters/
│   ├── Enemies/
│   ├── Heros/
│   └── Perks/
├── Prefabs/
├── Scenes/
├── Scripts/
│   ├── Creators/
│   ├── Data/
│   ├── Effects/
│   ├── Enums/
│   ├── GameActions/
│   ├── General/
│   ├── Models/
│   ├── Systems/
│   ├── UI/
│   └── Views/
└── Resources/
```

Key systems include:

- `BattleSystem` — battle state and victory/defeat flow
- `CardSystem` — hand, draw, discard, exhaust, and card-play behavior
- `ComboSystem` — combo resources and combo-related perks
- `EnemySystem` — enemy setup and enemy actions
- `HeroSystem` — hero state and combat setup
- `RunSystem` — run progression, inventory, rewards, and save state
- `MapGeneratorSystem` — procedural map generation
- `ActionSystem` — queued game actions and reactions

## Requirements

- **Unity 6000.0.84f1**
- A PC capable of running Unity projects
- Visual Studio, JetBrains Rider, or another C#-compatible editor

The project uses Unity's Universal Render Pipeline and the Unity Input System.

## Getting Started

### Open in Unity

1. Clone the repository:

   ```bash
   git clone https://github.com/KleinNguyen/TheMutantHeroGame.git
   cd TheMutantHeroGame
   ```

2. Open **Unity Hub**.
3. Click **Add** and select the cloned project folder.
4. Open the project with **Unity 6000.0.84f1**.
5. Open `Assets/Scenes/MenuScene.unity`.
6. Press **Play** in the Unity Editor.

### Build a Standalone Game

1. In Unity, open **File > Build Profiles** or **File > Build Settings**.
2. Confirm that the scenes listed below are enabled:
   - `MenuScene`
   - `SelectHeroScene`
   - `StoryScene`
   - `MapScene`
   - `InGameScene`
   - `EndingScene`
   - `LibraryScene`
3. Select a target platform.
4. Click **Build** or **Build and Run**.

## Controls

| Action | Input |
|---|---|
| Navigate menus | Mouse |
| Select and play cards | Left mouse button |
| View card or status details | Hover |
| End turn | End Turn button |
| Open settings | Settings button |
| Close panels | Close/Back button |

## Save Data

The game stores run checkpoints and save data through `RunSaveSystem`. Leaving an active run through the settings menu can save the run before returning to the main menu. Save data is removed when a run finishes.

## Testing

Open **Window > General > Test Runner** in Unity to run available Edit Mode or Play Mode tests.

## Technologies

- Unity
- C#
- Unity 2D
- Universal Render Pipeline
- Unity Input System
- TextMesh Pro
- DOTween
- Unity Test Framework

## Contributing

Contributions are welcome. To contribute:

1. Fork the repository.
2. Create a feature branch.
3. Make focused changes.
4. Test the change in Unity.
5. Open a pull request with a clear description of the update.

Please avoid committing generated Unity directories such as `Library/`, `Logs/`, `Temp/`, or local `UserSettings/` unless those files are intentionally required for project configuration.
