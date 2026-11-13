# Card upgrades and large hands

All 90 cards have complete upgrade data. Brace retains its existing upgrade; Tactical Guard now has complete upgraded effects. The other 88 cards receive one main improvement (damage, Block, status, draw, utility, or mana). These values are an initial balance pass and still need playtesting.

Upgrades live on the existing CardData assets, so Library, rewards, shop, and run decks continue using the same asset references. No separate plus-card assets or new list entries are needed.

The hand keeps its original spacing with 1–10 cards and compresses larger hands inside spline positions 0.05–0.95. There is no new hand-size limit.

## Upgrade reference

| Card | Mana before → after | Upgraded effect |
|---|---:|---|
| green/Breach and Venom | 1 → 1 | Deal 10 damage. If the target has Poison, apply 2 Armor Break. |
| green/Clean Sweep | 2 → 1 | Discard the remaining cards in your hand, then draw the same number of cards. Exhaust. |
| green/Contagion | 2 → 1 | Add Poison to every other enemy equal to half of the Poison on the enemy with the most Poison, rounded down. Exhaust. |
| green/Corrosive Mark | 1 → 1 | Apply 3 Armor Break to one enemy. |
| green/Corrosive Scheme | 2 → 2 | Apply 3 Armor Break to all enemies. Enemies that already have Poison receive 1 additional Armor Break. Sly. |
| green/Deepening Toxin | 1 → 1 | Deal 11 damage. If the target has at least 5 Poison, deal 6 additional damage. |
| green/Delayed Payday | 0 → 0 | Next turn, gain 3 additional mana. Exhaust. |
| green/Disposable Defense | 1 → 1 | Gain 15 Block. Exhaust. |
| green/Fortified Trap | 1 → 0 | Double the Block you gain from cards for the rest of this turn. Exhaust. |
| green/Free Follow-Up | 2 → 2 | Deal 15 damage. The next card you play this turn costs 0 mana. |
| green/Grand Disappearance | 1 → 1 | Can only be played with at least 2 other cards in your hand. Discard all of them. Next turn, your Attacks deal double damage. Exhaust. |
| green/Hidden Guard | 1 → 1 | Gain 9 Block. Sly. |
| green/Lethal Dose | 1 → 1 | Apply 7 Poison to the enemy with the lowest HP. |
| green/Light Guard | 1 → 1 | Gain 8 Block. |
| green/Pocketed Double | 1 → 0 | Choose a card in your hand. At the start of your next turn, add 2 temporary copies of it to your hand. Exhaust. |
| green/Poison Detonation | 2 → 2 | Deal 13 damage to all enemies, plus half of each target's current Poison, rounded down. |
| green/Poisoned Knife | 1 → 1 | Deal 3 damage. Apply 4 Poison. |
| green/Quick Recovery | 1 → 1 | Gain 8 Block. Draw 1 card. |
| green/Recovered Technique | 0 → 0 | Return a random card from your Discard Pile to your hand. Exhaust. |
| green/Sleight of Hand | 0 → 0 | Draw 2 cards, then discard 1 card. |
| green/Smuggled Skill | 1 → 1 | Gain 10 Block. Give a card in your hand Sly for this turn. |
| green/Tainted Edge | 1 → 1 | Deal 5 damage. Apply 2 Poison. |
| green/The Long Con | 2 → 2 | Deal 4 damage, plus 1 for each card drawn this combat. Maximum: 30 base damage. |
| green/Toxic Exchange | 1 → 1 | Exhaust a card in your hand to draw 2 cards. Gain 9 Block. |
| green/Toxic Opening | 1 → 1 | Deal 10 damage. If the target has Poison, deal 4 additional damage. |
| green/Toxic Requisition | 1 → 1 | Discard up to 3 cards. Deal 6 damage to one enemy for each card discarded. Exhaust. |
| green/Vanishing Volley | 1 → 1 | Deal 8 damage to all enemies. Sly. |
| green/Venom Cloud | 1 → 1 | Apply 4 Poison to all enemies. |
| green/Venom Ward | 1 → 0 | Gain Block equal to the total Poison on all enemies, up to 30 Block. Exhaust. |
| green/Venomous Shiv | 1 → 1 | Deal 9 damage. |
| red/Adversity's Gift | 1 → 0 | Power: Each turn, the first time you lose HP from a card, gain 1 Strength and 1 Dexterity for that turn. Exhaust. |
| red/Blood Price | 0 → 0 | Lose 2 HP. Gain 1 mana. Exhaust. |
| red/Bloodwall | 1 → 1 | Lose 2 HP. Gain 8 Block. |
| red/Brace (existing) | 1 → 1 | Gain 7 block |
| red/Burn the Hand | 1 → 1 | Exhaust the remaining cards in your hand. Deal 9 damage to one enemy for each card exhausted. Exhaust. |
| red/Collapse | 1 → 1 | Deal 9 damage, plus 3 for each Weak on the target. Maximum bonus: 12 damage. |
| red/Crippling Cut | 1 → 1 | Deal 7 damage. Apply 1 Weak. |
| red/Culling Lesson | 1 → 1 | Exhaust a card of your choice from your hand, then draw 3 cards. |
| red/Desperate Sweep | 1 → 1 | Lose 3 HP. Deal 12 damage to all enemies. |
| red/Dread Mark | 0 → 0 | Apply 3 Weak to one enemy. Exhaust. |
| red/Endless Reserves | 3 → 2 | Gain 1 additional mana at the start of each turn, starting next turn. Exhaust. |
| red/Exploit Weakness | 1 → 1 | Deal 9 damage. If the target has Weak, restore 2 HP. |
| red/Focused Study | 1 → 1 | Draw 4 cards. You cannot draw any more cards this turn. |
| red/Hunt the Hobbled | 1 → 1 | Deal 11 damage. Restore 2 HP for each enemy with Weak, up to 6 HP. |
| red/Iron Counter | 2 → 2 | Gain 14 Block. Deal 12 damage. |
| red/Lasting Guard | 1 → 0 | Keep your Block at the start of your next turn. |
| red/Measured Strike | 1 → 1 | Deal 9 damage. |
| red/Pain-Forged Guard | 1 → 1 | Gain 9 Block. If you have lost HP from a card this turn, gain 4 additional Block. |
| red/Punish the Weak | 1 → 1 | Deal 10 damage. If the target has Weak, deal 4 additional damage. |
| red/Reclaiming Blow | 1 → 1 | Deal 10 damage. Put a card from your Discard Pile on top of your Draw Pile. |
| red/Riposte Stance | 1 → 1 | Gain 7 Block. Deal 5 damage. |
| red/Scorched Guard | 1 → 1 | Gain 12 Block. Exhaust a random card in your hand. |
| red/Second Wind | 1 → 1 | Restore 9 HP. Exhaust. |
| red/Stored Momentum | 1 → 0 | Deal damage equal to your current Block. |
| red/Suppression Wave | 1 → 1 | Deal 8 damage and apply 1 Weak to all enemies. |
| red/Tactical Guard | 1 → 1 | Gain 8 Block. Upgrade a card in your hand for this combat. Exhaust. |
| red/Thorns of Resolve | 2 → 2 | Gain 14 Block. Whenever you are attacked this turn, deal 6 damage to the attacker. |
| red/Triple Verdict | 2 → 2 | Deal 6 damage to all enemies 3 times. |
| red/Vow of Might | 1 → 1 | Lose 3 HP. Gain 3 Strength for this combat. Apply 1 Weak to one enemy. Exhaust. |
| red/Withering Brand | 0 → 0 | Lose 2 HP. Apply 3 Weak to one enemy. |
| yellow/Build Tempo | 0 → 0 | Gain 3 Combo. |
| yellow/Burn the Excess | 0 → 0 | Exhaust up to 2 cards in your hand. Gain 3 Combo for each card exhausted. |
| yellow/Combat Flow | 1 → 1 | Gain 10 Block. If you have played at least one Attack this turn, gain 1 Combo. |
| yellow/Combo Breaker | 1 → 1 | Deal 10 damage. If you have at least 3 Combo, deal 5 additional damage. |
| yellow/Cut Loose | 1 → 1 | Exhaust a card of your choice from your hand. Gain 3 Combo. |
| yellow/Draw on Momentum | 0 → 0 | Spend up to 2 Combo. Draw 2 cards for each Combo spent. |
| yellow/Escalating Rhythm | 1 → 1 | Deal 11 damage. Gain 1 Combo for every 2 Combo you had before playing this card, up to 3 Combo gained. |
| yellow/Exploitative Instinct | 1 → 0 | Each turn, the first time you apply Exposed to an enemy, gain 2 Combo. Exhaust. |
| yellow/Expose the Guard | 1 → 1 | Deal 8 damage. Apply 2 Exposed. |
| yellow/Exposed Flurry | 2 → 2 | Attack 4 times for 5 damage each. If the target has Exposed, each hit deals 2 additional damage. |
| yellow/Final Payoff | 2 → 2 | Spend all Combo. Deal 13 damage, plus 4 for each Combo spent. If this defeats the target, regain 2 Combo. |
| yellow/Flash Cut | 0 → 0 | Deal 11 damage. Exhaust. |
| yellow/Flowing Guard | 1 → 1 | Gain 10 Block. If you have at least 3 Combo, draw 1 card. |
| yellow/Follow-Through | 1 → 1 | Deal 7 damage. If you have at least 3 Combo, attack again for 7 damage. |
| yellow/Fourfold Flourish | 1 → 1 | Attack 3 times for 5 damage each. Exhaust a card of your choice from your hand. |
| yellow/Heavy Strike | 1 → 1 | Deal 9 damage. |
| yellow/Marked Opening | 1 → 1 | Deal 10 damage. If the target has Exposed, gain 2 Combo. |
| yellow/Marking Thrust | 1 → 1 | Deal 10 damage. Apply 1 Exposed. |
| yellow/Measured Rhythm | 1 → 1 | Deal 8 damage, plus 1 for each Combo. Do not spend Combo. |
| yellow/Perfect Strike | 2 → 2 | Deal 18 damage. Exhaust. |
| yellow/Piercing Opening | 1 → 1 | Deal 8 damage. If the target has Exposed, add 5 base damage. |
| yellow/Press the Advantage | 1 → 1 | Deal 9 damage. Gain 1 Combo. |
| yellow/Read the Opening | 1 → 1 | Gain 3 Combo. If the target has Exposed, draw 1 card. |
| yellow/Relentless Sequence | 1 → 1 | Each turn, after playing your fourth Attack, gain 3 Combo and draw 1 card. Exhaust. |
| yellow/Strong Shield | 1 → 1 | Gain 8 Block. |
| yellow/Tactical Pressure | 1 → 1 | Gain 10 Block. If you have played at least 2 Attacks this turn, deal 6 damage to the enemy with the lowest HP. |
| yellow/Tactical Sacrifice | 1 → 1 | Gain 12 Block. Exhaust a card of your choice from your hand. |
| yellow/Three-Beat Rhythm | 1 → 1 | Each turn, after playing your third Attack, gain 3 Combo. Exhaust. |
| yellow/Twin Feint | 1 → 1 | Attack twice for 4 damage each. Gain 1 Combo. |
| yellow/Whirlwind Payoff | 1 → 1 | Spend all Combo. Deal 4 damage to all enemies for each Combo spent. |

## Unity checks

1. Open the project and allow Unity to reimport the changed assets; check the Console for import errors.
2. Open the Library, turn on the upgrade preview, and inspect all three characters.
3. Start a Green or Yellow run, upgrade a card at Rest, and confirm its mana and gameplay effects in the next combat.
4. With two copies of one card, upgrade only one and verify the other is unchanged.
5. Verify Tactical Guard+ still opens the combat upgrade selection after granting Block.
6. Draw or copy enough cards to reach 12 and 20 cards; check positioning, hover, play, discard, and reflow.
7. Rendering and card balance have not been verified in Unity in this environment.
