#pragma once
#include <string>
#include <vector>
#include <map>
#include <unordered_map>

struct Spell {
    uint16_t id;
    std::string name;
    uint8_t aoe; // 0 = single-target, 1 = AoE
};

// Define the maps for debuffs and dispelable buffs
static const std::map<int, std::vector<std::string>> rdm_debuff_map = {
    {23, {"Dia", "Dia II", "Dia III"}},
    {71, {"Paralyze", "Paralyze II"}},
    {72, {"Slow", "Slow II"}},
    {73, {"Silence"}},
    {74, {"Blind", "Blind II"}},
    {75, {"Bind", "Bind II"}},
    {89, {"Gravity", "Gravity II"}},
    {134, {"Bio", "Bio II", "Bio III"}},
    {149, {"Burn"}},
    {151, {"Frost"}},
    {153, {"Shock"}},
    {154, {"Rasp"}},
    {155, {"Choke"}},
    {156, {"Drown"}}
};

// Category: Defense Boost
inline std::unordered_map<uint16_t, std::string> dispel_defense_map = {
    {872, "Harden Shell"}, {873, "Sand Shield"}, {874, "Scutum"}, {547, "Cocoon"},
    {875, "Scissor Guard"}, {876, "Promyvion Barrier"}, {877, "Barrier Tusk"},
    {878, "Arm Block"}, {879, "Shell Guard"}, {880, "Particle Shield"},
    {881, "Amber Scutum"}, {882, "Aura of Persistence"}, {883, "Hexagon Belt"},
    {884, "Parry"}, {885, "Shiko no Mitate"}, {886, "Molluscous Mutation"},
    {887, "Reactor Cool"}
};

// Category: Magic Shield
inline std::unordered_map<uint16_t, std::string> dispel_magic_map = {
    {888, "Magic Barrier"}, {889, "Perfect Defense"}, {890, "Polar Bulwark"},
    {891, "Spectral Barrier"}, {892, "Mind Wall"}, {893, "Discharger"},
    {894, "Bastion of Twilight"}, {895, "Mana Screen"}, {896, "Hydro Blast"},
    {897, "Shadow Lord (Magic Stance)"}, {898, "Immortal Shield"}
};

// Category: Evasion Boost
inline std::unordered_map<uint16_t, std::string> dispel_evasion_map = {
    {899, "Sand Veil"}, {900, "Rhino Guard"}, {901, "Rabid Dance"},
    {902, "Material Fend"}, {903, "Secretion"}, {904, "Warm-Up"},
    {905, "Water Shield"}, {906, "Feather Barrier"}, {907, "Evasion"},
    {908, "Hard Membrane"}, {909, "Sigh"}, {910, "Mirage"},
    {911, "Wind Wall"}
};


inline std::unordered_map<uint16_t, Spell> spells = {
    // Healing Magic
    {1,   {1,   "Cure", 0}},
    {2,   {2,   "Cure II", 0}},
    {3,   {3,   "Cure III", 0}},
    {4,   {4,   "Cure IV", 0}},
    {5,   {5,   "Cure V", 0}},
    {6,   {6,   "Cure VI", 0}},
    {7,   {7,   "Curaga", 1}},
    {8,   {8,   "Curaga II", 1}},
    {9,   {9,   "Curaga III", 1}},
    {10,  {10,  "Curaga IV", 1}},
    {11,  {11,  "Curaga V", 1}},
    {12,  {12,  "Raise", 0}},
    {13,  {13,  "Raise II", 0}},
    {14,  {14,  "Raise III", 0}},
    {15,  {15,  "Reraise", 0}},
    {16,  {16,  "Reraise II", 0}},
    {17,  {17,  "Reraise III", 0}},

    // Enhancing Magic
    {43,  {43,  "Protect", 0}},
    {44,  {44,  "Protect II", 0}},
    {45,  {45,  "Protect III", 0}},
    {46,  {46,  "Protect IV", 0}},
    {47,  {47,  "Protect V", 0}},
    {48,  {48,  "Shell", 0}},
    {49,  {49,  "Shell II", 0}},
    {40,  {50,  "Shell III", 0}},
    {51,  {51,  "Shell IV", 0}},
    {52,  {52,  "Shell V", 0}},
    {125, {125, "Protectra", 1}},
    {130,  {130,  "Shellra", 1}},
    {53,  {53,  "Blink", 0}},
    {54,  {54,  "Stoneskin", 0}},
    {55,  {55,  "Aquaveil", 0}},
    {56,  {56,  "Slow", 0}},
    {57,  {57,  "Haste", 0}},
    {58,  {58,  "Paralyze", 0}},
    {59,  {59,  "Silence", 0}},
    {60,  {60,  "Phalanx", 0}},
    {108, {108, "Regen", 0}},
    {109, {109, "Regen II", 0}},
    {110, {110, "Regen III", 0}},
    {111, {111, "Regen IV", 0}},
    {112, {112, "Regen V", 0}},
    {882, {882, "Phalanx II", 0}}, // Merit

    // Enfeebling Magic
    {23,  {23,  "Dia", 0}},
    {24,  {24,  "Dia II", 0}},
    {25,  {25,  "Dia III", 0}},
    {59,  {59,  "Bio", 0}},
    {60,  {60,  "Bio II", 0}},
    {61,  {61,  "Bio III", 0}},
    {71,  {71,  "Paralyze", 0}},
    {72,  {72,  "Slow", 0}},
    {73,  {73,  "Silence", 0}},
    {74,  {74,  "Blind", 0}},
    {75,  {75,  "Bind", 0}},
    {76,  {76,  "Sleep", 0}},
    {77,  {77,  "Sleep II", 0}},
    {78,  {78,  "Break", 0}},
    {79,  {79,  "Dispel", 0}},
    {880, {880, "Paralyze II", 0}}, // Merit
    {881, {881, "Bind II", 0}},     // Merit
    {883, {883, "Blind II", 0}},    // Merit

    // Elemental Magic
    {101, {101, "Fire", 0}},
    {102, {102, "Fire II", 0}},
    {103, {103, "Fire III", 0}},
    {104, {104, "Enthunder", 0}},
    {105, {105, "Firaga", 1}},
    {106, {106, "Firaga II", 1}},
    {107, {107, "Firaga III", 1}},
    {111, {111, "Blizzard", 0}},
    {112, {112, "Blizzard II", 0}},
    {113, {113, "Blizzard III", 0}},
    {114, {114, "Blizzard IV", 0}},
    {115, {115, "Blizzaga", 1}},
    {116, {116, "Blizzaga II", 1}},
    {117, {117, "Blizzaga III", 1}},
    {121, {121, "Thunder", 0}},
    {122, {122, "Thunder II", 0}},
    {123, {123, "Thunder III", 0}},
    {124, {124, "Thunder IV", 0}},
    {126, {126, "Thundaga II", 1}},
    {127, {127, "Thundaga III", 1}},
    {144, {144, "Water IV", 0}},
    {150, {150, "Aero IV", 0}},
    {159, {159, "Stone", 0}},
    {171, {171, "Thunder IV", 0}},
    {193, {193, "Thundaga II", 1}},
    {322, {322, "Thundaga III", 1}},
    {136, {136, "Invisible", 0}},
    {137, {137, "Sneak", 0}},
    {249, {249, "Blaze Spikes", 0}},
    {250, {250, "Posion", 0}},
    {253, {253, "Sleep", 0}},
    {254, {254, "Blind", 0}},
    {258, {258, "Bind", 0}},

    // Add more as needed...
};
