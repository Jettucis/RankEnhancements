# RankEnhancements
 Rank Enhancement plugin for [levels_ranks](https://github.com/Pisex/cs2-lvl_ranks) for cases when you don't want to use VIP but want best players to have some benefits for being so good.

# Installation

# How it works/What it does
Plugin fetches player exp (`value` parameter) from [levels_ranks](https://github.com/Pisex/cs2-lvl_ranks) `lvl_base` (or whatever you have named it) table and depending from it's value will provide benefits to them.

Current implementation expects that reward progression is in following order:
- Flashbang
- Smoke
- Grenade
- Molotov/Incendiary
- Armour
- Helmet

```diff
- If you change the order of reward progression (ie. make armour cost less exp than flashbang and etc.) then it's your fault, I've warned you.
```

# TODO
- [ ] Un-spagghetti-fy my code
- [ ] Cleaner config

# Config
Config is created automatically in `addons\counterstrikesharp\configs\plugins` folder, you will have to edit it with relevant information to use plugin fully.

```json
{
  "db_host": "HOST",
  "db_user": "USER",
  "db_pass": "PASSWORD",
  "db_name": "DATABASE",
  "db_port": "3306",
  "rank_table_name": "lvl_base", // default name, use table with player ranks which has value parameter
  "chat_prefix": "[RankEnhancements]",
  "min_players": 4, // default, how many players have to PLAY to get "benefits"
  "give_fleshbang": true, // Whether fleshbang should be given as a benefit
  "points_fleshbang": 10000, // How much exp player needs to get free flashbang
  "give_smoke": true, // Whether smoke should be given as a benefit
  "points_smoke": 15000, // How much exp player needs to get free smoke
  "give_grenade": true, // Whether grenade should be given as a benefit
  "points_grenade": 20000, // How much exp player needs to get free grenade
  "give_fire": true, // Whether molotov/incendiary should be given as a benefit
  "points_fire": 25000, // How much exp player needs to get free molotov/incendiary
  "give_armour": true, // Whether armour should be given as a benefit
  "points_armour": 30000, // How much exp player needs to get free armour
  "give_helmet": true, // Whether helmet should be given as a benefit
  "points_helmet": 35000 // How much exp player needs to get free helmet
}
```
# Commands
Plugin has following commands:
  - `rankaid` (alternative `benefits`) - information about benefits

# Credits
- cs2-LiteVIP: https://github.com/partiusfabaa/cs2-LiteVIP <- Code snippet "yoinked" for first/first half-time round detection