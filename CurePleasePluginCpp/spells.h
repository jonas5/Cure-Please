#pragma once
#include <cstdint>
#include <string>
#include <unordered_map>

// Define the Spell struct
struct Spell {
    uint16_t id;
    std::string name;
    std::string french_name;
    uint16_t skill;
    uint16_t mp_cost;
    uint16_t cast_time;
    uint16_t recast_time;
    uint16_t range;
    uint16_t type;
    uint16_t element;
    uint16_t targets;
    uint16_t level_needed;
};

// Create a map to store the spells
std::unordered_map<uint16_t, Spell> spells = {
    {1, {"Cure", "Guerison", 1, 8, 2, 5, 1, 1, 1, 1, 3}},
    {2, {"Cure II", "Guerison II", 1, 24, 3, 10, 1, 1, 1, 1, 12}},
    {3, {"Cure III", "Guerison III", 1, 46, 4, 15, 1, 1, 1, 1, 22}},
    {4, {"Cure IV", "Guerison IV", 1, 88, 5, 20, 1, 1, 1, 1, 41}},
    {5, {"Cure V", "Guerison V", 1, 125, 6, 25, 1, 1, 1, 1, 65}},
    {6, {"Cure VI", "Guerison VI", 1, 227, 7, 30, 1, 1, 1, 1, 85}},
    {7, {"Curaga", "Guerison Generale", 1, 60, 4, 20, 1, 1, 2, 1, 18}},
    {8, {"Curaga II", "Guerison Generale II", 1, 120, 5, 30, 1, 1, 2, 1, 38}},
    {9, {"Curaga III", "Guerison Generale III", 1, 180, 6, 40, 1, 1, 2, 1, 58}},
    {10, {"Curaga IV", "Guerison Generale IV", 1, 260, 7, 50, 1, 1, 2, 1, 78}},
    {11, {"Curaga V", "Guerison Generale V", 1, 380, 8, 60, 1, 1, 2, 1, 98}},
};