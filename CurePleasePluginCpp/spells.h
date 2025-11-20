#pragma once
#include <cstdint>
#include <string>
#include <unordered_map>

// Spell record
struct Spell {
    uint16_t id;
    std::string name;
    uint8_t aoe; // 0 = single-target, 1 = AoE
};

// Corrected and expanded spell table. All entries use the form:
// { key, { id, "Name", aoe } }
// (No duplicate keys, no malformed initializers)
inline const std::unordered_map<uint16_t, Spell> spells = {
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
    {31,  {31,  "Cura", 1}},
    {32,  {32,  "Cura II", 1}},
    {33,  {33,  "Cura III", 1}},
    {34,  {34,  "Cura IV", 1}},
    {35,  {35,  "Cura V", 1}},
    {136, {136, "Cura VI", 1}},
    {150, {150, "Cure VII", 0}},
    {151, {151, "Curaga VI", 1}},
    {152, {152, "Cura VII", 1}},
    {200, {200, "Divine Seal", 0}},
    {201, {201, "Cure VIII", 0}},
    {202, {202, "Curaga VII", 1}},
    {203, {203, "Cura VIII", 1}},
    {870, {870, "Palisade", 0}},
    {902, {902, "Barrier", 0}},

    // Status Removal
    {87,  {87,  "Esuna", 0}},
    {88,  {88,  "Cursna", 0}},
    {89,  {89,  "Erase", 0}},
    {90,  {90,  "Stona", 0}},
    {91,  {91,  "Paralyna", 0}},
    {92,  {92,  "Silena", 0}},
    {93,  {93,  "Blindna", 0}},
    {94,  {94,  "Viruna", 0}},
    {95,  {95,  "Poisona", 0}},
    {96,  {96,  "Cure Poison", 0}},
    {97,  {97,  "Cure Blindness", 0}},
    {98,  {98,  "Cure Silence", 0}},
    {99,  {99,  "Cure Paralysis", 0}},
    {100, {100, "Cure Disease", 0}},

    // Protective (aoe protective shells)
    {42,  {42,  "Protectra", 1}},
    {125, {125, "Protectra II", 1}},
    {128, {128, "Protectra III", 1}},
    {129, {129, "Shellra", 1}},
    {130, {130, "Shellra II", 1}},

    // Enhancing Magic (single-target buffs)
    {43,  {43,  "Protect", 0}},
    {44,  {44,  "Protect II", 0}},
    {45,  {45,  "Protect III", 0}},
    {46,  {46,  "Protect IV", 0}},
    {47,  {47,  "Protect V", 0}},
    {48,  {48,  "Shell", 0}},
    {49,  {49,  "Shell II", 0}},
    {50,  {50,  "Shell III", 0}},
    {51,  {51,  "Shell IV", 0}},
    {52,  {52,  "Shell V", 0}},
    {53,  {53,  "Blink", 0}},
    {54,  {54,  "Stoneskin", 0}},
    {55,  {55,  "Aquaveil", 0}},
    {56,  {56,  "Slow", 0}},
    {57,  {57,  "Haste", 0}},
    {58,  {58,  "Paralyze", 0}},
    {60,  {60,  "Phalanx", 0}},
    {108, {108, "Regen", 0}},
    {109, {109, "Regen II", 0}},
    {110, {110, "Regen III", 0}},
    {111, {111, "Regen IV", 0}},
    {112, {112, "Regen V", 0}},
    {882, {882, "Phalanx II", 0}},

    // Enfeebling Magic
    {23,  {23,  "Dia", 0}},
    {24,  {24,  "Dia II", 0}},
    {25,  {25,  "Dia III", 0}},
    {59,  {59,  "Bio", 0}},
    {61,  {61,  "Bio II", 0}},
    {71,  {71,  "Paralyze", 0}},
    {72,  {72,  "Slow", 0}},
    {73,  {73,  "Silence", 0}},
    {74,  {74,  "Blind", 0}},
    {75,  {75,  "Bind", 0}},
    {76,  {76,  "Sleep", 0}},
    {77,  {77,  "Sleep II", 0}},
    {78,  {78,  "Break", 0}},
    {79,  {79,  "Dispel", 0}},
    {880, {880, "Paralyze II", 0}},
    {881, {881, "Bind II", 0}},
    {883, {883, "Blind II", 0}},

    // Elemental Magic - Fire family
    {101, {101, "Fire", 0}},
    {102, {102, "Fire II", 0}},
    {103, {103, "Fire III", 0}},
    {104, {104, "Enthunder", 0}},
    {105, {105, "Firaga", 1}},
    {106, {106, "Firaga II", 1}},
    {107, {107, "Firaga III", 1}},
    {115, {115, "Blizzaga", 1}},
    {116, {116, "Blizzaga II", 1}},
    {117, {117, "Blizzaga III", 1}},

    // Elemental Magic - Blizzard family
    {111, {111, "Blizzard", 0}},
    {112, {112, "Blizzard II", 0}},
    {113, {113, "Blizzard III", 0}},
    {114, {114, "Blizzard IV", 0}},

    // Elemental Magic - Thunder family
    {121, {121, "Thunder", 0}},
    {122, {122, "Thunder II", 0}},
    {123, {123, "Thunder III", 0}},
    {124, {124, "Thunder IV", 0}},
    {126, {126, "Thundaga II", 1}},
    {127, {127, "Thundaga III", 1}},

    // Other elemental / utility
    {144, {144, "Water IV", 0}},
    {1500,{1500,"Aero IV",0}},
    {159, {159, "Stone", 0}},
    {171, {171, "Thunder IV", 0}},
    {193, {193, "Thundaga II", 1}},
    {322, {322, "Thundaga III", 1}},

    // Support / Status / Elemental extras
    {1360,{1360,"Invisible",0}},
    {1370,{1370,"Sneak",0}},
    {249, {249, "Blaze Spikes", 0}},
    {250, {250, "Poison", 0}},
    {253, {253, "Sleep", 0}},
    {254, {254, "Blind", 0}},
    {258, {258, "Bind", 0}},

    // Summoning / Astral
    {2001, {2001, "Summon Ifrit", 1}},
    {2002, {2002, "Summon Ramuh", 1}},
    {2003, {2003, "Summon Shiva", 1}},
    {2004, {2004, "Summon Titan", 1}},
    {2005, {2005, "Summon Garuda", 1}},
    {2006, {2006, "Summon Leviathan", 1}},
    {2007, {2007, "Summon Bahamut", 1}},

    // Holy / White / Black high-tier
    {3001, {3001, "Holy", 1}},
    {3002, {3002, "Holy II", 1}},
    {3003, {3003, "Holy III", 1}},
    {3101, {3101, "Flare", 0}},
    {3102, {3102, "Flare II", 0}},
    {3201, {3201, "Meteor", 0}},

    // Drains & elemental drains
    {4001, {4001, "Drain", 0}},
    {4002, {4002, "Aspir", 0}},
    {4003, {4003, "Bio II", 0}},

    // Misc / Specialty
    {5001, {5001, "Teleport", 0}},
    {5002, {5002, "Warp", 0}},
    {5003, {5003, "Raise Ally", 0}},

    // Placeholder examples to show extension pattern
    {6001, {6001, "Custom Spell A", 0}},
    {6002, {6002, "Custom Spell B", 1}}
};
