# EchoFactory — strategic layer v0.3

## Direction
EchoFactory is a turn-based factory management game. The factory simulation is a subsystem entered from the strategic map; real-time walking is not the primary game loop.

## Core loop
Main Menu → Strategic Map → choose parcel / enter facility → Planning → End Turn → Resolution → Turn Summary → next turn.

## Core design principle: systems, not bonus stacking
EchoFactory should not feel like a mobile game where every unlock is simply `+10% income`. The player should construct a production system whose parts interact. A strategy can work extremely well, work poorly, or fail completely depending on how the player combines land, facilities, machines, resources and research.

A good decision should introduce a new possibility and a new constraint at the same time.

Examples:
- a faster press consumes more energy and creates more scrap;
- a recycler turns scrap back into steel, making waste part of a deliberate production loop;
- an Energy Hall can make a high-power factory viable, but consumes space and investment;
- a logistics-focused parcel can compensate for a physically inefficient production layout;
- an energy-efficiency technology changes the resource requirements of an improved press rather than merely adding a percentage bonus.

The target loop is:

```text
Decision → consequence → interaction → bottleneck → adaptation
```

## Starter economy
The player begins with 10,000 Credits, one owned parcel and one basic Production Hall. The starter hall generates **300 Credits passive income per turn** and contains one Basic Press. The press converts Steel + Energy into Finished Goods and Scrap. This makes the starter factory useful immediately without making passive income the entire game.

Prototype values are intentionally small and subject to balancing.

## Production model
Machines are explicit state objects placed into facility machine slots. A machine has a type, level and enabled state. Production is resolved per turn through recipes and resource requirements.

Current prototype machine roles:

| Machine | Inputs | Outputs | Strategic purpose |
|---|---|---|---|
| Basic Press | 1 Steel + 1 Energy | 1 Finished Good + 1 Scrap | Simple starting production |
| Improved Press | 1 Steel + 1–2 Energy | 2 Finished Goods + 1 Scrap | Higher output, benefits strongly from Energy Efficiency |
| High-Speed Press | 2 Steel + 3 Energy | 3+ Finished Goods + 2 Scrap | High-volume strategy with energy/scrap pressure |
| Recycler | 2 Scrap + 1 Energy | 1–2 Steel | Closes the steel/scrap loop |
| Generator | 2 Bitumen | 3–5 Energy | Enables energy-intensive factories |
| Electronics Assembler | Electronics + Steel + Energy | Finished Goods | Late-game diversified production |

This is intentionally a **chain model**, not a percentage model. A player can build around energy generation, scrap recovery, logistics or raw-material access, and those choices can reinforce or undermine one another.

## R&D / technology progression
The Research Hall is the gate to technology progression. It currently costs **7,500 Credits** and must be built on an owned parcel.

Technologies should unlock mechanics, machines, production chains or strategic options. Flat bonuses are allowed only when they support a meaningful mechanical change.

Current prototype tree:

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

Research costs:
- Basic Automation — 2,500
- Improved Press — 3,500
- Advanced Press — 4,500
- Smart Logistics — 5,500
- Energy Efficiency — 6,500
- Advanced Materials — 7,500

The exact tree is provisional. The important rule is that researching something should change what the player can **build or combine**, not merely make the same factory produce a larger number.

## Example strategies
These are intended as possible player-created strategies, not fixed optimal builds.

### High-volume factory
```text
Generator → cheap/stable Energy
                 ↓
          High-Speed Press
             ↓       ↓
       Finished     Scrap
                     ↓
                  Recycler
                     ↓
                   Steel
                     └────→ Press
```

### Efficient factory
```text
Energy Efficiency
        ↓
 Improved Press
        ↓
 more output per unit of energy
        ↓
 less pressure on Generator capacity
```

### Logistics-oriented factory
A player can select land with a logistics advantage and use Smart Logistics to make distributed facilities more practical. The benefit should come from changing transport/capacity decisions, not from a permanent income multiplier.

## Failure is allowed
A bad factory should be capable of becoming unprofitable. The game should expose the reason: missing energy, insufficient steel, scrap bottleneck, poor logistics, insufficient machine capacity, etc. The player then decides whether to redesign, buy land, build another facility or research a different technology.

The goal is for the player to think:

> “My factory has a bottleneck. How do I redesign it?”

rather than:

> “Which building gives me the biggest income bonus?”

## Intended progression
```text
Starter Hall
   ↓
Passive income + simple production
   ↓
Buy land / add machine
   ↓
Discover the first bottleneck
   ↓
Invest in a Research Hall
   ↓
Choose a technology direction
   ↓
Unlock a different production option
   ↓
Create a resource chain / synergy
   ↓
Expand the factory network
   ↓
Handle more complex bottlenecks
```

## Parcels
Each parcel has price, size, primary resource, resource richness, logistics bonus and ownership. Different map seeds produce different distributions. Parcels should eventually create specialization: cheap land, rich deposits, logistics hubs and large expansion sites should have different consequences.

## Turn resolution
Production, passive income, operating costs and resource changes resolve when the player ends the turn. The turn summary should explain **what happened to the factory**, not just show a single profit number.

## Visual direction
Use a lighter, readable industrial-management presentation rather than the dark prototype palette. Temporary geometry is acceptable until the systems are proven; final models can be introduced later without changing the Core state model.

## Scope discipline
Immediate playable target:
1. Main Menu
2. Strategic Map
3. Parcel selection
4. Facility entry
5. Planning
6. End Turn
7. Turn Summary
8. Return to Map

The next implementation layer should expose the new machine/production model in Unity UI. After that, connect the existing Echo factory simulation to Production Hall execution and expand facilities, contracts, events and production chains without reverting to flat percentage progression.
