Project M

A third-person action-survival game focused on responsive melee combat and a world that adapts to the player.

Status: Active development — early playable prototypeEngine: Unity 6Language: C#


Overview

Project M is an independent third-person action-survival game inspired by the satisfying exploration and progression loop of games such as Valheim, while building its own identity around adaptation, mutation, and environmental change.

The world exists between three conditions: Balanced, Blossoming, and Decaying. The player's performance and behaviour influence how the world evolves, affecting its presentation, encounters, and eventually the challenges generated in each region.

The project is being developed as both a long-term commercial game concept and a gameplay programming / technical design portfolio piece.

Design Pillars

Simple, skill-based combat — readable attacks, deliberate timing, and satisfying defensive options.

A reactive world — the game responds to how the player fights, survives, and explores.

Meaningful exploration — camps, encounters, and future mini-dungeons contribute to regional progression.

Focused survival systems — survival mechanics support preparation and exploration without becoming constant chores.

Implemented Systems

Third-person player movement and free-look camera

Basic melee combat and player health

Enemy finite-state machine with Idle, Chase, and Attack states

Enemy health and death handling

Enemy camp detection and automatic camp-clear tracking

Boss arena access tied to regional objectives

Boss behaviour that responds to world state and camp progression

Player performance tracking, including kills, damage taken, and survival time

Rule-based world adaptation with Balanced, Blossoming, and Decaying states

Environmental visual changes based on the current world state

Prototype objective flow and demo completion tracking

Adaptive Game Design

The current prototype uses a transparent, rule-based adaptation system rather than machine learning.

Player performance is observed during play and translated into a world state. That state can then influence environmental visuals, encounter behaviour, and boss configuration. The long-term goal is to expand this into separate Combat, Survival, and Exploration profiles, allowing regions and mini-dungeons to respond to different play styles without relying on a single difficulty score.

This system is designed to make adaptation understandable, testable, and useful to gameplay—not simply to make enemies stronger when the player performs well.

Development Roadmap

Combat foundation

Light and heavy attacks

Shield blocking and timing-based parry

Dodge roll with invulnerability frames

Hit reactions, impact feedback, and combat polish

Stamina and equipment integration

World and progression

Regional combat, survival, and exploration profiles

Adaptive mini-dungeon generation

Expanded camps and encounter variety

Boss progression and biome transitions

Loot, crafting, and equipment progression

Survival and identity

Lightweight building and crafting stations

A useful and comfortable player base

Rest and preparation benefits with a distinct identity

Stronger visual language for Balanced, Blossoming, and Decaying regions

Technical Focus

The codebase is being developed with an emphasis on:

Clear separation between gameplay data, game state, and presentation

Reusable systems instead of one-off scripts for every interaction

Inspector-driven configuration for fast iteration

Explicit state transitions and useful debugging output

Systems that can grow from prototype to production without unnecessary complexity

Getting Started

Clone the repository.

Add the project through Unity Hub.

Open it using the Unity editor version recorded in ProjectSettings/ProjectVersion.txt.

Allow Unity to restore packages and import the project.

Some third-party art, audio, or marketplace assets may be excluded from the public repository because of their licences. A playable build will be linked here when the next portfolio milestone is ready.

Project Scope

Project M is currently a solo-developed work in progress. Features shown in the roadmap describe the intended direction and may change as systems are prototyped and playtested.

Credits and Third-Party Assets

The project may use properly licensed third-party assets during development so that production can remain focused on gameplay. All such assets remain the property of their respective creators and will be credited according to their licence terms.

Key visual assets and identity-defining elements are intended to be replaced or substantially customised as development progresses.

Licence

Unless explicitly stated otherwise, the original source code in this repository is shared for portfolio and evaluation purposes. Third-party assets are governed by their respective licences and are not covered by any licence applied to the original code.

Contact

Panagiotis NikosGameplay Programmer / Technical Designer
