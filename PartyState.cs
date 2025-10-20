using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurePlease
{
    public class ActiveBuff
    {
        public int Id { get; set; }
        public DateTime Expiration { get; set; }
    }

    public class PartyMemberState
    {
        public string Name { get; set; }
        public uint ServerId { get; set; }
        public List<ActiveBuff> Buffs { get; set; }

        public PartyMemberState(string name, uint serverId)
        {
            Name = name;
            ServerId = serverId;
            Buffs = new List<ActiveBuff>();
        }
    }

    public class PartyState
    {
        public Dictionary<string, PartyMemberState> Members { get; private set; }

        public PartyState()
        {
            Members = new Dictionary<string, PartyMemberState>();
        }

        public void AddOrUpdateMember(string name, uint serverId)
        {
            if (!Members.ContainsKey(name))
            {
                Members[name] = new PartyMemberState(name, serverId);
            }
            else
            {
                Members[name].ServerId = serverId;
            }
        }

        public void ClearBuffs(string name)
        {
            if (Members.ContainsKey(name))
            {
                Members[name].Buffs.Clear();
            }
        }

        public void UpdateMemberBuffs(string name, List<ActiveBuff> buffs)
        {
            if (Members.ContainsKey(name))
            {
                Members[name].Buffs = buffs;
            }
        }
        public PartyMemberState GetPartyMember(string name)
        {
            if (Members.TryGetValue(name, out PartyMemberState member))
            {
                return member;
            }
            return null;
        }
    }
}
