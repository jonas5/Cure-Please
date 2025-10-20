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
inline std::unordered_map<uint16_t, Spell> spells = {
    // Corrected IDs based on logs
    {11, {11, "Protect II", "Protection II", 1, 21, 3, 5, 1, 1, 1, 1, 21}},
    {27, {27, "Regen", "Récup", 1, 15, 3, 10, 1, 1, 1, 1, 10}},

    // Original Cure spells (assuming their IDs might be different now, but keeping them as a base)
    {1, {1, "Cure", "Guérison", 1, 8, 2, 5, 1, 1, 1, 1, 3}},
    {2, {2, "Cure II", "Guérison II", 1, 24, 3, 10, 1, 1, 1, 1, 12}},
    {3, {3, "Cure III", "Guérison III", 1, 46, 4, 15, 1, 1, 1, 1, 22}},
    {4, {4, "Cure IV", "Guérison IV", 1, 88, 5, 20, 1, 1, 1, 1, 41}},
    {5, {5, "Cure V", "Guérison V", 1, 125, 6, 25, 1, 1, 1, 1, 65}},
    {6, {6, "Cure VI", "Guérison VI", 1, 227, 7, 30, 1, 1, 1, 1, 85}},
    {7, {7, "Curaga", "Guérison Générale", 1, 60, 4, 20, 1, 1, 2, 1, 18}},
    {8, {8, "Curaga II", "Guérison Générale II", 1, 120, 5, 30, 1, 1, 2, 1, 38}},
    {9, {9, "Curaga III", "Guérison Générale III", 1, 180, 6, 40, 1, 1, 2, 1, 58}},
    {10, {10, "Curaga IV", "Guérison Générale IV", 1, 260, 7, 50, 1, 1, 2, 1, 78}},
    // Curaga V was misidentified as ID 11, true ID is unknown for now.
};
