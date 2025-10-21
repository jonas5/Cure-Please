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
        public void ResetBuffTimer(string memberName, string buffType)
        {
            if (Members.TryGetValue(memberName, out PartyMemberState member))
            {
                // Find the buff definition to get the duration and all associated IDs
                if (Form1.buff_definitions.TryGetValue(buffType, out BuffInfo buffInfo))
                {
                    // Remove all existing instances of this buff type
                    member.Buffs.RemoveAll(b => buffInfo.Ids.Contains(b.Id));

                    // Add a new instance with a reset timer.
                    // We just use the first ID from the definition as a representative ID for the group.
                    member.Buffs.Add(new ActiveBuff
                    {
                        Id = buffInfo.Ids.First(),
                        Expiration = DateTime.Now.AddSeconds(buffInfo.Duration)
                    });
                }
            }
        }
    }
}
