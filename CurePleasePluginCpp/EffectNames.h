#pragma once
#include <unordered_map>
#include <string>
#include <cstdint>

inline const std::unordered_map<uint32_t, std::string> effectNames = {
    { 2,  "Paralyze" },
    { 19, "Slow"     },
    { 31, "Silence"  }
    // add more IDs from bufftable.lua
};
