#pragma once
#include <unordered_map>
#include <string>
#include <cstdint>

// Use 64‑bit keys to hold large spell IDs safely
inline const std::unordered_map<uint64_t, std::string> spellNames = {
    { 2800344086ULL, "Paralyze" },
    { 56ULL,         "Slow"     },
    { 258ULL,        "Silence"  }
    // add more IDs from resources/spells.lua
};
