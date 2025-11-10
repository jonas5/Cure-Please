#pragma once
#include <string>
#include <unordered_map>

struct Spell {
    uint16_t id;
    std::string name;
    uint8_t aoe; // 0 = single-target, 1 = AoE
    int cooldown; // Cooldown in seconds
};

inline std::unordered_map<uint16_t, Spell> spells = {
    // Healing Magic
    {1,   {1,   "Cure", 0, 0}},
    {2,   {2,   "Cure II", 0, 0}},
    {3,   {3,   "Cure III", 0, 0}},
    {4,   {4,   "Cure IV", 0, 0}},
    {5,   {5,   "Cure V", 0, 0}},
    {6,   {6,   "Cure VI", 0, 0}},
    {7,   {7,   "Curaga", 1, 0}},
    {8,   {8,   "Curaga II", 1, 0}},
    {9,   {9,   "Curaga III", 1, 0}},
    {10,  {10,  "Curaga IV", 1, 0}},
    {11,  {11,  "Curaga V", 1, 0}},
    {12,  {12,  "Raise", 0, 0}},
    {13,  {13,  "Raise II", 0, 0}},
    {14,  {14,  "Raise III", 0, 0}},
    {15,  {15,  "Reraise", 0, 0}},
    {16,  {16,  "Reraise II", 0, 0}},
    {17,  {17,  "Reraise III", 0, 0}},

    // Enhancing Magic
    {43,  {43,  "Protect", 0, 300}},
    {44,  {44,  "Protect II", 0, 300}},
    {45,  {45,  "Protect III", 0, 300}},
    {46,  {46,  "Protect IV", 0, 300}},
    {47,  {47,  "Protect V", 0, 300}},
    {48,  {48,  "Shell", 0, 300}},
    {49,  {49,  "Shell II", 0, 300}},
    {50,  {50,  "Shell III", 0, 300}},
    {51,  {51,  "Shell IV", 0, 300}},
    {52,  {52,  "Shell V", 0, 300}},
    {125, {125, "Protectra", 1, 300}},
    {130,  {130,  "Shellra", 1, 300}},
    {53,  {53,  "Blink", 0, 0}},
    {54,  {54,  "Stoneskin", 0, 0}},
    {55,  {55,  "Aquaveil", 0, 0}},
    {56,  {56,  "Slow", 0, 0}},
    {57,  {57,  "Haste", 0, 180}},
    {58,  {58,  "Paralyze", 0, 0}},
    {59,  {59,  "Silence", 0, 0}},
    {60,  {60,  "Phalanx", 0, 300}},
    {108, {108, "Regen", 0, 60}},
    {109, {109, "Regen II", 0, 60}},
    {110, {110, "Regen III", 0, 60}},
    {111, {111, "Regen IV", 0, 60}},
    {112, {112, "Regen V", 0, 60}},
    {882, {882, "Phalanx II", 0, 300}}, // Merit

    // Enfeebling Magic
    {23,  {23,  "Dia", 0, 0}},
    {24,  {24,  "Dia II", 0, 0}},
    {25,  {25,  "Dia III", 0, 0}},
    {59,  {59,  "Bio", 0, 0}},
    {60,  {60,  "Bio II", 0, 0}},
    {61,  {61,  "Bio III", 0, 0}},
    {71,  {71,  "Paralyze", 0, 0}},
    {72,  {72,  "Slow", 0, 0}},
    {73,  {73,  "Silence", 0, 0}},
    {74,  {74,  "Blind", 0, 0}},
    {75,  {75,  "Bind", 0, 0}},
    {76,  {76,  "Sleep", 0, 0}},
    {77,  {77,  "Sleep II", 0, 0}},
    {78,  {78,  "Break", 0, 0}},
    {79,  {79,  "Dispel", 0, 0}},
    {880, {880, "Paralyze II", 0, 0}}, // Merit
    {881, {881, "Bind II", 0, 0}},     // Merit
    {883, {883, "Blind II", 0, 0}},    // Merit

    // Elemental Magic
    {101, {101, "Fire", 0, 0}},
    {102, {102, "Fire II", 0, 0}},
    {103, {103, "Fire III", 0, 0}},
    {104, {104, "Enthunder", 0, 0}},
    {105, {105, "Firaga", 1, 0}},
    {106, {106, "Firaga II", 1, 0}},
    {107, {107, "Firaga III", 1, 0}},
    {111, {111, "Blizzard", 0, 0}},
    {112, {112, "Blizzard II", 0, 0}},
    {113, {113, "Blizzard III", 0, 0}},
    {114, {114, "Blizzard IV", 0, 0}},
    {115, {115, "Blizzaga", 1, 0}},
    {116, {116, "Blizzaga II", 1, 0}},
    {117, {117, "Blizzaga III", 1, 0}},
    {121, {121, "Thunder", 0, 0}},
    {122, {122, "Thunder II", 0, 0}},
    {123, {123, "Thunder III", 0, 0}},
    {124, {124, "Thunder IV", 0, 0}},
    {126, {126, "Thundaga II", 1, 0}},
    {127, {127, "Thundaga III", 1, 0}},
    {144, {144, "Water IV", 0, 0}},
    {150, {150, "Aero IV", 0, 0}},
    {159, {159, "Stone", 0, 0}},
    {171, {171, "Thunder IV", 0, 0}},
    {193, {193, "Thundaga II", 1, 0}},
    {322, {322, "Thundaga III", 1, 0}},
    {136, {136, "Invisible", 0, 0}},
    {137, {137, "Sneak", 0, 0}},
    {249, {249, "Blaze Spikes", 0, 0}},
    {250, {250, "Posion", 0, 0}},
    {253, {253, "Sleep", 0, 0}},
    {254, {254, "Blind", 0, 0}},
    {258, {258, "Bind", 0, 0}},

    // Add more as needed...
};
