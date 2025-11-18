import re
import sys
from collections import defaultdict

SPELL_ID_MAP = {
    4: "Cure I",
    12: "Cure II",
    17: "Poisona",
    30: "Cure III",
    34: "Cure IV",
    108: "Diaga",
    109: "Dia",
    144: "Protect",
    152: "Regen",
    158: "Refresh",
    156: "Haste",
    160: "Aquaveil",
    148: "Blink",
    168: "Ice Spikes"
}

def read_bits(data: bytes, bit_offset: int, bit_length: int) -> int:
    byte_offset = bit_offset // 8
    bit_in_byte = bit_offset % 8

    num_bytes_to_read = (bit_in_byte + bit_length + 7) // 8

    if byte_offset + num_bytes_to_read > len(data):
        return 0

    val = int.from_bytes(data[byte_offset:byte_offset + num_bytes_to_read], 'little')
    return (val >> bit_in_byte) & ((1 << bit_length) - 1)

def parse_log_file(filename: str):
    correlated_packets = []

    with open(filename, "r") as f:
        lines = f.readlines()

        for i, line in enumerate(lines):
            if line.startswith("PACKET_OUT|001a|"):
                m_spell = re.search(r"spellId=(\d+)", line)
                if not m_spell:
                    continue
                current_spell_id = int(m_spell.group(1))

                for next_line in lines[i+1 : i+20]:
                    if next_line.startswith("PACKET_IN|00ca|"):
                        parts = next_line.strip().split('|')
                        hex_str = parts[2]
                        correlated_packets.append({
                            "spell_id": current_spell_id,
                            "packet_id": 0xCA,
                            "data": bytes.fromhex(hex_str)
                        })
                        break

    print(f"Found {len(correlated_packets)} correlated 0xCA packets to analyze.")
    if not correlated_packets:
        return

    # --- Brute-force analysis ---
    best_results = defaultdict(int)

    for packet in correlated_packets:
        data = packet["data"]
        for bit_offset in range(len(data) * 8):
            for bit_length in range(1, 33):

                found_spell_id = read_bits(data, bit_offset, bit_length)

                if found_spell_id == packet["spell_id"]:
                    best_results[(bit_offset, bit_length)] += 1

    # --- Print the most promising results ---
    print("\n--- Brute-Force Results ---")
    if not best_results:
        print("No matches found.")
        return

    sorted_results = sorted(best_results.items(), key=lambda item: item[1], reverse=True)

    for (bit_offset, bit_length), matches in sorted_results:
        if matches > 0:
            print(f"Offset={bit_offset}, Length={bit_length}  => {matches} matches")

if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Usage: python3 brute_force_0xCA.py <logfile>")
    else:
        parse_log_file(sys.argv[1])
