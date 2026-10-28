# Kirby / 卡比 — Slay the Spire 2 character mod

卡比加入杀戮尖塔 2：吸入敌人，吞下复制能力。
Kirby joins Slay the Spire 2: inhale enemies and copy their abilities.

![preview](art/preview/preview.png)

## 玩法 / How it plays

- **吸入 Inhale**：给予敌人「被吸入」（生命不高于 16 时，它行动前死亡），并根据它的意图决定嘴里的东西。
  Inhaled enemies die before acting at 16 HP or less; what Kirby gets depends on the enemy's intent.
- **吐星星 / 吞咽 Star Spit / Swallow**：把满嘴吐出去造成伤害，或吞下获得复制能力。
  Spit the Mouthful out as damage, or swallow to gain a Copy Ability.
- **复制能力 Copy Abilities**：剑、回旋镖、石头、格斗家、冰、火、睡眠、等离子、汽车、星之杖、万能剑、医生。每种能力都有配套的卡牌，持有能力时这些牌会保留并获得加成。
  Sword, Boomerang, Stone, Fighter, Ice, Fire, Sleep, Plasma, Car, Star Rod, Galaxia and Doctor. Each has its own
  cards that are retained and boosted while you hold it.

The full design (cards, numbers, intent table) is in [DESIGN.md](DESIGN.md) (Chinese).

## 安装 / Install

1. 订阅前置模组 **RitsuLib**（Steam 创意工坊 3747602295）。Subscribe to **RitsuLib** (Workshop 3747602295).
2. 从创意工坊订阅本模组，或把 Release 里的 `KirbyMod` 文件夹放进游戏目录的 `mods/`。
   Subscribe on the Workshop, or put the `KirbyMod` folder from a Release into the game's `mods/` folder.
3. 在游戏的模组列表里启用。Enable it in the in-game mod list.

Built for game version 0.107.1 with RitsuLib 0.6.10.

## 美术 / Art

This repository only contains simple placeholder art. Character, card and relic art are placeholders or base-game
icons until original art is drawn.

## 构建 / Building

Requirements: .NET 9 SDK, the game, RitsuLib from the Workshop, and MegaDot 4.5.1 (the game's Godot build) for
exporting the `.pck`.

1. Edit the paths in `Directory.Build.props`.
2. `dotnet build` compiles and copies the `.dll` to the game's `mods/KirbyMod/`.
3. `dotnet publish -c Debug` also exports `KirbyMod.pck` there.

Kirby is © Nintendo / HAL Laboratory. This is an unofficial fan mod.
