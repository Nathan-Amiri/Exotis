Exotis is a competitive online PvP strategy game, emphasizing outside-the-box creativity

Exotis is currently in development. A playable demo is coming soon!

Created in Unity and coded in C#

Check out the game's code [here](https://github.com/Nathan-Amiri/Exotis/tree/main/Assets/Scripts)

Exotis uses Unity's Lobby, Relay, and Netcode for Gameobjects networking systems.

The GameManager class handles lobby connection code.

The in-game logic is handled primarily by three classes, which can only impact each other in one direction: DelegationCore > RelayCore > ExecutionCore.

DelegationCore prompts the player to delegate (choose) an action, handling all relevant ui logic. It then places that action's data into a packet, sending it to RelayCore.

RelayCore is the only class permitted to contain in-game multiplayer logic. When RelayCore receives a packet from DelegationCore, it sends that packet to both players' ExecutionCores.
No communication occurs between players outside of RelayCore packets.

ExecutionCore acts upon the packet(s) it receives, then either requests a new packet from DelegationCore or instructs the player to wait for the enemy player's next packet.

Simple effects, such as item effects (Gem/Spark/Potion effects), are handled by ExecutionCore. Spell and Trait effects are handled by the SpellTraitEffect class--an index of all unique Spell/Trait effects.

Due to the wide variety of unique effects in the game, many of these Spells/Traits have specific logic scattered throughout the Core classes.
Any specific logic is marked with the following comment for searchability: <// *spellOrTraitName>

Each player creates a team before the game starts, consisting of four characters, or 'Elementals.' Players can choose three Spells for each Elemental. Elementals each have a unique ability, or 'Trait.'

An Elemental's placement on the board is called its 'Slot.' The SlotManager class keeps track of each Elemental's slot positions throughout the game.
