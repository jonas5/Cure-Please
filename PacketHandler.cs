using System;
using System.Collections.Generic;

namespace CurePlease
{
    /// <summary>
    /// Represents the state of a spell cast.
    /// </summary>
    public enum CastingState
    {
        Finished,
        Interrupted,
        Blocked
    }

    /// <summary>
    /// Provides data for the CastingFinished event.
    /// </summary>
    public class CastingEventArgs : EventArgs
    {
        public CastingState State { get; set; }
    }

    /// <summary>
    /// Provides data for the BuffsChanged event.
    /// </summary>
    public class BuffsChangedEventArgs : EventArgs
    {
        public ushort CharacterId { get; set; }
        public List<ushort> Buffs { get; set; }
    }

    /// <summary>
    /// Handles parsing of incoming FFXI game packets.
    /// </summary>
    public class PacketHandler
    {
        public event EventHandler<CastingEventArgs> CastingFinished;
        public event EventHandler<BuffsChangedEventArgs> BuffsChanged;

        private bool isZoning = false;
        private uint selfServerId = 0;

        /// <summary>
        /// Sets the server ID of the player running the application.
        /// This is used to filter action packets.
        /// </summary>
        /// <param name="id">The player's server ID.</param>
        public void SetPlayerId(uint id)
        {
            this.selfServerId = id;
        }

        /// <summary>
        /// Main entry point for handling raw incoming packets.
        /// </summary>
        /// <param name="id">The packet ID.</param>
        /// <param name="data">The raw packet data.</param>
        public void HandleIncomingPacket(ushort id, byte[] data)
        {
            switch (id)
            {
                case 0xB: // Start zoning
                    isZoning = true;
                    break;
                case 0xA: // Finish zoning
                    isZoning = false;
                    break;
                case 0x28: // Action packet
                    if (!isZoning)
                    {
                        ParseActionPacket(data);
                    }
                    break;
                case 0x076: // Buff packet
                    if (!isZoning)
                    {
                        ParseBuffPacket(data);
                    }
                    break;
            }
        }

        /// <summary>
        /// Parses a 0x28 action packet to determine casting status.
        /// </summary>
        /// <param name="data">The packet data.</param>
        private void ParseActionPacket(byte[] data)
        {
            uint actorId = BitConverter.ToUInt32(data, 5);

            if (actorId != this.selfServerId || this.selfServerId == 0)
            {
                return;
            }

            uint category = BitTools.Unpack(data, 82, 4);

            CastingState? state = null;

            if (category == 4)
            {
                state = CastingState.Finished;
            }
            else if (category == 8)
            {
                uint message = BitTools.Unpack(data, 86, 16);
                if (message == 28787)
                {
                    state = CastingState.Interrupted;
                }
                else if (message == 24931)
                {
                    state = CastingState.Blocked;
                }
            }

            if (state.HasValue)
            {
                CastingFinished?.Invoke(this, new CastingEventArgs { State = state.Value });
            }
        }

        /// <summary>
        /// Parses a 0x076 buff packet to get party member buffs.
        /// </summary>
        /// <param name="data">The packet data.</param>
        private void ParseBuffPacket(byte[] data)
        {
            // Packet contains 5 chunks of 48 bytes, one for each potential party member + self
            for (int k = 0; k < 5; k++)
            {
                int chunkOffset = k * 0x30;

                ushort userId = BitConverter.ToUInt16(data, chunkOffset + 8);

                if (userId == 0) continue;

                var buffs = new List<ushort>();
                for (int i = 1; i <= 32; i++)
                {
                    int lowByteOffset = chunkOffset + 20 + i - 1;
                    byte lowByte = data[lowByteOffset];

                    int highBitsByteOffset = chunkOffset + 12 + ((i - 1) / 4);
                    byte highBitsByte = data[highBitsByteOffset];

                    int power = (i - 1) % 4;
                    int divisor = (int)Math.Pow(4, power);

                    byte highBits = (byte)(((highBitsByte / divisor) % 4));

                    ushort currentBuff = (ushort)(lowByte + (256 * highBits));

                    if (currentBuff != 255 && currentBuff != 0)
                    {
                        buffs.Add(currentBuff);
                    }
                }
                BuffsChanged?.Invoke(this, new BuffsChangedEventArgs { CharacterId = userId, Buffs = buffs });
            }
        }

        /// <summary>
        /// A utility class for unpacking bits from a byte array in Big Endian format.
        /// </summary>
        private static class BitTools
        {
            public static uint Unpack(byte[] data, int startBit, int numBits)
            {
                ulong value = 0;
                int startByte = startBit / 8;
                int endByte = (startBit + numBits - 1) / 8;

                for (int i = startByte; i <= endByte; i++)
                {
                    value = (value << 8) | data[i];
                }

                int endBit = (startBit + numBits) % 8;
                if (endBit != 0)
                {
                    value >>= (8 - endBit);
                }

                ulong mask = (1UL << numBits) - 1;
                return (uint)(value & mask);
            }
        }
    }
}
