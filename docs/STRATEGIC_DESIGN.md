# EchoFactory — strategic layer v0.1

## Direction

EchoFactory becomes a turn-based factory management game. The factory simulation remains a subsystem entered from the strategic map; real-time walking is no longer the primary game loop.

## Core loop

Main Menu → Strategic Map → choose parcel / enter facility → planning → end turn → simulation → turn summary → next turn.

The player makes structural decisions during Planning. Ending a turn resolves production, consumption, operating costs and income. No offline income is required for the core loop.

## Strategic map

The first prototype contains nine deterministic parcels generated from a seed. Each parcel has:

- purchase price,
- size,
- one primary resource,
- resource richness,
- logistics bonus,
- ownership state.

The starter game owns parcel 0 and contains one level-1 Production Hall with two machine slots. Starting credits: 10,000.

Resource types currently defined: Steel, Energy, Bitumen, Scrap and Electronics.

These values are foundation data, not final balance.

## Facilities

Facility categories reserved by the model:

- Production Hall — manufacturing;
- Logistics Hall — storage and movement;
- Energy Hall — power generation / processing;
- Workshop — maintenance and machine upgrades;
- Research Hall — technology progression.

Not all categories need to be available at the beginning of the game. Unlocking should create meaningful progression rather than requiring the player to build everything.

## Turn resolution v0.1

A Production Hall generates a small number of production cycles from its level and machine slots. Each cycle consumes one unit of Steel and produces one product unit. Product value and operating costs are deliberately simple placeholders for the first playable loop.

The current resolver records income, costs and production in the turn summary, then advances to the next Planning phase.

## UX direction

The strategic layer should use a light, readable industrial management presentation. Avoid the dark, visually heavy prototype treatment. Final 3D models and art can replace temporary geometry without changing the Core state model.

## Scope discipline

Do not add workers, research trees, contracts, random events, complex supply chains or additional resource transformations until the following loop is playable in Unity:

1. Main Menu
2. Strategic Map
3. Parcel selection
4. Facility entry
5. Planning
6. End Turn
7. Turn Summary
8. Return to Map

The existing Echo logistics and factory simulation remain valuable, but they should be connected to this strategic layer after the turn loop is validated.
