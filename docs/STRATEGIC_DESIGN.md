# EchoFactory — strategic layer v0.2

## Direction
EchoFactory is a turn-based factory management game. The factory simulation is a subsystem entered from the strategic map; real-time walking is not the primary game loop.

## Core loop
Main Menu → Strategic Map → choose parcel / enter facility → Planning → End Turn → Resolution → Turn Summary → next turn.

## Starter economy
The player begins with 10,000 Credits, one owned parcel and one basic Production Hall. The starter hall is deliberately useful immediately and generates **300 Credits passive income per turn**. Production can add additional income, while operating costs prevent passive income from making the economy completely automatic.

The starter hall is the economic foundation, not the final factory. Expansion should require reinvesting profits into land, facilities, machines and research.

## Facilities
- Production Hall — manufacturing; starter facility.
- Logistics Hall — storage, transport and later logistics automation.
- Energy Hall — power generation / processing.
- Workshop — maintenance and machine upgrades.
- Research Hall — paid unlock for the technology tree.

The Research Hall currently costs **7,500 Credits** and can only be built on an owned parcel. These are prototype balance values.

## R&D / technology progression
The Research Hall is the gate to technology progression. Buying the building does not unlock everything immediately; technologies must be researched individually and obey prerequisites.

Current prototype technology tree:

```text
                    Basic Automation
                   /       |        \
                  /        |         \
        Improved Press  Smart Logistics  Energy Efficiency
              |
       Advanced Press
              \
          Advanced Materials
```

Prototype research costs:
- Basic Automation — 2,500
- Improved Press — 3,500
- Advanced Press — 4,500
- Smart Logistics — 5,500
- Energy Efficiency — 6,500
- Advanced Materials — 7,500

The tree is intentionally small. Later technologies should unlock **new machines, machine upgrades, new hall categories, production chains and automation**, rather than being a collection of flat percentage bonuses.

## Intended progression
A typical early game should feel like:

```text
Starter Hall
   ↓
Passive income + simple production
   ↓
Buy first additional parcel
   ↓
Build / expand production
   ↓
Save for Research Hall
   ↓
Research first technology
   ↓
Unlock better machine / upgrade
   ↓
Increase production efficiency
   ↓
Unlock new hall category
   ↓
New resources + production chains
   ↓
Expand the factory network
```

## Design rule: every unlock should create a new decision
Avoid technology that only says “+5%”. Prefer unlocks such as:
- a new press that changes throughput vs. power consumption;
- a larger buffer that changes factory layout;
- a logistics system that reduces transport overhead;
- an energy technology that enables high-power machines;
- a new hall that creates a new production chain.

Flat bonuses can exist, but should support a meaningful mechanical unlock.

## Parcels
Each parcel has price, size, primary resource, resource richness, logistics bonus and ownership. Different map seeds produce different resource distributions. Parcels should eventually create strategic specialization: cheap land, rich deposits, logistics hubs and large expansion sites should all have different consequences.

## Turn resolution
Production, passive income, operating costs and other economic effects resolve when the player ends the turn. No offline income is required for the core loop.

## Visual direction
Use a lighter, readable industrial-management presentation rather than the dark prototype palette. Temporary geometry is acceptable until the systems are proven; final models can be introduced later without changing the Core state model.

## Scope discipline
The immediate playable target remains:
1. Main Menu
2. Strategic Map
3. Parcel selection
4. Facility entry
5. Planning
6. End Turn
7. Turn Summary
8. Return to Map

After this is validated in Unity, connect the existing Echo factory simulation to the Production Hall and then expand R&D, machines, contracts, events and additional production chains.
