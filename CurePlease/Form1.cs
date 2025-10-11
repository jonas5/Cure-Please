namespace CurePlease
{
    using CurePlease.Properties;
    using EliteMMO.API;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using System.Xml.Serialization;

    public partial class Form1 : Form
    {

        private Form2 Form2 = new CurePlease.Form2();

        public class BuffStorage : List<BuffStorage>
        {
            public string CharacterName { get; set; }

            public string CharacterBuffs { get; set; }
        }

        public class CharacterData : List<CharacterData>
        {
            public int TargetIndex { get; set; }

            public int MemberNumber { get; set; }
        }

        public class SongData : List<SongData>
        {
            public string song_type { get; set; }

            public int song_position { get; set; }

            public string song_name { get; set; }

            public int buff_id { get; set; }
        }

        public class SpellsData : List<SpellsData>
        {
            public string Spell_Name { get; set; }

            public int spell_position { get; set; }

            public int type { get; set; }

            public int buffID { get; set; }

            public bool aoe_version { get; set; }
        }

        public class GeoData : List<GeoData>
        {
            public int geo_position { get; set; }

            public string indi_spell { get; set; }

            public string geo_spell { get; set; }
        }

        public class JobTitles : List<JobTitles>
        {
            public int job_number { get; set; }

            public string job_name { get; set; }
        }

        private int currentSCHCharges = 0;

        private string debug_MSG_show = string.Empty;

        private int lastCommand = 0;

        private int lastKnownEstablisherTarget = 0;

        // BARD SONG VARIABLES
        private int song_casting = 0;

        private int PL_BRDCount = 0;
        private bool ForceSongRecast = false;
        private string Last_Song_Cast = string.Empty;


        private uint PL_Index = 0;
        private uint Monitored_Index = 0;


        //  private int song_casting = 0;
        //  private string LastSongCast = String.Empty;


        // private bool ForceSongRecast = false;
        //  private string Last_Song_Cast = String.Empty;


        // GEO ENGAGED CHECK
        public bool targetEngaged = false;

        public bool EclipticStillUp = false;

        public bool CastingBackground_Check = false;
        public bool JobAbilityLock_Check = false;

        public string JobAbilityCMD = String.Empty;

        private DateTime DefaultTime = new DateTime(1970, 1, 1);

        private bool curePlease_autofollow = false;

        private List<string> characterNames_naRemoval = new List<string>();

        public enum LoginStatus
        {
            CharacterLoginScreen = 0,
            Loading = 1,
            LoggedIn = 2
        }

        public enum Status : byte
        {
            Standing = 0,
            Fighting = 1,
            Dead1 = 2,
            Dead2 = 3,
            Event = 4,
            Chocobo = 5,
            Healing = 33,
            Synthing = 44,
            Sitting = 47,
            Fishing = 56,
            FishBite = 57,
            Obtained = 58,
            RodBreak = 59,
            LineBreak = 60,
            CatchMonster = 61,
            LostCatch = 62,
            Unknown
        }

        public string WindowerMode = "Windower";

        public List<JobTitles> JobNames = new List<JobTitles>();

        public List<SpellsData> barspells = new List<SpellsData>();

        public List<SpellsData> enspells = new List<SpellsData>();

        public List<SpellsData> stormspells = new List<SpellsData>();


        private int GetInventoryItemCount(EliteAPI api, ushort itemid)
        {
            int count = 0;
            for (int x = 0; x <= 80; x++)
            {
                EliteAPI.InventoryItem item = api.Inventory.GetContainerItem(0, x);
                if (item != null && item.Id == itemid)
                {
                    count += (int)item.Count;
                }
            }

            return count;
        }

        private int GetTempItemCount(EliteAPI api, ushort itemid)
        {
            int count = 0;
            for (int x = 0; x <= 80; x++)
            {
                EliteAPI.InventoryItem item = api.Inventory.GetContainerItem(3, x);
                if (item != null && item.Id == itemid)
                {
                    count += (int)item.Count;
                }
            }

            return count;
        }

        private ushort GetItemId(string name)
        {
            EliteAPI.IItem item = _ELITEAPIPL.Resources.GetItem(name, 0);
            return item != null ? (ushort)item.ItemID : (ushort)0;
        }

        private int GetAbilityRecastBySpellId(int id)
        {
            List<int> abilityIds = _ELITEAPIPL.Recast.GetAbilityIds();
            for (int x = 0; x < abilityIds.Count; x++)
            {
                if (abilityIds[x] == id)
                {
                    return _ELITEAPIPL.Recast.GetAbilityRecast(x);
                }
            }

            return -1;
        }

        public static EliteAPI _ELITEAPIPL;

        public EliteAPI _ELITEAPIMonitored;

        public ListBox processids = new ListBox();

        public ListBox activeprocessids = new ListBox();

        public double last_percent = 1;

        public string castingSpell = string.Empty;

        public int max_count = 10;
        public int spell_delay_count = 0;

        public int geo_step = 0;

        public int followWarning = 0;

        public bool stuckWarning = false;
        public int stuckCount = 0;

        public int protectionCount = 0;

        public int IDFound = 0;

        public float lastZ;
        public float lastX;
        public float lastY;

        // Stores the previously-colored button, if any
        public List<BuffStorage> ActiveBuffs = new List<BuffStorage>();

        public List<SongData> SongInfo = new List<SongData>();

        public List<GeoData> GeomancerInfo = new List<GeoData>();

        public List<int> known_song_buffs = new List<int>();

        public List<string> TemporaryItem_Zones = new List<string> { "Escha Ru'Aun", "Escha Zi'Tah", "Reisenjima", "Abyssea - La Theine", "Abyssea - Konschtat", "Abyssea - Tahrongi",
                                                                        "Abyssea - Attohwa", "Abyssea - Misareaux", "Abyssea - Vunkerl", "Abyssea - Altepa", "Abyssea - Uleguerand", "Abyssea - Grauberg", "Walk of Echoes" };

        public string wakeSleepSpellName = "Cure";

        public string plSilenceitemName = "Echo Drops";

        public string plDoomItemName = "Holy Water";

        private float plX;

        private float plY;

        private float plZ;

        private byte playerOptionsSelected;

        private byte autoOptionsSelected;

        private bool pauseActions;

        private bool islowmp;

        public int LUA_Plugin_Loaded = 0;

        public int firstTime_Pause = 0;

        private int rdmCurrentDebuffIndex = 0;
        private DateTime lastDebuffCastTime = new DateTime(1970, 1, 1);

        public int GetAbilityRecast(string checked_abilityName)
        {
            int id = _ELITEAPIPL.Resources.GetAbility(checked_abilityName, 0).TimerID;
            List<int> IDs = _ELITEAPIPL.Recast.GetAbilityIds();
            for (int x = 0; x < IDs.Count; x++)
            {
                if (IDs[x] == id)
                {
                    return _ELITEAPIPL.Recast.GetAbilityRecast(x);
                }
            }
            return 0;
        }

        public int CheckSpellRecast(string checked_recastspellName)
        {
            checked_recastspellName = checked_recastspellName.Trim().ToLower();

            if (checked_recastspellName == "honor march")
            {
                return 0;
            }

            if (checked_recastspellName != "blank")
            {
                EliteAPI.ISpell magic = _ELITEAPIPL.Resources.GetSpell(checked_recastspellName, 0);

                if (magic == null)
                {
                    showErrorMessage("Error detected, please Report Error: #SpellRecastError #" + checked_recastspellName);
                    return 1;
                }
                else
                {
                    if (_ELITEAPIPL.Recast.GetSpellRecast(magic.Index) == 0)
                    {
                        return 0;
                    }
                    else
                    {
                        return 1;
                    }
                }
            }
            else
            {
                return 1;
            }
        }

        public static bool HasAbility(string checked_abilityName)
        {
            if (_ELITEAPIPL.Player.GetPlayerInfo().Buffs.Any(b => b == 261) || _ELITEAPIPL.Player.GetPlayerInfo().Buffs.Any(b => b == 16)) // IF YOU HAVE INPAIRMENT/AMNESIA THEN BLOCK JOB ABILITY CASTING
            {
                return false;
            }
            else if (_ELITEAPIPL.Player.HasAbility(_ELITEAPIPL.Resources.GetAbility(checked_abilityName, 0).ID))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool HasSpell(string checked_spellName)
        {

            checked_spellName = checked_spellName.Trim().ToLower();

            if (checked_spellName == "honor march")
            {
                return true;
            }

            EliteAPI.ISpell magic = _ELITEAPIPL.Resources.GetSpell(checked_spellName, 0);

            if (_ELITEAPIPL.Player.GetPlayerInfo().Buffs.Any(b => b == 262)) // IF YOU HAVE OMERTA THEN BLOCK MAGIC CASTING
            {
                return false;
            }
            else if (_ELITEAPIPL.Player.HasSpell(magic.Index) && JobChecker(checked_spellName) == true)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool JobChecker(string SpellName)
        {

            string checked_spellName = SpellName.Trim().ToLower();

            EliteAPI.ISpell magic = _ELITEAPIPL.Resources.GetSpell(checked_spellName, 0); // GRAB THE REQUESTED SPELL DATA

            int mainjobLevelRequired = magic.LevelRequired[(_ELITEAPIPL.Player.MainJob)]; // GRAB SPELL LEVEL FOR THE MAIN JOB
            int subjobLevelRequired = magic.LevelRequired[(_ELITEAPIPL.Player.SubJob)]; // GRAB SPELL LEVEL FOR THE SUB JOB

            if (checked_spellName == "honor march")
            {
                return true;
            }

            if (mainjobLevelRequired <= _ELITEAPIPL.Player.MainJobLevel && mainjobLevelRequired != -1)
            { // IF THE MAIN JOB DOES NOT EQUAl -1 (Meaning the JOB can't use the spell) AND YOUR LEVEL IS EQUAL TO OR LOVER THAN THE REQUIRED LEVEL RETURN true
                return true;
            }
            else if (subjobLevelRequired <= _ELITEAPIPL.Player.SubJobLevel && subjobLevelRequired != -1)
            { // IF THE SUB JOB DOES NOT EQUAl -1 (Meaning the JOB can't use the spell) AND YOUR LEVEL IS EQUAL TO OR LOVER THAN THE REQUIRED LEVEL RETURN true
                return true;
            }
            else if (mainjobLevelRequired > 99 && mainjobLevelRequired != -1)
            { // IF THE MAIN JOB LEVEL IS GREATER THAN 99 BUT DOES NOT EQUAL -1 THEN IT IS A JOB POINT REQUIRED SPELL AND SO FURTHER CHECKS MUST BE MADE SO GRAB CURRENT JOB POINT TABLE
                EliteAPI.PlayerJobPoints JobPoints = _ELITEAPIPL.Player.GetJobPoints(_ELITEAPIPL.Player.MainJob);

                // Spell is a JP spell so check this works correctly and that you possess the spell
                if (checked_spellName == "refresh iii" || checked_spellName == "temper ii")
                {
                    if (_ELITEAPIPL.Player.MainJob == 5 && _ELITEAPIPL.Player.MainJobLevel == 99 && JobPoints.SpentJobPoints >= 1200) // IF MAIN JOB IS RDM, AND JOB LEVEL IS AT MAX WITH REQUIRED JOB POINTS
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else if (checked_spellName == "distract iii" || checked_spellName == "frazzle iii")
                {
                    if (_ELITEAPIPL.Player.MainJob == 5 && _ELITEAPIPL.Player.MainJobLevel == 99 && JobPoints.SpentJobPoints >= 550) // IF MAIN JOB IS RDM, AND JOB LEVEL IS AT MAX WITH REQUIRED JOB POINTS
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else if (checked_spellName.Contains("storm ii"))
                {
                    if (_ELITEAPIPL.Player.MainJob == 20 && _ELITEAPIPL.Player.MainJobLevel == 99 && JobPoints.SpentJobPoints >= 100) // IF MAIN JOB IS SCH, AND JOB LEVEL IS AT MAX WITH REQUIRED JOB POINTS
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else if (checked_spellName == "reraise iv")
                {
                    if (_ELITEAPIPL.Player.MainJob == 3 && _ELITEAPIPL.Player.MainJobLevel == 99 && JobPoints.SpentJobPoints >= 100) // IF MAIN JOB IS WHM, AND JOB LEVEL IS AT MAX WITH REQUIRED JOB POINTS
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else if (checked_spellName == "full cure")
                {
                    if (_ELITEAPIPL.Player.MainJob == 3 && _ELITEAPIPL.Player.MainJobLevel == 99 && JobPoints.SpentJobPoints >= 1200) // IF MAIN JOB IS WHM, AND JOB LEVEL IS AT MAX WITH REQUIRED JOB POINTS
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        // SPELL CHECKER CODE: (CheckSpellRecast("") == 0) && (HasSpell(""))
        // ABILITY CHECKER CODE: (GetAbilityRecast("") == 0) && (HasAbility(""))
        // PIANISSIMO TIME FORMAT
        // SONGNUMBER_SONGSET (Example: 1_2 = Song #1 in Set #2
        private bool[] autoHasteEnabled = new bool[]
      {
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false
      };

        private bool[] autoHaste_IIEnabled = new bool[]
      {
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false
      };

        private bool[] autoFlurryEnabled = new bool[]
      {
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false
      };

        private bool[] autoFlurry_IIEnabled = new bool[]
      {
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false
      };

        private bool[] autoPhalanx_IIEnabled = new bool[]
       {
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false
       };

        private bool[] autoRegen_Enabled = new bool[]
      {
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false
      };

        private bool[] autoShell_Enabled = new bool[]
      {
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false
      };

        private bool[] autoProtect_Enabled = new bool[]
      {
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false
      };

        private bool[] autoSandstormEnabled = new bool[]
{
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false
};

        private bool[] autoRainstormEnabled = new bool[]
{
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false
};

        private bool[] autoWindstormEnabled = new bool[]
{
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false
};

        private bool[] autoFirestormEnabled = new bool[]
{
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false
};

        private bool[] autoHailstormEnabled = new bool[]
{
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false
};

        private bool[] autoThunderstormEnabled = new bool[]
{
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false
};

        private bool[] autoVoidstormEnabled = new bool[]
{
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false
};

        private bool[] autoAurorastormEnabled = new bool[]
{
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false
};



        private bool[] autoRefreshEnabled = new bool[]
      {
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false
      };

        private bool[] autoAdloquium_Enabled = new bool[]
      {
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false
      };



        private DateTime currentTime = DateTime.Now;

        private DateTime[] playerHaste = new DateTime[]
      {
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0)
      };

        private DateTime[] playerHaste_II = new DateTime[]
      {
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0)
      };

        private DateTime[] playerStormspell = new DateTime[]
      {
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0)
      };

        private DateTime[] playerFlurry = new DateTime[]
      {
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0)
      };

        private DateTime[] playerFlurry_II = new DateTime[]
      {
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0)
      };

        private DateTime[] playerShell = new DateTime[]
      {
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0)
      };

        private DateTime[] playerProtect = new DateTime[]
      {
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0)
      };

        private DateTime[] playerPhalanx_II = new DateTime[]
      {
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0)
      };

        private DateTime[] playerRegen = new DateTime[]
       {
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0)
       };

        private DateTime[] playerRefresh = new DateTime[]
      {
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0)
      };

        private DateTime[] playerAdloquium = new DateTime[]
      {
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0)
      };

        private DateTime[] playerSong1 = new DateTime[]
      {
            new DateTime(1970, 1, 1, 0, 0, 0)
      };

        private DateTime[] playerSong2 = new DateTime[]
      {
            new DateTime(1970, 1, 1, 0, 0, 0)
      };

        private DateTime[] playerSong3 = new DateTime[]
      {
            new DateTime(1970, 1, 1, 0, 0, 0)
      };

        private DateTime[] playerSong4 = new DateTime[]
      {
            new DateTime(1970, 1, 1, 0, 0, 0)
      };

        private DateTime[] Last_SongCast_Timer = new DateTime[]
      {
            new DateTime(1970, 1, 1, 0, 0, 0)
      };

        private DateTime[] playerPianissimo1_1 = new DateTime[]
      {
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0)
      };

        private DateTime[] playerPianissimo2_1 = new DateTime[]
      {
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0)
      };

        private DateTime[] playerPianissimo1_2 = new DateTime[]
      {
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0)
      };

        private DateTime[] playerPianissimo2_2 = new DateTime[]
      {
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0),
            new DateTime(1970, 1, 1, 0, 0, 0)
      };

        private TimeSpan[] playerHasteSpan = new TimeSpan[]
      {
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan()
      };

        private TimeSpan[] playerStormspellSpan = new TimeSpan[]
      {
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan()
      };

        private TimeSpan[] playerHaste_IISpan = new TimeSpan[]
      {
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan()
      };

        private TimeSpan[] playerFlurrySpan = new TimeSpan[]
      {
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan()
      };

        private TimeSpan[] playerFlurry_IISpan = new TimeSpan[]
      {
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan()
      };

        private TimeSpan[] playerShell_Span = new TimeSpan[]
      {
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan()
      };

        private TimeSpan[] playerProtect_Span = new TimeSpan[]
      {
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan()
      };

        private TimeSpan[] playerPhalanx_IISpan = new TimeSpan[]
      {
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan()
      };

        private TimeSpan[] playerRegen_Span = new TimeSpan[]
      {
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan()
      };

        private TimeSpan[] playerRefresh_Span = new TimeSpan[]
      {
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan()
      };


        private TimeSpan[] playerAdloquium_Span = new TimeSpan[]
      {
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan()
      };

        private TimeSpan[] playerSong1_Span = new TimeSpan[]
      {
            new TimeSpan()
      };

        private TimeSpan[] playerSong2_Span = new TimeSpan[]
      {
            new TimeSpan()
      };

        private TimeSpan[] playerSong3_Span = new TimeSpan[]
      {
            new TimeSpan()
      };

        private TimeSpan[] playerSong4_Span = new TimeSpan[]
     {
            new TimeSpan()
     };

        private TimeSpan[] Last_SongCast_Timer_Span = new TimeSpan[]
     {
            new TimeSpan()
     };

        private TimeSpan[] pianissimo1_1_Span = new TimeSpan[]
      {
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
      };

        private TimeSpan[] pianissimo2_1_Span = new TimeSpan[]
      {
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
      };

        private TimeSpan[] pianissimo1_2_Span = new TimeSpan[]
      {
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
      };

        private TimeSpan[] pianissimo2_2_Span = new TimeSpan[]
      {
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
            new TimeSpan(),
      };

        private void PaintBorderlessGroupBox(object sender, PaintEventArgs e)
        {
            GroupBox box = sender as GroupBox;
            DrawGroupBox(box, e.Graphics, Color.Black, Color.Gray);
        }

        private void DrawGroupBox(GroupBox box, Graphics g, Color textColor, Color borderColor)
        {
            if (box != null)
            {
                Brush textBrush = new SolidBrush(textColor);
                Brush borderBrush = new SolidBrush(borderColor);
                Pen borderPen = new Pen(borderBrush);
                SizeF strSize = g.MeasureString(box.Text, box.Font);
                Rectangle rect = new Rectangle(box.ClientRectangle.X,
                                           box.ClientRectangle.Y + (int)(strSize.Height / 2),
                                           box.ClientRectangle.Width - 1,
                                           box.ClientRectangle.Height - (int)(strSize.Height / 2) - 1);

                // Clear text and border
                g.Clear(BackColor);

                // Draw text
                g.DrawString(box.Text, box.Font, textBrush, box.Padding.Left, 0);

                // Drawing Border
                //Left
                g.DrawLine(borderPen, rect.Location, new Point(rect.X, rect.Y + rect.Height));
                //Right
                g.DrawLine(borderPen, new Point(rect.X + rect.Width, rect.Y), new Point(rect.X + rect.Width, rect.Y + rect.Height));
                //Bottom
                g.DrawLine(borderPen, new Point(rect.X, rect.Y + rect.Height), new Point(rect.X + rect.Width, rect.Y + rect.Height));
                //Top1
                g.DrawLine(borderPen, new Point(rect.X, rect.Y), new Point(rect.X + box.Padding.Left, rect.Y));
                //Top2
                g.DrawLine(borderPen, new Point(rect.X + box.Padding.Left + (int)(strSize.Width), rect.Y), new Point(rect.X + rect.Width, rect.Y));
            }
        }

        private void PaintButton(object sender, PaintEventArgs e)
        {
            Button button = sender as Button;

            button.FlatAppearance.BorderColor = System.Drawing.Color.Gray;
        }


        public Form1()
        {


            StartPosition = FormStartPosition.CenterScreen;

            InitializeComponent();




            currentAction.Text = string.Empty;

            if (System.IO.File.Exists("debug"))
            {
                debug.Visible = true;
            }

            JobNames.Add(new JobTitles
            {
                job_number = 1,
                job_name = "WAR",
            });
            JobNames.Add(new JobTitles
            {
                job_number = 2,
                job_name = "MNK"
            });
            JobNames.Add(new JobTitles
            {
                job_number = 3,
                job_name = "WHM"
            });
            JobNames.Add(new JobTitles
            {
                job_number = 4,
                job_name = "BLM"
            });
            JobNames.Add(new JobTitles
            {
                job_number = 5,
                job_name = "RDM"
            });
            JobNames.Add(new JobTitles
            {
                job_number = 6,
                job_name = "THF"
            });
            JobNames.Add(new JobTitles
            {
                job_number = 7,
                job_name = "PLD"
            });
            JobNames.Add(new JobTitles
            {
                job_number = 8,
                job_name = "DRK"
            });
            JobNames.Add(new JobTitles
            {
                job_number = 9,
                job_name = "BST"
            });
            JobNames.Add(new JobTitles
            {
                job_number = 10,
                job_name = "BRD"
            });
            JobNames.Add(new JobTitles
            {
                job_number = 11,
                job_name = "RNG"
            });
            JobNames.Add(new JobTitles
            {
                job_number = 12,
                job_name = "SAM"
            });
            JobNames.Add(new JobTitles
            {
                job_number = 13,
                job_name = "NIN"
            });
            JobNames.Add(new JobTitles
            {
                job_number = 14,
                job_name = "DRG"
            });
            JobNames.Add(new JobTitles
            {
                job_number = 15,
                job_name = "SMN"
            });
            JobNames.Add(new JobTitles
            {
                job_number = 16,
                job_name = "BLU"
            });
            JobNames.Add(new JobTitles
            {
                job_number = 17,
                job_name = "COR"
            });
            JobNames.Add(new JobTitles
            {
                job_number = 18,
                job_name = "PUP"
            });
            JobNames.Add(new JobTitles
            {
                job_number = 19,
                job_name = "DNC"
            });
            JobNames.Add(new JobTitles
            {
                job_number = 20,
                job_name = "SCH"
            });

            JobNames.Add(new JobTitles
            {
                job_number = 21,
                job_name = "GEO"
            });
            JobNames.Add(new JobTitles
            {
                job_number = 22,
                job_name = "RUN"
            });

            int position = 0;

            // Buff lists
            known_song_buffs.Add(197);
            known_song_buffs.Add(198);
            known_song_buffs.Add(195);
            known_song_buffs.Add(199);
            known_song_buffs.Add(200);
            known_song_buffs.Add(215);
            known_song_buffs.Add(196);
            known_song_buffs.Add(214);
            known_song_buffs.Add(216);
            known_song_buffs.Add(218);
            known_song_buffs.Add(222);

            SongInfo.Add(new SongData
            {
                song_type = "Blank",
                song_name = "Blank",
                song_position = position,
                buff_id = 0
            });
            position++;

            SongInfo.Add(new SongData
            {
                song_type = "Minne",
                song_name = "Knight's Minne",
                song_position = position,
                buff_id = 197
            });
            position++;

            SongInfo.Add(new SongData
            {
                song_type = "Minne",
                song_name = "Knight's Minne II",
                song_position = position,
                buff_id = 197
            });
            position++;

            SongInfo.Add(new SongData
            {
                song_type = "Minne",
                song_name = "Knight's Minne III",
                song_position = position,
                buff_id = 197
            });
            position++;

            SongInfo.Add(new SongData
            {
                song_type = "Minne",
                song_name = "Knight's Minne IV",
                song_position = position,
                buff_id = 197
            });
            position++;

            SongInfo.Add(new SongData
            {
                song_type = "Minne",
                song_name = "Knight's Minne V",
                song_position = position,
                buff_id = 197
            });
            position++;

            SongInfo.Add(new SongData
            {
                song_type = "Blank",
                song_name = "Blank",
                song_position = position,
                buff_id = 0
            });
            position++;

            SongInfo.Add(new SongData
            {
                song_type = "Minuet",
                song_name = "Valor Minuet",
                song_position = position,
                buff_id = 198
            });
            position++;

            SongInfo.Add(new SongData
            {
                song_type = "Minuet",
                song_name = "Valor Minuet II",
                song_position = position,
                buff_id = 198
            });
            position++;

            SongInfo.Add(new SongData
            {
                song_type = "Minuet",
                song_name = "Valor Minuet III",
                song_position = position,
                buff_id = 198
            });
            position++;

            SongInfo.Add(new SongData
            {
                song_type = "Minuet",
                song_name = "Valor Minuet IV",
                song_position = position,
                buff_id = 198
            });
            position++;

            SongInfo.Add(new SongData
            {
                song_type = "Minuet",
                song_name = "Valor Minuet V",
                song_position = position,
                buff_id = 198
            });
            position++;

            SongInfo.Add(new SongData
            {
                song_type = "Blank",
                song_name = "Blank",
                song_position = position,
                buff_id = 0
            });
            position++;

            SongInfo.Add(new SongData
            {
                song_type = "Paeon",
                song_name = "Army's Paeon",
                song_position = position,
                buff_id = 195
            });
            position++;

            SongInfo.Add(new SongData
            {
                song_type = "Paeon",
                song_name = "Army's Paeon II",
                song_position = position,
                buff_id = 195
            });
            position++;

            SongInfo.Add(new SongData
            {
                song_type = "Paeon",
                song_name = "Army's Paeon III",
                song_position = position,
                buff_id = 195
            });
            position++;

            SongInfo.Add(new SongData
            {
                song_type = "Paeon",
                song_name = "Army's Paeon IV",
                song_position = position,
                buff_id = 195
            });
            position++;

            SongInfo.Add(new SongData
            {
                song_type = "Paeon",
                song_name = "Army's Paeon V",
                song_position = position,
                buff_id = 195
            });
            position++;

            SongInfo.Add(new SongData
            {
                song_type = "Paeon",
                song_name = "Army's Paeon VI",
                song_position = position,
                buff_id = 195
            });
            position++;

            SongInfo.Add(new SongData
            {
                song_type = "Blank",
                song_name = "Blank",
                song_position = position,
                buff_id = 0
            });
            position++;

            SongInfo.Add(new SongData
            {
                song_type = "Madrigal",
                song_name = "Sword Madrigal",
                song_position = position,
                buff_id = 199
            });
            position++;
            SongInfo.Add(new SongData
            {
                song_type = "Madrigal",
                song_name = "Blade Madrigal",
                song_position = position,
                buff_id = 199
            });
            position++;

            SongInfo.Add(new SongData
            {
                song_type = "Blank",
                song_name = "Blank",
                song_position = position,
                buff_id = 0
            });
            position++;

            SongInfo.Add(new SongData
            {
                song_type = "Prelude",
                song_name = "Hunter's Prelude",
                song_position = position,
                buff_id = 200
            });
            position++;

            SongInfo.Add(new SongData
            {
                song_type = "Prelude",
                song_name = "Archer's Prelude",
                song_position = position,
                buff_id = 200
            });
            position++;

            SongInfo.Add(new SongData
            {
                song_type = "Blank",
                song_name = "Blank",
                song_position = position,
                buff_id = 0
            });
            position++;

            SongInfo.Add(new SongData
            {
                song_type = "Etude",
                song_name = "Sinewy Etude",
                song_position = position,
                buff_id = 215
            });
            position++;

            SongInfo.Add(new SongData
            {
                song_type = "Etude",
                song_name = "Dextrous Etude",
                song_position = position,
                buff_id = 215
            });
            position++;

            SongInfo.Add(new SongData
            {
                song_type = "Etude",
                song_name = "Vivacious Etude",
                song_position = position,
                buff_id = 215
            });
            position++;

            SongInfo.Add(new SongData
            {
                song_type = "Etude",
                song_name = "Quick Etude",
                song_position = position,
                buff_id = 215
            });
            position++;

            SongInfo.Add(new SongData
            {
                song_type = "Etude",
                song_name = "Learned Etude",
                song_position = position,
                buff_id = 215
            });
            position++;

            SongInfo.Add(new SongData
            {
                song_type = "Etude",
                song_name = "Spirited Etude",
                song_position = position,
                buff_id = 215
            });
            position++;

            SongInfo.Add(new SongData
            {
                song_type = "Etude",
                song_name = "Enchanting Etude",
                song_position = position,
                buff_id = 215
            });
            position++;

            SongInfo.Add(new SongData
            {
                song_type = "Etude",
                song_name = "Herculean Etude",
                song_position = position,
                buff_id = 215
            });
            position++;

            SongInfo.Add(new SongData
            {
                song_type = "Etude",
                song_name = "Uncanny Etude",
                song_position = position,
                buff_id = 215
            });
            position++;

            SongInfo.Add(new SongData
            {
                song_type = "Etude",
                song_name = "Vital Etude",
                song_position = position,
                buff_id = 215
            });
            position++;

            SongInfo.Add(new SongData
            {
                song_type = "Etude",
                song_name = "Swift Etude",
                song_position = position,
                buff_id = 215
            });
            position++;

            SongInfo.Add(new SongData
            {
                song_type = "Etude",
                song_name = "Sage Etude",
                song_position = position,
                buff_id = 215
            });
            position++;

            SongInfo.Add(new SongData
            {
                song_type = "Etude",
                song_name = "Logical Etude",
                song_position = position,
                buff_id = 215
            });
            position++;

            SongInfo.Add(new SongData
            {
                song_type = "Etude",
                song_name = "Bewitching Etude",
                song_position = position,
                buff_id = 215
            });
            position++;

            SongInfo.Add(new SongData
            {
                song_type = "Blank",
                song_name = "Blank",
                song_position = position,
                buff_id = 0
            });
            position++;

            SongInfo.Add(new SongData
            {
                song_type = "Mambo",
                song_name = "Sheepfoe Mambo",
                song_position = position,
                buff_id = 201
            });
            position++;

            SongInfo.Add(new SongData
            {
                song_type = "Mambo",
                song_name = "Dragonfoe Mambo",
                song_position = position,
                buff_id = 201
            });
            position++;

            SongInfo.Add(new SongData
            {
                song_type = "Blank",
                song_name = "Blank",
                song_position = position,
                buff_id = 0
            });
            position++;

            SongInfo.Add(new SongData
            {
                song_type = "Ballad",
                song_name = "Mage's Ballad",
                song_position = position,
                buff_id = 196
            });
            position++;

            SongInfo.Add(new SongData
            {
                song_type = "Ballad",
                song_name = "Mage's Ballad II",
                song_position = position,
                buff_id = 196
            });
            position++;

            SongInfo.Add(new SongData
            {
                song_type = "Ballad",
                song_name = "Mage's Ballad III",
                song_position = position,
                buff_id = 196
            });
            position++;

            SongInfo.Add(new SongData
            {
                song_type = "Blank",
                song_name = "Blank",
                song_position = position,
                buff_id = 0
            });
            position++;

            SongInfo.Add(new SongData
            {
                song_type = "March",
                song_name = "Advancing March",
                song_position = position,
                buff_id = 214
            });
            position++;

            SongInfo.Add(new SongData
            {
                song_type = "March",
                song_name = "Victory March",
                song_position = position,
                buff_id = 214
            });
            position++;

            SongInfo.Add(new SongData
            {
                song_type = "March",
                song_name = "Honor March",
                song_position = position,
                buff_id = 214
            });
            position++;

            SongInfo.Add(new SongData
            {
                song_type = "Blank",
                song_name = "Blank",
                song_position = position,
                buff_id = 0
            });
            position++;

            SongInfo.Add(new SongData
            {
                song_type = "Carol",
                song_name = "Fire Carol",
                song_position = position
            });
            position++;

            SongInfo.Add(new SongData
            {
                song_type = "Carol",
                song_name = "Fire Carol II",
                song_position = position,
                buff_id = 216
            });
            position++;

            SongInfo.Add(new SongData
            {
                song_type = "Carol",
                song_name = "Ice Carol",
                song_position = position,
                buff_id = 216
            });
            position++;

            SongInfo.Add(new SongData
            {
                song_type = "Carol",
                song_name = "Ice Carol II",
                song_position = position,
                buff_id = 216
            });
            position++;

            SongInfo.Add(new SongData
            {
                song_type = "Carol",
                song_name = " Wind Carol",
                song_position = position,
                buff_id = 216
            });
            position++;

            SongInfo.Add(new SongData
            {
                song_type = "Carol",
                song_name = "Wind Carol II",
                song_position = position,
                buff_id = 216
            });
            position++;

            SongInfo.Add(new SongData
            {
                song_type = "Carol",
                song_name = "Earth Carol",
                song_position = position,
                buff_id = 216
            });
            position++;

            SongInfo.Add(new SongData
            {
                song_type = "Carol",
                song_name = "Earth Carol II",
                song_position = position,
                buff_id = 216
            });
            position++;

            SongInfo.Add(new SongData
            {
                song_type = "Carol",
                song_name = "Lightning Carol",
                song_position = position,
                buff_id = 216
            });
            position++;

            SongInfo.Add(new SongData
            {
                song_type = "Carol",
                song_name = "Lightning Carol II",
                song_position = position,
                buff_id = 216
            });
            position++;

            SongInfo.Add(new SongData
            {
                song_type = "Carol",
                song_name = "Water Carol",
                song_position = position,
                buff_id = 216
            });
            position++;

            SongInfo.Add(new SongData
            {
                song_type = "Carol",
                song_name = "Water Carol II",
                song_position = position,
                buff_id = 216
            });
            position++;

            SongInfo.Add(new SongData
            {
                song_type = "Carol",
                song_name = "Light Carol",
                song_position = position,
                buff_id = 216
            });
            position++;

            SongInfo.Add(new SongData
            {
                song_type = "Carol",
                song_name = "Light Carol II",
                song_position = position,
                buff_id = 216
            });
            position++;

            SongInfo.Add(new SongData
            {
                song_type = "Carol",
                song_name = "Dark Carol",
                song_position = position,
                buff_id = 216
            });
            position++;

            SongInfo.Add(new SongData
            {
                song_type = "Carol",
                song_name = "Dark Carol II",
                song_position = position,
                buff_id = 216
            });
            position++;

            SongInfo.Add(new SongData
            {
                song_type = "Blank",
                song_name = "Blank",
                song_position = position,
                buff_id = 0
            });
            position++;

            SongInfo.Add(new SongData
            {
                song_type = "Hymnus",
                song_name = "Godess's Hymnus",
                song_position = position,
                buff_id = 218
            });
            position++;

            SongInfo.Add(new SongData
            {
                song_type = "Blank",
                song_name = "Blank",
                song_position = position,
                buff_id = 0
            });
            position++;

            SongInfo.Add(new SongData
            {
                song_type = "Scherzo",
                song_name = "Sentinel's Scherzo",
                song_position = position,
                buff_id = 222
            });
            position++;

            int geo_position = 0;

            GeomancerInfo.Add(new GeoData
            {
                indi_spell = "Indi-Voidance",
                geo_spell = "Geo-Voidance",
                geo_position = geo_position,
            });
            geo_position++;

            GeomancerInfo.Add(new GeoData
            {
                indi_spell = "Indi-Precision",
                geo_spell = "Geo-Precision",
                geo_position = geo_position,
            });
            geo_position++;

            GeomancerInfo.Add(new GeoData
            {
                indi_spell = "Indi-Regen",
                geo_spell = "Geo-Regen",
                geo_position = geo_position,
            });
            geo_position++;

            GeomancerInfo.Add(new GeoData
            {
                indi_spell = "Indi-Haste",
                geo_spell = "Geo-Haste",
                geo_position = geo_position,
            });
            geo_position++;

            GeomancerInfo.Add(new GeoData
            {
                indi_spell = "Indi-Attunement",
                geo_spell = "Geo-Attunement",
                geo_position = geo_position,
            });
            geo_position++;

            GeomancerInfo.Add(new GeoData
            {
                indi_spell = "Indi-Focus",
                geo_spell = "Geo-Focus",
                geo_position = geo_position,
            });
            geo_position++;

            GeomancerInfo.Add(new GeoData
            {
                indi_spell = "Indi-Barrier",
                geo_spell = "Geo-Barrier",
                geo_position = geo_position,
            });
            geo_position++;

            GeomancerInfo.Add(new GeoData
            {
                indi_spell = "Indi-Refresh",
                geo_spell = "Geo-Refresh",
                geo_position = geo_position,
            });
            geo_position++;

            GeomancerInfo.Add(new GeoData
            {
                indi_spell = "Indi-CHR",
                geo_spell = "Geo-CHR",
                geo_position = geo_position,
            });
            geo_position++;

            GeomancerInfo.Add(new GeoData
            {
                indi_spell = "Indi-MND",
                geo_spell = "Geo-MND",
                geo_position = geo_position,
            });
            geo_position++;

            GeomancerInfo.Add(new GeoData
            {
                indi_spell = "Indi-Fury",
                geo_spell = "Geo-Fury",
                geo_position = geo_position,
            });
            geo_position++;

            GeomancerInfo.Add(new GeoData
            {
                indi_spell = "Indi-INT",
                geo_spell = "Geo-INT",
                geo_position = geo_position,
            });
            geo_position++;

            GeomancerInfo.Add(new GeoData
            {
                indi_spell = "Indi-AGI",
                geo_spell = "Geo-AGI",
                geo_position = geo_position,
            });
            geo_position++;

            GeomancerInfo.Add(new GeoData
            {
                indi_spell = "Indi-Fend",
                geo_spell = "Geo-Fend",
                geo_position = geo_position,
            });
            geo_position++;

            GeomancerInfo.Add(new GeoData
            {
                indi_spell = "Indi-VIT",
                geo_spell = "Geo-VIT",
                geo_position = geo_position,
            });
            geo_position++;

            GeomancerInfo.Add(new GeoData
            {
                indi_spell = "Indi-DEX",
                geo_spell = "Geo-DEX",
                geo_position = geo_position,
            });
            geo_position++;

            GeomancerInfo.Add(new GeoData
            {
                indi_spell = "Indi-Acumen",
                geo_spell = "Geo-Acumen",
                geo_position = geo_position,
            });
            geo_position++;

            GeomancerInfo.Add(new GeoData
            {
                indi_spell = "Indi-STR",
                geo_spell = "Geo-STR",
                geo_position = geo_position,
            });
            geo_position++;

            GeomancerInfo.Add(new GeoData
            {
                indi_spell = "Indi-Poison",
                geo_spell = "Geo-Poison",
                geo_position = geo_position,
            });
            geo_position++;

            GeomancerInfo.Add(new GeoData
            {
                indi_spell = "Indi-Slow",
                geo_spell = "Geo-Slow",
                geo_position = geo_position,
            });
            geo_position++;

            GeomancerInfo.Add(new GeoData
            {
                indi_spell = "Indi-Torpor",
                geo_spell = "Geo-Torpor",
                geo_position = geo_position,
            });
            geo_position++;

            GeomancerInfo.Add(new GeoData
            {
                indi_spell = "Indi-Slip",
                geo_spell = "Geo-Slip",
                geo_position = geo_position,
            });
            geo_position++;

            GeomancerInfo.Add(new GeoData
            {
                indi_spell = "Indi-Languor",
                geo_spell = "Geo-Languor",
                geo_position = geo_position,
            });
            geo_position++;

            GeomancerInfo.Add(new GeoData
            {
                indi_spell = "Indi-Paralysis",
                geo_spell = "Geo-Paralysis",
                geo_position = geo_position,
            });
            geo_position++;

            GeomancerInfo.Add(new GeoData
            {
                indi_spell = "Indi-Vex",
                geo_spell = "Geo-Vex",
                geo_position = geo_position,
            });
            geo_position++;

            GeomancerInfo.Add(new GeoData
            {
                indi_spell = "Indi-Frailty",
                geo_spell = "Geo-Frailty",
                geo_position = geo_position,
            });
            geo_position++;

            GeomancerInfo.Add(new GeoData
            {
                indi_spell = "Indi-Wilt",
                geo_spell = "Geo-Wilt",
                geo_position = geo_position,
            });
            geo_position++;

            GeomancerInfo.Add(new GeoData
            {
                indi_spell = "Indi-Malaise",
                geo_spell = "Geo-Malaise",
                geo_position = geo_position,
            });
            geo_position++;

            GeomancerInfo.Add(new GeoData
            {
                indi_spell = "Indi-Gravity",
                geo_spell = "Geo-Gravity",
                geo_position = geo_position,
            });
            geo_position++;

            GeomancerInfo.Add(new GeoData
            {
                indi_spell = "Indi-Fade",
                geo_spell = "Geo-Fade",
                geo_position = geo_position,
            });
            geo_position++;

            barspells.Add(new SpellsData
            {
                Spell_Name = "Barfire",
                type = 1,
                spell_position = 0,
                buffID = 100,
            });
            barspells.Add(new SpellsData
            {
                Spell_Name = "Barfira",
                type = 1,
                spell_position = 0,
                buffID = 100,
                aoe_version = true,
            });
            barspells.Add(new SpellsData
            {
                Spell_Name = "Barstone",
                type = 1,
                spell_position = 1,
                buffID = 103,
            });
            barspells.Add(new SpellsData
            {
                Spell_Name = "Barstonra",
                type = 1,
                spell_position = 1,
                buffID = 103,
                aoe_version = true,
            });
            barspells.Add(new SpellsData
            {
                Spell_Name = "Barwater",
                type = 1,
                spell_position = 2,
                buffID = 105,
            });
            barspells.Add(new SpellsData
            {
                Spell_Name = "Barwatera",
                type = 1,
                spell_position = 2,
                buffID = 105,
                aoe_version = true,
            });
            barspells.Add(new SpellsData
            {
                Spell_Name = "Baraero",
                type = 1,
                spell_position = 3,
                buffID = 102
            });
            barspells.Add(new SpellsData
            {
                Spell_Name = "Baraera",
                type = 1,
                spell_position = 3,
                buffID = 102,
                aoe_version = true,
            });
            barspells.Add(new SpellsData
            {
                Spell_Name = "Barblizzard",
                type = 1,
                spell_position = 4,
                buffID = 101
            });
            barspells.Add(new SpellsData
            {
                Spell_Name = "Barblizzara",
                type = 1,
                spell_position = 4,
                buffID = 101,
                aoe_version = true,
            });
            barspells.Add(new SpellsData
            {
                Spell_Name = "Barthunder",
                type = 1,
                spell_position = 5,
                buffID = 104
            });
            barspells.Add(new SpellsData
            {
                Spell_Name = "Barthundra",
                type = 1,
                spell_position = 5,
                buffID = 104,
                aoe_version = true,
            });

            barspells.Add(new SpellsData
            {
                Spell_Name = "Baramnesia",
                type = 2,
                spell_position = 0,
                buffID = 286,
            });
            barspells.Add(new SpellsData
            {
                Spell_Name = "Baramnesra",
                type = 2,
                spell_position = 0,
                buffID = 286,
                aoe_version = true,
            });
            barspells.Add(new SpellsData
            {
                Spell_Name = "Barvirus",
                type = 2,
                spell_position = 1,
                buffID = 112
            });
            barspells.Add(new SpellsData
            {
                Spell_Name = "Barvira",
                type = 2,
                spell_position = 1,
                buffID = 112,
                aoe_version = true,
            });
            barspells.Add(new SpellsData
            {
                Spell_Name = "Barparalyze",
                type = 2,
                spell_position = 2,
                buffID = 108
            });
            barspells.Add(new SpellsData
            {
                Spell_Name = "Barparalyzra",
                type = 2,
                spell_position = 2,
                buffID = 108,
                aoe_version = true,
            });
            barspells.Add(new SpellsData
            {
                Spell_Name = "Barsilence",
                type = 2,
                spell_position = 3,
                buffID = 110
            });
            barspells.Add(new SpellsData
            {
                Spell_Name = "Barsilencera",
                type = 2,
                spell_position = 3,
                buffID = 110,
                aoe_version = true,
            });
            barspells.Add(new SpellsData
            {
                Spell_Name = "Barpetrify",
                type = 2,
                spell_position = 4,
                buffID = 111
            });
            barspells.Add(new SpellsData
            {
                Spell_Name = "Barpetra",
                type = 2,
                spell_position = 4,
                buffID = 111,
                aoe_version = true,
            });
            barspells.Add(new SpellsData
            {
                Spell_Name = "Barpoison",
                type = 2,
                spell_position = 5,
                buffID = 107
            });
            barspells.Add(new SpellsData
            {
                Spell_Name = "Barpoisonra",
                type = 2,
                spell_position = 5,
                buffID = 107,
                aoe_version = true,
            });
            barspells.Add(new SpellsData
            {
                Spell_Name = "Barblind",
                type = 2,
                spell_position = 6,
                buffID = 109
            });
            barspells.Add(new SpellsData
            {
                Spell_Name = "Barblindra",
                type = 2,
                spell_position = 6,
                buffID = 109,
                aoe_version = true,
            });
            barspells.Add(new SpellsData
            {
                Spell_Name = "Barsleep",
                type = 2,
                spell_position = 7,
                buffID = 106
            });
            barspells.Add(new SpellsData
            {
                Spell_Name = "Barsleepra",
                type = 2,
                spell_position = 7,
                buffID = 106,
                aoe_version = true,
            });

            enspells.Add(new SpellsData
            {
                Spell_Name = "Enfire",
                type = 1,
                spell_position = 0,
                buffID = 94
            });
            enspells.Add(new SpellsData
            {
                Spell_Name = "Enstone",
                type = 1,
                spell_position = 1,
                buffID = 97
            });
            enspells.Add(new SpellsData
            {
                Spell_Name = "Enwater",
                type = 1,
                spell_position = 2,
                buffID = 99
            });
            enspells.Add(new SpellsData
            {
                Spell_Name = "Enaero",
                type = 1,
                spell_position = 3,
                buffID = 96
            });
            enspells.Add(new SpellsData
            {
                Spell_Name = "Enblizzard",
                type = 1,
                spell_position = 4,
                buffID = 95
            });
            enspells.Add(new SpellsData
            {
                Spell_Name = "Enthunder",
                type = 1,
                spell_position = 5,
                buffID = 98
            });

            enspells.Add(new SpellsData
            {
                Spell_Name = "Enfire II",
                type = 1,
                spell_position = 6,
                buffID = 277
            });
            enspells.Add(new SpellsData
            {
                Spell_Name = "Enstone II",
                type = 1,
                spell_position = 7,
                buffID = 280
            });
            enspells.Add(new SpellsData
            {
                Spell_Name = "Enwater II",
                type = 1,
                spell_position = 8,
                buffID = 282
            });
            enspells.Add(new SpellsData
            {
                Spell_Name = "Enaero II",
                type = 1,
                spell_position = 9,
                buffID = 279
            });
            enspells.Add(new SpellsData
            {
                Spell_Name = "Enblizzard II",
                type = 1,
                spell_position = 10,
                buffID = 278
            });
            enspells.Add(new SpellsData
            {
                Spell_Name = "Enthunder II",
                type = 1,
                spell_position = 11,
                buffID = 281
            });

            stormspells.Add(new SpellsData
            {
                Spell_Name = "Firestorm",
                type = 1,
                spell_position = 0,
                buffID = 178
            });
            stormspells.Add(new SpellsData
            {
                Spell_Name = "Sandstorm",
                type = 1,
                spell_position = 1,
                buffID = 181
            });
            stormspells.Add(new SpellsData
            {
                Spell_Name = "Rainstorm",
                type = 1,
                spell_position = 2,
                buffID = 183
            });
            stormspells.Add(new SpellsData
            {
                Spell_Name = "Windstorm",
                type = 1,
                spell_position = 3,
                buffID = 180
            });
            stormspells.Add(new SpellsData
            {
                Spell_Name = "Hailstorm",
                type = 1,
                spell_position = 4,
                buffID = 179
            });
            stormspells.Add(new SpellsData
            {
                Spell_Name = "Thunderstorm",
                type = 1,
                spell_position = 5,
                buffID = 182
            });
            stormspells.Add(new SpellsData
            {
                Spell_Name = "Voidstorm",
                type = 1,
                spell_position = 6,
                buffID = 185
            });
            stormspells.Add(new SpellsData
            {
                Spell_Name = "Aurorastorm",
                type = 1,
                spell_position = 7,
                buffID = 184
            });

            stormspells.Add(new SpellsData
            {
                Spell_Name = "Firestorm II",
                type = 1,
                spell_position = 8,
                buffID = 589
            });
            stormspells.Add(new SpellsData
            {
                Spell_Name = "Sandstorm II",
                type = 1,
                spell_position = 9,
                buffID = 592
            });
            stormspells.Add(new SpellsData
            {
                Spell_Name = "Rainstorm II",
                type = 1,
                spell_position = 10,
                buffID = 594
            });
            stormspells.Add(new SpellsData
            {
                Spell_Name = "Windstorm II",
                type = 1,
                spell_position = 11,
                buffID = 591
            });
            stormspells.Add(new SpellsData
            {
                Spell_Name = "Hailstorm II",
                type = 1,
                spell_position = 12,
                buffID = 590
            });
            stormspells.Add(new SpellsData
            {
                Spell_Name = "Thunderstorm II",
                type = 1,
                spell_position = 13,
                buffID = 593
            });
            stormspells.Add(new SpellsData
            {
                Spell_Name = "Voidstorm II",
                type = 1,
                spell_position = 14,
                buffID = 596
            });
            stormspells.Add(new SpellsData
            {
                Spell_Name = "Aurorastorm II",
                type = 1,
                spell_position = 15,
                buffID = 595
            });

            IEnumerable<Process> pol = Process.GetProcessesByName("pol").Union(Process.GetProcessesByName("xiloader")).Union(Process.GetProcessesByName("edenxi"));

            if (pol.Count() < 1)
            {
                MessageBox.Show("FFXI not found");
            }
            else
            {
                for (int i = 0; i < pol.Count(); i++)
                {
                    POLID.Items.Add(pol.ElementAt(i).MainWindowTitle);
                    POLID2.Items.Add(pol.ElementAt(i).MainWindowTitle);
                    processids.Items.Add(pol.ElementAt(i).Id);
                    activeprocessids.Items.Add(pol.ElementAt(i).Id);
                }
                POLID.SelectedIndex = 0;
                POLID2.SelectedIndex = 0;
                processids.SelectedIndex = 0;
                activeprocessids.SelectedIndex = 0;
            }
            // Show the current version number..
            Text = notifyIcon1.Text = "Cure Please v" + Application.ProductVersion;

            notifyIcon1.BalloonTipTitle = "Cure Please v" + Application.ProductVersion;
            notifyIcon1.BalloonTipText = "CurePlease has been minimized.";
            notifyIcon1.BalloonTipIcon = ToolTipIcon.Info;
        }

        private void setinstance_Click(object sender, EventArgs e)
        {
            if (!CheckForDLLFiles())
            {
                MessageBox.Show(
                    "Unable to locate EliteAPI.dll or EliteMMO.API.dll\nMake sure both files are in the same directory as the application",
                    "Error");
                return;
            }

            processids.SelectedIndex = POLID.SelectedIndex;
            activeprocessids.SelectedIndex = POLID.SelectedIndex;
            _ELITEAPIPL = new EliteAPI((int)processids.SelectedItem);
            plLabel.Text = "Selected PL: " + _ELITEAPIPL.Player.Name;
            Text = notifyIcon1.Text = _ELITEAPIPL.Player.Name + " - " + "Cure Please v" + Application.ProductVersion;

            plLabel.ForeColor = Color.Green;
            POLID.BackColor = Color.White;
            plPosition.Enabled = true;
            setinstance2.Enabled = true;
            Form2.config.autoFollowName = string.Empty;

            ForceSongRecast = true;

            foreach (Process dats in Process.GetProcessesByName("pol").Union(Process.GetProcessesByName("xiloader")).Union(Process.GetProcessesByName("edenxi")).Where(dats => POLID.Text == dats.MainWindowTitle))
            {
                for (int i = 0; i < dats.Modules.Count; i++)
                {
                    if (dats.Modules[i].FileName.Contains("Ashita.dll"))
                    {
                        WindowerMode = "Ashita";
                    }
                    else if (dats.Modules[i].FileName.Contains("Hook.dll"))
                    {
                        WindowerMode = "Windower";
                    }
                }
            }

            if (firstTime_Pause == 0)
            {
                Follow_BGW.RunWorkerAsync();
                AddonReader.RunWorkerAsync();
                firstTime_Pause = 1;
            }

            // LOAD AUTOMATIC SETTINGS
            string path = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Settings");
            if (System.IO.File.Exists(path + "/loadSettings"))
            {
                if (_ELITEAPIPL.Player.MainJob != 0)
                {
                    if (_ELITEAPIPL.Player.SubJob != 0)
                    {
                        JobTitles mainJob = JobNames.Where(c => c.job_number == _ELITEAPIPL.Player.MainJob).FirstOrDefault();
                        JobTitles subJob = JobNames.Where(c => c.job_number == _ELITEAPIPL.Player.SubJob).FirstOrDefault();

                        string filename = path + "\\" + _ELITEAPIPL.Player.Name + "_" + mainJob.job_name + "_" + subJob.job_name + ".xml";
                        string filename2 = path + "\\" + mainJob.job_name + "_" + subJob.job_name + ".xml";


                        if (System.IO.File.Exists(filename))
                        {
                            Form2.MySettings config = new Form2.MySettings();

                            XmlSerializer mySerializer = new XmlSerializer(typeof(Form2.MySettings));

                            StreamReader reader = new StreamReader(filename);
                            config = (Form2.MySettings)mySerializer.Deserialize(reader);

                            reader.Close();
                            reader.Dispose();
                            Form2.updateForm(config);
                            Form2.button4_Click(sender, e);
                        }
                        else if (System.IO.File.Exists(filename2))
                        {
                            Form2.MySettings config = new Form2.MySettings();

                            XmlSerializer mySerializer = new XmlSerializer(typeof(Form2.MySettings));

                            StreamReader reader = new StreamReader(filename2);
                            config = (Form2.MySettings)mySerializer.Deserialize(reader);

                            reader.Close();
                            reader.Dispose();
                            Form2.updateForm(config);
                            Form2.button4_Click(sender, e);
                        }
                    }
                }
            }

            if (LUA_Plugin_Loaded == 0 && !Form2.config.pauseOnStartBox && _ELITEAPIMonitored != null)
            {
                // Wait a milisecond and then load and set the config.
                Thread.Sleep(500);

                if (WindowerMode == "Windower")
                {
                    _ELITEAPIPL.ThirdParty.SendString("//lua load CurePlease_addon");
                    Thread.Sleep(1500);
                    _ELITEAPIPL.ThirdParty.SendString("//cpaddon settings " + Form2.config.ipAddress + " " + Form2.config.listeningPort);
                    Thread.Sleep(100);
                    _ELITEAPIPL.ThirdParty.SendString("//cpaddon verify");
                    if (Form2.config.enableHotKeys)
                    {
                        _ELITEAPIPL.ThirdParty.SendString("//bind ^!F1 cureplease toggle");
                        _ELITEAPIPL.ThirdParty.SendString("//bind ^!F2 cureplease start");
                        _ELITEAPIPL.ThirdParty.SendString("//bind ^!F3 cureplease pause");
                    }
                }
                else if (WindowerMode == "Ashita")
                {
                    _ELITEAPIPL.ThirdParty.SendString("/addon load CurePlease_addon");
                    Thread.Sleep(1500);
                    _ELITEAPIPL.ThirdParty.SendString("/cpaddon settings " + Form2.config.ipAddress + " " + Form2.config.listeningPort);
                    Thread.Sleep(100);

                    _ELITEAPIPL.ThirdParty.SendString("/cpaddon verify");
                    if (Form2.config.enableHotKeys)
                    {
                        _ELITEAPIPL.ThirdParty.SendString("/bind ^!F1 /cureplease toggle");
                        _ELITEAPIPL.ThirdParty.SendString("/bind ^!F2 /cureplease start");
                        _ELITEAPIPL.ThirdParty.SendString("/bind ^!F3 /cureplease pause");
                    }
                }

                AddOnStatus_Click(sender, e);


                currentAction.Text = "LUA Addon loaded. ( " + Form2.config.ipAddress + " - " + Form2.config.listeningPort + " )";

                LUA_Plugin_Loaded = 1;
            }
        }

        private void setinstance2_Click(object sender, EventArgs e)
        {
            if (!CheckForDLLFiles())
            {
                MessageBox.Show(
                    "Unable to locate EliteAPI.dll or EliteMMO.API.dll\nMake sure both files are in the same directory as the application",
                    "Error");
                return;
            }
            processids.SelectedIndex = POLID2.SelectedIndex;
            _ELITEAPIMonitored = new EliteAPI((int)processids.SelectedItem);
            monitoredLabel.Text = "Monitoring: " + _ELITEAPIMonitored.Player.Name;
            monitoredLabel.ForeColor = Color.Green;
            POLID2.BackColor = Color.White;
            partyMembersUpdate.Enabled = true;
            actionTimer.Enabled = true;
            pauseButton.Enabled = true;
            hpUpdates.Enabled = true;

            if (Form2.config.pauseOnStartBox)
            {
                pauseActions = true;
                pauseButton.Text = "Loaded, Paused!";
                pauseButton.ForeColor = Color.Red;
                actionTimer.Enabled = false;
            }
            else
            {
                if (Form2.config.MinimiseonStart == true && WindowState != FormWindowState.Minimized)
                {
                    WindowState = FormWindowState.Minimized;
                }
            }

            if (LUA_Plugin_Loaded == 0 && !Form2.config.pauseOnStartBox && _ELITEAPIPL != null)
            {
                // Wait a milisecond and then load and set the config.
                Thread.Sleep(500);
                if (WindowerMode == "Windower")
                {
                    _ELITEAPIPL.ThirdParty.SendString("//lua load CurePlease_addon");
                    Thread.Sleep(1500);
                    _ELITEAPIPL.ThirdParty.SendString("//cpaddon settings " + Form2.config.ipAddress + " " + Form2.config.listeningPort);
                    Thread.Sleep(100);
                    _ELITEAPIPL.ThirdParty.SendString("//cpaddon verify");

                    if (Form2.config.enableHotKeys)
                    {
                        _ELITEAPIPL.ThirdParty.SendString("//bind ^!F1 cureplease toggle");
                        _ELITEAPIPL.ThirdParty.SendString("//bind ^!F2 cureplease start");
                        _ELITEAPIPL.ThirdParty.SendString("//bind ^!F3 cureplease pause");
                    }
                }
                else if (WindowerMode == "Ashita")
                {
                    _ELITEAPIPL.ThirdParty.SendString("/addon load CurePlease_addon");
                    Thread.Sleep(1500);
                    _ELITEAPIPL.ThirdParty.SendString("/cpaddon settings " + Form2.config.ipAddress + " " + Form2.config.listeningPort);
                    Thread.Sleep(100);
                    _ELITEAPIPL.ThirdParty.SendString("/cpaddon verify");
                    if (Form2.config.enableHotKeys)
                    {
                        _ELITEAPIPL.ThirdParty.SendString("/bind ^!F1 /cureplease toggle");
                        _ELITEAPIPL.ThirdParty.SendString("/bind ^!F2 /cureplease start");
                        _ELITEAPIPL.ThirdParty.SendString("/bind ^!F3 /cureplease pause");
                    }
                }

                currentAction.Text = "LUA Addon loaded. ( " + Form2.config.ipAddress + " - " + Form2.config.listeningPort + " )";

                LUA_Plugin_Loaded = 1;

                AddOnStatus_Click(sender, e);

                lastCommand = _ELITEAPIMonitored.ThirdParty.ConsoleIsNewCommand();
            }
        }

        private bool CheckForDLLFiles()
        {
            if (!File.Exists("eliteapi.dll") || !File.Exists("elitemmo.api.dll"))
            {
                return false;
            }
            return true;
        }

        private string CureTiers(string cureSpell, bool HP)
        {
            if (cureSpell.ToLower() == "cure vi")
            {
                if (HasSpell("Cure VI") && JobChecker("Cure VI") == true && CheckSpellRecast("Cure VI") == 0)
                {
                    return "Cure VI";
                }
                else if (HasSpell("Cure V") && JobChecker("Cure V") == true && CheckSpellRecast("Cure V") == 0 && Form2.config.Undercure)
                {
                    return "Cure V";
                }
                else if (HasSpell("Cure IV") && JobChecker("Cure IV") == true && CheckSpellRecast("Cure IV") == 0 && Form2.config.Undercure)
                {
                    return "Cure IV";
                }
                else
                {
                    return "false";
                }
            }
            else if (cureSpell.ToLower() == "cure v")
            {
                if (HasSpell("Cure V") && JobChecker("Cure V") == true && CheckSpellRecast("Cure V") == 0)
                {
                    return "Cure V";
                }
                else if (HasSpell("Cure IV") && JobChecker("Cure IV") == true && CheckSpellRecast("Cure IV") == 0 && Form2.config.Undercure)
                {
                    return "Cure IV";
                }
                else if (HasSpell("Cure VI") && JobChecker("Cure VI") == true && CheckSpellRecast("Cure VI") == 0 && (Form2.config.Overcure && Form2.config.OvercureOnHighPriority != true || Form2.config.OvercureOnHighPriority && HP == true))
                {
                    return "Cure VI";
                }
                else
                {
                    return "false";
                }
            }
            else if (cureSpell.ToLower() == "cure iv")
            {
                if (HasSpell("Cure IV") && JobChecker("Cure IV") == true && CheckSpellRecast("Cure IV") == 0)
                {
                    return "Cure IV";
                }
                else if (HasSpell("Cure III") && JobChecker("Cure III") == true && CheckSpellRecast("Cure III") == 0 && Form2.config.Undercure)
                {
                    return "Cure III";
                }
                else if (HasSpell("Cure V") && JobChecker("Cure V") == true && CheckSpellRecast("Cure V") == 0 && (Form2.config.Overcure && Form2.config.OvercureOnHighPriority != true || Form2.config.OvercureOnHighPriority && HP == true))
                {
                    return "Cure V";
                }
                else
                {
                    return "false";
                }
            }
            else if (cureSpell.ToLower() == "cure iii")
            {
                if (HasSpell("Cure III") && JobChecker("Cure III") == true && CheckSpellRecast("Cure III") == 0)
                {
                    return "Cure III";
                }
                else if (HasSpell("Cure IV") && JobChecker("Cure IV") == true && CheckSpellRecast("Cure IV") == 0 && (Form2.config.Overcure && Form2.config.OvercureOnHighPriority != true || Form2.config.OvercureOnHighPriority && HP == true))
                {
                    return "Cure IV";
                }
                else if (HasSpell("Cure II") && JobChecker("Cure II") == true && CheckSpellRecast("Cure II") == 0 && Form2.config.Undercure)
                {
                    return "Cure II";
                }
                else
                {
                    return "false";
                }
            }
            else if (cureSpell.ToLower() == "cure ii")
            {
                if (HasSpell("Cure II") && JobChecker("Cure II") == true && CheckSpellRecast("Cure II") == 0)
                {
                    return "Cure II";
                }
                else if (HasSpell("Cure") && JobChecker("Cure") == true && CheckSpellRecast("Cure") == 0 && Form2.config.Undercure)
                {
                    return "Cure";
                }
                else if (HasSpell("Cure III") && JobChecker("Cure III") == true && CheckSpellRecast("Cure III") == 0 && (Form2.config.Overcure && Form2.config.OvercureOnHighPriority != true || Form2.config.OvercureOnHighPriority && HP == true))
                {
                    return "Cure III";
                }
                else
                {
                    return "false";
                }
            }
            else if (cureSpell.ToLower() == "cure")
            {
                if (HasSpell("Cure") && JobChecker("Cure") == true && CheckSpellRecast("Cure") == 0)
                {
                    return "Cure";
                }
                else if (HasSpell("Cure II") && JobChecker("Cure II") == true && CheckSpellRecast("Cure II") == 0 && (Form2.config.Overcure && Form2.config.OvercureOnHighPriority != true || Form2.config.OvercureOnHighPriority && HP == true))
                {
                    return "Cure II";
                }
                else
                {
                    return "false";
                }
            }
            else if (cureSpell.ToLower() == "curaga v")
            {
                if (HasSpell("Curaga V") && JobChecker("Curaga V") == true && CheckSpellRecast("Curaga V") == 0)
                {
                    return "Curaga V";
                }
                else if (HasSpell("Curaga IV") && JobChecker("Curaga IV") == true && CheckSpellRecast("Curaga IV") == 0 && Form2.config.Undercure)
                {
                    return "Curaga IV";
                }
                else
                {
                    return "false";
                }
            }
            else if (cureSpell.ToLower() == "curaga iv")
            {
                if (HasSpell("Curaga IV") && JobChecker("Curaga IV") == true && CheckSpellRecast("Curaga IV") == 0)
                {
                    return "Curaga IV";
                }
                else if (HasSpell("Curaga V") && JobChecker("Curaga V") == true && CheckSpellRecast("Curaga V") == 0 && Form2.config.Overcure)
                {
                    return "Curaga V";
                }
                else if (HasSpell("Curaga III") && JobChecker("Curaga III") == true && CheckSpellRecast("Curaga III") == 0 && Form2.config.Undercure)
                {
                    return "Curaga III";
                }
                else
                {
                    return "false";
                }
            }
            else if (cureSpell.ToLower() == "curaga iii")
            {
                if (HasSpell("Curaga III") && JobChecker("Curaga III") == true && CheckSpellRecast("Curaga III") == 0)
                {
                    return "Curaga III";
                }
                else if (HasSpell("Curaga IV") && JobChecker("Curaga IV") == true && CheckSpellRecast("Curaga IV") == 0 && Form2.config.Overcure)
                {
                    return "Curaga IV";
                }
                else if (HasSpell("Curaga II") && JobChecker("Curaga II") == true && CheckSpellRecast("Curaga II") == 0 && Form2.config.Undercure)
                {
                    return "Curaga II";
                }
                else
                {
                    return "false";
                }
            }
            else if (cureSpell.ToLower() == "curaga ii")
            {
                if (HasSpell("Curaga II") && JobChecker("Curaga II") == true && CheckSpellRecast("Curaga II") == 0)
                {
                    return "Curaga II";
                }
                else if (HasSpell("Curaga") && JobChecker("Curaga") == true && CheckSpellRecast("Curaga") == 0 && Form2.config.Undercure)
                {
                    return "Curaga";
                }
                else if (HasSpell("Curaga III") && JobChecker("Curaga III") == true && CheckSpellRecast("Curaga III") == 0 && Form2.config.Overcure)
                {
                    return "Curaga III";
                }
                else
                {
                    return "false";
                }
            }
            else if (cureSpell.ToLower() == "curaga")
            {
                if (HasSpell("Curaga") && JobChecker("Curaga") == true && CheckSpellRecast("Curaga") == 0)
                {
                    return "Curaga";
                }
                else if (HasSpell("Curaga II") && JobChecker("Curaga II") == true && CheckSpellRecast("Curaga II") == 0 && Form2.config.Overcure)
                {
                    return "Curaga II";
                }
                else
                {
                    return "false";
                }
            }
            return "false";
        }

        private bool partyMemberUpdateMethod(byte partyMemberId)
        {
            if (_ELITEAPIMonitored.Party.GetPartyMembers()[partyMemberId].Active >= 1)
            {
                if (_ELITEAPIPL.Player.ZoneId == _ELITEAPIMonitored.Party.GetPartyMembers()[partyMemberId].Zone)
                {
                    return true;
                }

                return false;
            }
            return false;
        }

        private async void partyMembersUpdate_TickAsync(object sender, EventArgs e)
        {
            if (_ELITEAPIPL == null || _ELITEAPIMonitored == null)
            {
                return;
            }

            if (_ELITEAPIPL.Player.LoginStatus == (int)LoginStatus.Loading || _ELITEAPIMonitored.Player.LoginStatus == (int)LoginStatus.Loading)
            {
                if (Form2.config.pauseOnZoneBox == true)
                {
                    song_casting = 0;
                    ForceSongRecast = true;
                    if (pauseActions != true)
                    {
                        pauseButton.Text = "Zoned, paused.";
                        pauseButton.ForeColor = Color.Red;
                        pauseActions = true;
                        actionTimer.Enabled = false;
                    }
                }
                else
                {
                    song_casting = 0;
                    ForceSongRecast = true;

                    if (pauseActions != true)
                    {
                        pauseButton.Text = "Zoned, waiting.";
                        pauseButton.ForeColor = Color.Red;
                        await Task.Delay(100);
                        Thread.Sleep(17000);
                        pauseButton.Text = "Pause";
                        pauseButton.ForeColor = Color.Black;
                    }
                }
                ActiveBuffs.Clear();
            }

            if (_ELITEAPIPL.Player.LoginStatus != (int)LoginStatus.LoggedIn || _ELITEAPIMonitored.Player.LoginStatus != (int)LoginStatus.LoggedIn)
            {
                return;
            }
            if (partyMemberUpdateMethod(0))
            {
                player0.Text = _ELITEAPIMonitored.Party.GetPartyMember(0).Name;
                player0.Enabled = true;
                player0optionsButton.Enabled = true;
                player0buffsButton.Enabled = true;
            }
            else
            {
                player0.Text = "Inactive or out of zone";
                player0.Enabled = false;
                player0HP.Value = 0;
                player0optionsButton.Enabled = false;
                player0buffsButton.Enabled = false;
            }

            if (partyMemberUpdateMethod(1))
            {
                player1.Text = _ELITEAPIMonitored.Party.GetPartyMember(1).Name;
                player1.Enabled = true;
                player1optionsButton.Enabled = true;
                player1buffsButton.Enabled = true;
            }
            else
            {
                player1.Text = Resources.Form1_partyMembersUpdate_Tick_Inactive;
                player1.Enabled = false;
                player1HP.Value = 0;
                player1optionsButton.Enabled = false;
                player1buffsButton.Enabled = false;
            }

            if (partyMemberUpdateMethod(2))
            {
                player2.Text = _ELITEAPIMonitored.Party.GetPartyMember(2).Name;
                player2.Enabled = true;
                player2optionsButton.Enabled = true;
                player2buffsButton.Enabled = true;
            }
            else
            {
                player2.Text = Resources.Form1_partyMembersUpdate_Tick_Inactive;
                player2.Enabled = false;
                player2HP.Value = 0;
                player2optionsButton.Enabled = false;
                player2buffsButton.Enabled = false;
            }

            if (partyMemberUpdateMethod(3))
            {
                player3.Text = _ELITEAPIMonitored.Party.GetPartyMember(3).Name;
                player3.Enabled = true;
                player3optionsButton.Enabled = true;
                player3buffsButton.Enabled = true;
            }
            else
            {
                player3.Text = Resources.Form1_partyMembersUpdate_Tick_Inactive;
                player3.Enabled = false;
                player3HP.Value = 0;
                player3optionsButton.Enabled = false;
                player3buffsButton.Enabled = false;
            }

            if (partyMemberUpdateMethod(4))
            {
                player4.Text = _ELITEAPIMonitored.Party.GetPartyMember(4).Name;
                player4.Enabled = true;
                player4optionsButton.Enabled = true;
                player4buffsButton.Enabled = true;
            }
            else
            {
                player4.Text = Resources.Form1_partyMembersUpdate_Tick_Inactive;
                player4.Enabled = false;
                player4HP.Value = 0;
                player4optionsButton.Enabled = false;
                player4buffsButton.Enabled = false;
            }

            if (partyMemberUpdateMethod(5))
            {
                player5.Text = _ELITEAPIMonitored.Party.GetPartyMember(5).Name;
                player5.Enabled = true;
                player5optionsButton.Enabled = true;
                player5buffsButton.Enabled = true;
            }
            else
            {
                player5.Text = Resources.Form1_partyMembersUpdate_Tick_Inactive;
                player5.Enabled = false;
                player5HP.Value = 0;
                player5optionsButton.Enabled = false;
                player5buffsButton.Enabled = false;
            }
            if (partyMemberUpdateMethod(6))
            {
                player6.Text = _ELITEAPIMonitored.Party.GetPartyMember(6).Name;
                player6.Enabled = true;
                player6optionsButton.Enabled = true;
            }
            else
            {
                player6.Text = Resources.Form1_partyMembersUpdate_Tick_Inactive;
                player6.Enabled = false;
                player6HP.Value = 0;
                player6optionsButton.Enabled = false;
            }

            if (partyMemberUpdateMethod(7))
            {
                player7.Text = _ELITEAPIMonitored.Party.GetPartyMember(7).Name;
                player7.Enabled = true;
                player7optionsButton.Enabled = true;
            }
            else
            {
                player7.Text = Resources.Form1_partyMembersUpdate_Tick_Inactive;
                player7.Enabled = false;
                player7HP.Value = 0;
                player7optionsButton.Enabled = false;
            }

            if (partyMemberUpdateMethod(8))
            {
                player8.Text = _ELITEAPIMonitored.Party.GetPartyMember(8).Name;
                player8.Enabled = true;
                player8optionsButton.Enabled = true;
            }
            else
            {
                player8.Text = Resources.Form1_partyMembersUpdate_Tick_Inactive;
                player8.Enabled = false;
                player8HP.Value = 0;
                player8optionsButton.Enabled = false;
            }

            if (partyMemberUpdateMethod(9))
            {
                player9.Text = _ELITEAPIMonitored.Party.GetPartyMember(9).Name;
                player9.Enabled = true;
                player9optionsButton.Enabled = true;
            }
            else
            {
                player9.Text = Resources.Form1_partyMembersUpdate_Tick_Inactive;
                player9.Enabled = false;
                player9HP.Value = 0;
                player9optionsButton.Enabled = false;
            }

            if (partyMemberUpdateMethod(10))
            {
                player10.Text = _ELITEAPIMonitored.Party.GetPartyMember(10).Name;
                player10.Enabled = true;
                player10optionsButton.Enabled = true;
            }
            else
            {
                player10.Text = Resources.Form1_partyMembersUpdate_Tick_Inactive;
                player10.Enabled = false;
                player10HP.Value = 0;
                player10optionsButton.Enabled = false;
            }

            if (partyMemberUpdateMethod(11))
            {
                player11.Text = _ELITEAPIMonitored.Party.GetPartyMember(11).Name;
                player11.Enabled = true;
                player11optionsButton.Enabled = true;
            }
            else
            {
                player11.Text = Resources.Form1_partyMembersUpdate_Tick_Inactive;
                player11.Enabled = false;
                player11HP.Value = 0;
                player11optionsButton.Enabled = false;
            }

            if (partyMemberUpdateMethod(12))
            {
                player12.Text = _ELITEAPIMonitored.Party.GetPartyMember(12).Name;
                player12.Enabled = true;
                player12optionsButton.Enabled = true;
            }
            else
            {
                player12.Text = Resources.Form1_partyMembersUpdate_Tick_Inactive;
                player12.Enabled = false;
                player12HP.Value = 0;
                player12optionsButton.Enabled = false;
            }

            if (partyMemberUpdateMethod(13))
            {
                player13.Text = _ELITEAPIMonitored.Party.GetPartyMember(13).Name;
                player13.Enabled = true;
                player13optionsButton.Enabled = true;
            }
            else
            {
                player13.Text = Resources.Form1_partyMembersUpdate_Tick_Inactive;
                player13.Enabled = false;
                player13HP.Value = 0;
                player13optionsButton.Enabled = false;
            }

            if (partyMemberUpdateMethod(14))
            {
                player14.Text = _ELITEAPIMonitored.Party.GetPartyMember(14).Name;
                player14.Enabled = true;
                player14optionsButton.Enabled = true;
            }
            else
            {
                player14.Text = Resources.Form1_partyMembersUpdate_Tick_Inactive;
                player14.Enabled = false;
                player14HP.Value = 0;
                player14optionsButton.Enabled = false;
            }

            if (partyMemberUpdateMethod(15))
            {
                player15.Text = _ELITEAPIMonitored.Party.GetPartyMember(15).Name;
                player15.Enabled = true;
                player15optionsButton.Enabled = true;
            }
            else
            {
                player15.Text = Resources.Form1_partyMembersUpdate_Tick_Inactive;
                player15.Enabled = false;
                player15HP.Value = 0;
                player15optionsButton.Enabled = false;
            }

            if (partyMemberUpdateMethod(16))
            {
                player16.Text = _ELITEAPIMonitored.Party.GetPartyMember(16).Name;
                player16.Enabled = true;
                player16optionsButton.Enabled = true;
            }
            else
            {
                player16.Text = Resources.Form1_partyMembersUpdate_Tick_Inactive;
                player16.Enabled = false;
                player16HP.Value = 0;
                player16optionsButton.Enabled = false;
            }

            if (partyMemberUpdateMethod(17))
            {
                player17.Text = _ELITEAPIMonitored.Party.GetPartyMember(17).Name;
                player17.Enabled = true;
                player17optionsButton.Enabled = true;
            }
            else
            {
                player17.Text = Resources.Form1_partyMembersUpdate_Tick_Inactive;
                player17.Enabled = false;
                player17HP.Value = 0;
                player17optionsButton.Enabled = false;
            }
        }

        private void hpUpdates_Tick(object sender, EventArgs e)
        {
            if (_ELITEAPIPL == null || _ELITEAPIMonitored == null)
            {
                return;
            }

            if (_ELITEAPIPL.Player.LoginStatus != (int)LoginStatus.LoggedIn || _ELITEAPIMonitored.Player.LoginStatus != (int)LoginStatus.LoggedIn)
            {
                return;
            }

            if (player0.Enabled)
            {
                UpdateHPProgressBar(player0HP, _ELITEAPIMonitored.Party.GetPartyMember(0).CurrentHPP);
            }

            if (player0.Enabled)
            {
                UpdateHPProgressBar(player0HP, _ELITEAPIMonitored.Party.GetPartyMember(0).CurrentHPP);
            }

            if (player1.Enabled)
            {
                UpdateHPProgressBar(player1HP, _ELITEAPIMonitored.Party.GetPartyMember(1).CurrentHPP);
            }

            if (player2.Enabled)
            {
                UpdateHPProgressBar(player2HP, _ELITEAPIMonitored.Party.GetPartyMember(2).CurrentHPP);
            }

            if (player3.Enabled)
            {
                UpdateHPProgressBar(player3HP, _ELITEAPIMonitored.Party.GetPartyMember(3).CurrentHPP);
            }

            if (player4.Enabled)
            {
                UpdateHPProgressBar(player4HP, _ELITEAPIMonitored.Party.GetPartyMember(4).CurrentHPP);
            }

            if (player5.Enabled)
            {
                UpdateHPProgressBar(player5HP, _ELITEAPIMonitored.Party.GetPartyMember(5).CurrentHPP);
            }

            if (player6.Enabled)
            {
                UpdateHPProgressBar(player6HP, _ELITEAPIMonitored.Party.GetPartyMember(6).CurrentHPP);
            }

            if (player7.Enabled)
            {
                UpdateHPProgressBar(player7HP, _ELITEAPIMonitored.Party.GetPartyMember(7).CurrentHPP);
            }

            if (player8.Enabled)
            {
                UpdateHPProgressBar(player8HP, _ELITEAPIMonitored.Party.GetPartyMember(8).CurrentHPP);
            }

            if (player9.Enabled)
            {
                UpdateHPProgressBar(player9HP, _ELITEAPIMonitored.Party.GetPartyMember(9).CurrentHPP);
            }

            if (player10.Enabled)
            {
                UpdateHPProgressBar(player10HP, _ELITEAPIMonitored.Party.GetPartyMember(10).CurrentHPP);
            }

            if (player11.Enabled)
            {
                UpdateHPProgressBar(player11HP, _ELITEAPIMonitored.Party.GetPartyMember(11).CurrentHPP);
            }

            if (player12.Enabled)
            {
                UpdateHPProgressBar(player12HP, _ELITEAPIMonitored.Party.GetPartyMember(12).CurrentHPP);
            }

            if (player13.Enabled)
            {
                UpdateHPProgressBar(player13HP, _ELITEAPIMonitored.Party.GetPartyMember(13).CurrentHPP);
            }

            if (player14.Enabled)
            {
                UpdateHPProgressBar(player14HP, _ELITEAPIMonitored.Party.GetPartyMember(14).CurrentHPP);
            }

            if (player15.Enabled)
            {
                UpdateHPProgressBar(player15HP, _ELITEAPIMonitored.Party.GetPartyMember(15).CurrentHPP);
            }

            if (player16.Enabled)
            {
                UpdateHPProgressBar(player16HP, _ELITEAPIMonitored.Party.GetPartyMember(16).CurrentHPP);
            }

            if (player17.Enabled)
            {
                UpdateHPProgressBar(player17HP, _ELITEAPIMonitored.Party.GetPartyMember(17).CurrentHPP);
            }
        }

        private void UpdateHPProgressBar(ProgressBar playerHP, int CurrentHPP)
        {
            playerHP.Value = CurrentHPP;
            if (CurrentHPP >= 75)
            {
                playerHP.ForeColor = Color.DarkGreen;
            }
            else if (CurrentHPP > 50 && CurrentHPP < 75)
            {
                playerHP.ForeColor = Color.Yellow;
            }
            else if (CurrentHPP > 25 && CurrentHPP < 50)
            {
                playerHP.ForeColor = Color.Orange;
            }
            else if (CurrentHPP < 25)
            {
                playerHP.ForeColor = Color.Red;
            }
        }

        private void plPosition_Tick(object sender, EventArgs e)
        {
            if (_ELITEAPIPL == null || _ELITEAPIMonitored == null)
            {
                return;
            }

            if (_ELITEAPIPL.Player.LoginStatus != (int)LoginStatus.LoggedIn || _ELITEAPIMonitored.Player.LoginStatus != (int)LoginStatus.LoggedIn)
            {
                return;
            }

            plX = _ELITEAPIPL.Player.X;
            plY = _ELITEAPIPL.Player.Y;
            plZ = _ELITEAPIPL.Player.Z;
        }

        private void removeDebuff(string characterName, int debuffID)
        {
            lock (ActiveBuffs)
            {
                foreach (BuffStorage ailment in ActiveBuffs)
                {
                    if (ailment.CharacterName.ToLower() == characterName.ToLower())
                    {
                        //MessageBox.Show("Found Match: " + ailment.CharacterName.ToLower()+" => "+characterName.ToLower());

                        // Build a new list, find cast debuff and remove it.
                        List<string> named_Debuffs = ailment.CharacterBuffs.Split(',').ToList();
                        named_Debuffs.Remove(debuffID.ToString());

                        // Now rebuild the list and replace previous one
                        string stringList = string.Join(",", named_Debuffs);

                        int i = ActiveBuffs.FindIndex(x => x.CharacterName.ToLower() == characterName.ToLower());
                        ActiveBuffs[i].CharacterBuffs = stringList;
                    }
                }
            }
        }

        private void CureCalculator_PL(bool HP)
        {
            // FIRST GET HOW MUCH HP IS MISSING FROM THE CURRENT PARTY MEMBER
            if (_ELITEAPIPL.Player.HP > 0)
            {
                uint HP_Loss = (_ELITEAPIPL.Player.HP * 100) / (_ELITEAPIPL.Player.HPP) - (_ELITEAPIPL.Player.HP);

                if (Form2.config.cure6enabled && HP_Loss >= Form2.config.cure6amount && _ELITEAPIPL.Player.MP > 227 && HasSpell("Cure VI") && JobChecker("Cure VI") == true)
                {
                    string cureSpell = CureTiers("Cure VI", HP);
                    if (cureSpell != "false")
                    {
                        CastSpell(_ELITEAPIPL.Player.Name, cureSpell);
                    }
                }
                else if (Form2.config.cure5enabled && HP_Loss >= Form2.config.cure5amount && _ELITEAPIPL.Player.MP > 125 && HasSpell("Cure V") && JobChecker("Cure V") == true)
                {
                    string cureSpell = CureTiers("Cure V", HP);
                    if (cureSpell != "false")
                    {
                        CastSpell(_ELITEAPIPL.Player.Name, cureSpell);
                    }
                }
                else if (Form2.config.cure4enabled && HP_Loss >= Form2.config.cure4amount && _ELITEAPIPL.Player.MP > 88 && HasSpell("Cure IV") && JobChecker("Cure IV") == true)
                {
                    string cureSpell = CureTiers("Cure IV", HP);
                    if (cureSpell != "false")
                    {
                        CastSpell(_ELITEAPIPL.Player.Name, cureSpell);
                    }
                }
                else if (Form2.config.cure3enabled && HP_Loss >= Form2.config.cure3amount && _ELITEAPIPL.Player.MP > 46 && HasSpell("Cure III") && JobChecker("Cure III") == true)
                {
                    if (Form2.config.PrioritiseOverLowerTier == true) { RunDebuffChecker(); }
                    string cureSpell = CureTiers("Cure III", HP);
                    if (cureSpell != "false")
                    {
                        CastSpell(_ELITEAPIPL.Player.Name, cureSpell);
                    }
                }
                else if (Form2.config.cure2enabled && HP_Loss >= Form2.config.cure2amount && _ELITEAPIPL.Player.MP > 24 && HasSpell("Cure II") && JobChecker("Cure II") == true)
                {
                    if (Form2.config.PrioritiseOverLowerTier == true) { RunDebuffChecker(); }
                    string cureSpell = CureTiers("Cure II", HP);
                    if (cureSpell != "false")
                    {
                        CastSpell(_ELITEAPIPL.Player.Name, cureSpell);
                    }
                }
                else if (Form2.config.cure1enabled && HP_Loss >= Form2.config.cure1amount && _ELITEAPIPL.Player.MP > 8 && HasSpell("Cure") && JobChecker("Cure") == true)
                {
                    if (Form2.config.PrioritiseOverLowerTier == true) { RunDebuffChecker(); }
                    string cureSpell = CureTiers("Cure", HP);
                    if (cureSpell != "false")
                    {
                        CastSpell(_ELITEAPIPL.Player.Name, cureSpell);
                    }
                }
            }
        }

        private void CureCalculator(byte partyMemberId, bool HP)
        {
            // FIRST GET HOW MUCH HP IS MISSING FROM THE CURRENT PARTY MEMBER
            if (_ELITEAPIMonitored.Party.GetPartyMembers()[partyMemberId].CurrentHP > 0)
            {
                uint HP_Loss = (_ELITEAPIMonitored.Party.GetPartyMembers()[partyMemberId].CurrentHP * 100) / (_ELITEAPIMonitored.Party.GetPartyMembers()[partyMemberId].CurrentHPP) - (_ELITEAPIMonitored.Party.GetPartyMembers()[partyMemberId].CurrentHP);

                if (Form2.config.cure6enabled && HP_Loss >= Form2.config.cure6amount && _ELITEAPIPL.Player.MP > 227 && HasSpell("Cure VI") && JobChecker("Cure VI") == true)
                {
                    string cureSpell = CureTiers("Cure VI", HP);
                    if (cureSpell != "false")
                    {
                        CastSpell(_ELITEAPIMonitored.Party.GetPartyMembers()[partyMemberId].Name, cureSpell);
                    }
                }
                else if (Form2.config.cure5enabled && HP_Loss >= Form2.config.cure5amount && _ELITEAPIPL.Player.MP > 125 && HasSpell("Cure V") && JobChecker("Cure V") == true)
                {
                    string cureSpell = CureTiers("Cure V", HP);
                    if (cureSpell != "false")
                    {
                        CastSpell(_ELITEAPIMonitored.Party.GetPartyMembers()[partyMemberId].Name, cureSpell);
                    }
                }
                else if (Form2.config.cure4enabled && HP_Loss >= Form2.config.cure4amount && _ELITEAPIPL.Player.MP > 88 && HasSpell("Cure IV") && JobChecker("Cure IV") == true)
                {
                    string cureSpell = CureTiers("Cure IV", HP);
                    if (cureSpell != "false")
                    {
                        CastSpell(_ELITEAPIMonitored.Party.GetPartyMembers()[partyMemberId].Name, cureSpell);
                    }
                }
                else if (Form2.config.cure3enabled && HP_Loss >= Form2.config.cure3amount && _ELITEAPIPL.Player.MP > 46 && HasSpell("Cure III") && JobChecker("Cure III") == true)
                {
                    if (Form2.config.PrioritiseOverLowerTier == true) { RunDebuffChecker(); }
                    string cureSpell = CureTiers("Cure III", HP);
                    if (cureSpell != "false")
                    {
                        CastSpell(_ELITEAPIMonitored.Party.GetPartyMembers()[partyMemberId].Name, cureSpell);
                    }
                }
                else if (Form2.config.cure2enabled && HP_Loss >= Form2.config.cure2amount && _ELITEAPIPL.Player.MP > 24 && HasSpell("Cure II") && JobChecker("Cure II") == true)
                {
                    if (Form2.config.PrioritiseOverLowerTier == true) { RunDebuffChecker(); }
                    string cureSpell = CureTiers("Cure II", HP);
                    if (cureSpell != "false")
                    {
                        CastSpell(_ELITEAPIMonitored.Party.GetPartyMembers()[partyMemberId].Name, cureSpell);
                    }
                }
                else if (Form2.config.cure1enabled && HP_Loss >= Form2.config.cure1amount && _ELITEAPIPL.Player.MP > 8 && HasSpell("Cure") && JobChecker("Cure") == true)
                {
                    if (Form2.config.PrioritiseOverLowerTier == true) { RunDebuffChecker(); }
                    string cureSpell = CureTiers("Cure", HP);
                    if (cureSpell != "false")
                    {
                        CastSpell(_ELITEAPIMonitored.Party.GetPartyMembers()[partyMemberId].Name, cureSpell);
                    }
                }
            }
        }

        private void RunDebuffChecker()
        {
            // PL and Monitored Player Debuff Removal Starting with PL
            if (_ELITEAPIPL.Player.Status != 33)
            {
                if (Form2.config.plSilenceItem == 0)
                {
                    plSilenceitemName = "Catholicon";
                }
                else if (Form2.config.plSilenceItem == 1)
                {
                    plSilenceitemName = "Echo Drops";
                }
                else if (Form2.config.plSilenceItem == 2)
                {
                    plSilenceitemName = "Remedy";
                }
                else if (Form2.config.plSilenceItem == 3)
                {
                    plSilenceitemName = "Remedy Ointment";
                }
                else if (Form2.config.plSilenceItem == 4)
                {
                    plSilenceitemName = "Vicar's Drink";
                }

                if (Form2.config.plDoomitem == 0)
                {
                    plDoomItemName = "Holy Water";
                }
                else if (Form2.config.plDoomitem == 1)
                {
                    plDoomItemName = "Hallowed Water";
                }

                if (Form2.config.wakeSleepSpell == 0)
                {
                    wakeSleepSpellName = "Cure";
                }
                else if (Form2.config.wakeSleepSpell == 1)
                {
                    wakeSleepSpellName = "Cura";
                }
                else if (Form2.config.wakeSleepSpell == 2)
                {
                    wakeSleepSpellName = "Curaga";
                }

                foreach (StatusEffect plEffect in _ELITEAPIPL.Player.Buffs)
                {
                    if ((plEffect == StatusEffect.Doom) && (Form2.config.plDoom) && (CheckSpellRecast("Cursna") == 0) && (HasSpell("Cursna")) && JobChecker("Cursna") == true)
                    {
                        CastSpell(_ELITEAPIPL.Player.Name, "Cursna");
                    }
                    else if ((plEffect == StatusEffect.Paralysis) && (Form2.config.plParalysis) && (CheckSpellRecast("Paralyna") == 0) && (HasSpell("Paralyna")) && JobChecker("Paralyna") == true)
                    {
                        CastSpell(_ELITEAPIPL.Player.Name, "Paralyna");
                    }
                    else if ((plEffect == StatusEffect.Amnesia) && (Form2.config.plAmnesia) && (CheckSpellRecast("Esuna") == 0) && (HasSpell("Esuna")) && JobChecker("Esuna") == true && BuffChecker(0, 418))
                    {
                        CastSpell(_ELITEAPIPL.Player.Name, "Esuna");
                    }
                    else if ((plEffect == StatusEffect.Poison) && (Form2.config.plPoison) && (CheckSpellRecast("Poisona") == 0) && (HasSpell("Poisona")) && JobChecker("Poisona") == true)
                    {
                        CastSpell(_ELITEAPIPL.Player.Name, "Poisona");
                    }
                    else if ((plEffect == StatusEffect.Attack_Down) && (Form2.config.plAttackDown) && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true)
                    {
                        CastSpell(_ELITEAPIPL.Player.Name, "Erase");
                    }
                    else if ((plEffect == StatusEffect.Blindness) && (Form2.config.plBlindness) && (CheckSpellRecast("Blindna") == 0) && (HasSpell("Blindna")) && JobChecker("Blindna") == true)
                    {
                        CastSpell(_ELITEAPIPL.Player.Name, "Blindna");
                    }
                    else if ((plEffect == StatusEffect.Bind) && (Form2.config.plBind) && (Form2.config.plAttackDown) && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true)
                    {
                        CastSpell(_ELITEAPIPL.Player.Name, "Erase");
                    }
                    else if ((plEffect == StatusEffect.Weight) && (Form2.config.plWeight) && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true)
                    {
                        CastSpell(_ELITEAPIPL.Player.Name, "Erase");
                    }
                    else if ((plEffect == StatusEffect.Slow) && (Form2.config.plSlow) && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true)
                    {
                        CastSpell(_ELITEAPIPL.Player.Name, "Erase");
                    }
                    else if ((plEffect == StatusEffect.Curse) && (Form2.config.plCurse) && (CheckSpellRecast("Cursna") == 0) && (HasSpell("Cursna")) && JobChecker("Cursna") == true)
                    {
                        CastSpell(_ELITEAPIPL.Player.Name, "Cursna");
                    }
                    else if ((plEffect == StatusEffect.Curse2) && (Form2.config.plCurse2) && (CheckSpellRecast("Cursna") == 0) && (HasSpell("Cursna")) && JobChecker("Cursna") == true)
                    {
                        CastSpell(_ELITEAPIPL.Player.Name, "Cursna");
                    }
                    else if ((plEffect == StatusEffect.Addle) && (Form2.config.plAddle) && (Form2.config.plAttackDown) && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true)
                    {
                        CastSpell(_ELITEAPIPL.Player.Name, "Erase");
                    }
                    else if ((plEffect == StatusEffect.Bane) && (Form2.config.plBane) && (CheckSpellRecast("Cursna") == 0) && (HasSpell("Cursna")) && JobChecker("Cursna") == true)
                    {
                        CastSpell(_ELITEAPIPL.Player.Name, "Cursna");
                    }
                    else if ((plEffect == StatusEffect.Plague) && (Form2.config.plPlague) && (CheckSpellRecast("Viruna") == 0) && (HasSpell("Viruna")) && JobChecker("Viruna") == true)
                    {
                        CastSpell(_ELITEAPIPL.Player.Name, "Viruna");
                    }
                    else if ((plEffect == StatusEffect.Disease) && (Form2.config.plDisease) && (CheckSpellRecast("Viruna") == 0) && (HasSpell("Viruna")) && JobChecker("Viruna") == true)
                    {
                        CastSpell(_ELITEAPIPL.Player.Name, "Viruna");
                    }
                    else if ((plEffect == StatusEffect.Burn) && (Form2.config.plBurn) && (Form2.config.plAttackDown) && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true)
                    {
                        CastSpell(_ELITEAPIPL.Player.Name, "Erase");
                    }
                    else if ((plEffect == StatusEffect.Frost) && (Form2.config.plFrost) && (Form2.config.plAttackDown) && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true)
                    {
                        CastSpell(_ELITEAPIPL.Player.Name, "Erase");
                    }
                    else if ((plEffect == StatusEffect.Choke) && (Form2.config.plChoke) && (Form2.config.plAttackDown) && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true)
                    {
                        CastSpell(_ELITEAPIPL.Player.Name, "Erase");
                    }
                    else if ((plEffect == StatusEffect.Rasp) && (Form2.config.plRasp) && (Form2.config.plAttackDown) && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true)
                    {
                        CastSpell(_ELITEAPIPL.Player.Name, "Erase");
                    }
                    else if ((plEffect == StatusEffect.Shock) && (Form2.config.plShock) && (Form2.config.plAttackDown) && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true)
                    {
                        CastSpell(_ELITEAPIPL.Player.Name, "Erase");
                    }
                    else if ((plEffect == StatusEffect.Drown) && (Form2.config.plDrown) && (Form2.config.plAttackDown) && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true)
                    {
                        CastSpell(_ELITEAPIPL.Player.Name, "Erase");
                    }
                    else if ((plEffect == StatusEffect.Dia) && (Form2.config.plDia) && (Form2.config.plAttackDown) && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true)
                    {
                        CastSpell(_ELITEAPIPL.Player.Name, "Erase");
                    }
                    else if ((plEffect == StatusEffect.Bio) && (Form2.config.plBio) && (Form2.config.plAttackDown) && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true)
                    {
                        CastSpell(_ELITEAPIPL.Player.Name, "Erase");
                    }
                    else if ((plEffect == StatusEffect.STR_Down) && (Form2.config.plStrDown) && (Form2.config.plAttackDown) && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true)
                    {
                        CastSpell(_ELITEAPIPL.Player.Name, "Erase");
                    }
                    else if ((plEffect == StatusEffect.DEX_Down) && (Form2.config.plDexDown) && (Form2.config.plAttackDown) && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true)
                    {
                        CastSpell(_ELITEAPIPL.Player.Name, "Erase");
                    }
                    else if ((plEffect == StatusEffect.VIT_Down) && (Form2.config.plVitDown) && (Form2.config.plAttackDown) && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true)
                    {
                        CastSpell(_ELITEAPIPL.Player.Name, "Erase");
                    }
                    else if ((plEffect == StatusEffect.AGI_Down) && (Form2.config.plAgiDown) && (Form2.config.plAttackDown) && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true)
                    {
                        CastSpell(_ELITEAPIPL.Player.Name, "Erase");
                    }
                    else if ((plEffect == StatusEffect.INT_Down) && (Form2.config.plIntDown) && (Form2.config.plAttackDown) && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true)
                    {
                        CastSpell(_ELITEAPIPL.Player.Name, "Erase");
                    }
                    else if ((plEffect == StatusEffect.MND_Down) && (Form2.config.plMndDown) && (Form2.config.plAttackDown) && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true)
                    {
                        CastSpell(_ELITEAPIPL.Player.Name, "Erase");
                    }
                    else if ((plEffect == StatusEffect.CHR_Down) && (Form2.config.plChrDown) && (Form2.config.plAttackDown) && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true)
                    {
                        CastSpell(_ELITEAPIPL.Player.Name, "Erase");
                    }
                    else if ((plEffect == StatusEffect.Max_HP_Down) && (Form2.config.plMaxHpDown) && (Form2.config.plAttackDown) && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true)
                    {
                        CastSpell(_ELITEAPIPL.Player.Name, "Erase");
                    }
                    else if ((plEffect == StatusEffect.Max_MP_Down) && (Form2.config.plMaxMpDown) && (Form2.config.plAttackDown) && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true)
                    {
                        CastSpell(_ELITEAPIPL.Player.Name, "Erase");
                    }
                    else if ((plEffect == StatusEffect.Accuracy_Down) && (Form2.config.plAccuracyDown) && (Form2.config.plAttackDown) && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true)
                    {
                        CastSpell(_ELITEAPIPL.Player.Name, "Erase");
                    }
                    else if ((plEffect == StatusEffect.Evasion_Down) && (Form2.config.plEvasionDown) && (Form2.config.plAttackDown) && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true)
                    {
                        CastSpell(_ELITEAPIPL.Player.Name, "Erase");
                    }
                    else if ((plEffect == StatusEffect.Defense_Down) && (Form2.config.plDefenseDown) && (Form2.config.plAttackDown) && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true)
                    {
                        CastSpell(_ELITEAPIPL.Player.Name, "Erase");
                    }
                    else if ((plEffect == StatusEffect.Flash) && (Form2.config.plFlash) && (Form2.config.plAttackDown) && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true)
                    {
                        CastSpell(_ELITEAPIPL.Player.Name, "Erase");
                    }
                    else if ((plEffect == StatusEffect.Magic_Acc_Down) && (Form2.config.plMagicAccDown) && (Form2.config.plAttackDown) && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true)
                    {
                        CastSpell(_ELITEAPIPL.Player.Name, "Erase");
                    }
                    else if ((plEffect == StatusEffect.Magic_Atk_Down) && (Form2.config.plMagicAtkDown) && (Form2.config.plAttackDown) && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true)
                    {
                        CastSpell(_ELITEAPIPL.Player.Name, "Erase");
                    }
                    else if ((plEffect == StatusEffect.Helix) && (Form2.config.plHelix) && (Form2.config.plAttackDown) && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true)
                    {
                        CastSpell(_ELITEAPIPL.Player.Name, "Erase");
                    }
                    else if ((plEffect == StatusEffect.Max_TP_Down) && (Form2.config.plMaxTpDown) && (Form2.config.plAttackDown) && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true)
                    {
                        CastSpell(_ELITEAPIPL.Player.Name, "Erase");
                    }
                    else if ((plEffect == StatusEffect.Requiem) && (Form2.config.plRequiem) && (Form2.config.plAttackDown) && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true)
                    {
                        CastSpell(_ELITEAPIPL.Player.Name, "Erase");
                    }
                    else if ((plEffect == StatusEffect.Elegy) && (Form2.config.plElegy) && (Form2.config.plAttackDown) && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true)
                    {
                        CastSpell(_ELITEAPIPL.Player.Name, "Erase");
                    }
                    else if ((plEffect == StatusEffect.Threnody) && (Form2.config.plThrenody) && (Form2.config.plAttackDown) && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true)
                    {
                        CastSpell(_ELITEAPIPL.Player.Name, "Erase");
                    }
                }
            }

            // Next, we check monitored player
            if ((_ELITEAPIPL.Entity.GetEntity((int)_ELITEAPIMonitored.Party.GetPartyMember(0).TargetIndex).Distance < 21) && (_ELITEAPIPL.Entity.GetEntity((int)_ELITEAPIMonitored.Party.GetPartyMember(0).TargetIndex).Distance > 0) && (_ELITEAPIMonitored.Player.HP > 0) && _ELITEAPIPL.Player.Status != 33)
            {
                foreach (StatusEffect monitoredEffect in _ELITEAPIMonitored.Player.Buffs)
                {
                    if ((monitoredEffect == StatusEffect.Doom) && (Form2.config.monitoredDoom) && (CheckSpellRecast("Cursna") == 0) && (HasSpell("Cursna")) && JobChecker("Cursna") == true)
                    {
                        CastSpell(_ELITEAPIMonitored.Player.Name, "Cursna");
                    }
                    else if ((monitoredEffect == StatusEffect.Sleep) && (Form2.config.monitoredSleep) && (Form2.config.wakeSleepEnabled))
                    {
                        CastSpell(_ELITEAPIMonitored.Player.Name, wakeSleepSpellName);
                    }
                    else if ((monitoredEffect == StatusEffect.Sleep2) && (Form2.config.monitoredSleep2) && (Form2.config.wakeSleepEnabled))
                    {
                        CastSpell(_ELITEAPIMonitored.Player.Name, wakeSleepSpellName);
                    }
                    else if ((monitoredEffect == StatusEffect.Silence) && (Form2.config.monitoredSilence) && (CheckSpellRecast("Silena") == 0) && (HasSpell("Silena")) && JobChecker("Silena") == true)
                    {
                        CastSpell(_ELITEAPIMonitored.Player.Name, "Silena");
                    }
                    else if ((monitoredEffect == StatusEffect.Petrification) && (Form2.config.monitoredPetrification) && (CheckSpellRecast("Stona") == 0) && (HasSpell("Stona")) && JobChecker("Stona") == true)
                    {
                        CastSpell(_ELITEAPIMonitored.Player.Name, "Stona");
                    }
                    else if ((monitoredEffect == StatusEffect.Paralysis) && (Form2.config.monitoredParalysis) && (CheckSpellRecast("Paralyna") == 0) && (HasSpell("Paralyna")) && JobChecker("Paralyna") == true)
                    {
                        CastSpell(_ELITEAPIMonitored.Player.Name, "Paralyna");
                    }
                    else if ((monitoredEffect == StatusEffect.Amnesia) && (Form2.config.monitoredAmnesia) && (CheckSpellRecast("Esuna") == 0) && (HasSpell("Esuna")) && JobChecker("Esuna") == true && BuffChecker(0, 418))
                    {
                        CastSpell(_ELITEAPIMonitored.Player.Name, "Esuna");
                    }
                    else if ((monitoredEffect == StatusEffect.Poison) && (Form2.config.monitoredPoison) && (CheckSpellRecast("Poisona") == 0) && (HasSpell("Poisona")) && JobChecker("Erase") == true)
                    {
                        CastSpell(_ELITEAPIMonitored.Player.Name, "Poisona");
                    }
                    else if ((monitoredEffect == StatusEffect.Attack_Down) && (Form2.config.monitoredAttackDown) && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true && plMonitoredSameParty() == true)
                    {
                        CastSpell(_ELITEAPIMonitored.Player.Name, "Erase");
                    }
                    else if ((monitoredEffect == StatusEffect.Blindness) && (Form2.config.monitoredBlindness) && (CheckSpellRecast("Blindna") == 0) && (HasSpell("Blindna")) && JobChecker("Blindna") == true)
                    {
                        CastSpell(_ELITEAPIMonitored.Player.Name, "Blindna");
                    }
                    else if ((monitoredEffect == StatusEffect.Bind) && (Form2.config.monitoredBind) && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true && plMonitoredSameParty() == true)
                    {
                        CastSpell(_ELITEAPIMonitored.Player.Name, "Erase");
                    }
                    else if ((monitoredEffect == StatusEffect.Weight) && (Form2.config.monitoredWeight) && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true && plMonitoredSameParty() == true)
                    {
                        CastSpell(_ELITEAPIMonitored.Player.Name, "Erase");
                    }
                    else if ((monitoredEffect == StatusEffect.Slow) && (Form2.config.monitoredSlow) && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true && plMonitoredSameParty() == true)
                    {
                        CastSpell(_ELITEAPIMonitored.Player.Name, "Erase");
                    }
                    else if ((monitoredEffect == StatusEffect.Curse) && (Form2.config.monitoredCurse) && (CheckSpellRecast("Cursna") == 0) && (HasSpell("Cursna")) && JobChecker("Cursna") == true)
                    {
                        CastSpell(_ELITEAPIMonitored.Player.Name, "Cursna");
                    }
                    else if ((monitoredEffect == StatusEffect.Curse2) && (Form2.config.monitoredCurse2) && (CheckSpellRecast("Cursna") == 0) && (HasSpell("Cursna")) && JobChecker("Cursna") == true)
                    {
                        CastSpell(_ELITEAPIMonitored.Player.Name, "Cursna");
                    }
                    else if ((monitoredEffect == StatusEffect.Addle) && (Form2.config.monitoredAddle) && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true && plMonitoredSameParty() == true)
                    {
                        CastSpell(_ELITEAPIMonitored.Player.Name, "Erase");
                    }
                    else if ((monitoredEffect == StatusEffect.Bane) && (Form2.config.monitoredBane) && (CheckSpellRecast("Cursna") == 0) && (HasSpell("Cursna")) && JobChecker("Cursna") == true)
                    {
                        CastSpell(_ELITEAPIMonitored.Player.Name, "Cursna");
                    }
                    else if ((monitoredEffect == StatusEffect.Plague) && (Form2.config.monitoredPlague) && (CheckSpellRecast("Viruna") == 0) && (HasSpell("Viruna")) && JobChecker("Viruna") == true)
                    {
                        CastSpell(_ELITEAPIMonitored.Player.Name, "Viruna");
                    }
                    else if ((monitoredEffect == StatusEffect.Disease) && (Form2.config.monitoredDisease) && (CheckSpellRecast("Viruna") == 0) && (HasSpell("Viruna")) && JobChecker("Viruna") == true)
                    {
                        CastSpell(_ELITEAPIMonitored.Player.Name, "Viruna");
                    }
                    else if ((monitoredEffect == StatusEffect.Burn) && (Form2.config.monitoredBurn) && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true && plMonitoredSameParty() == true)
                    {
                        CastSpell(_ELITEAPIMonitored.Player.Name, "Erase");
                    }
                    else if ((monitoredEffect == StatusEffect.Frost) && (Form2.config.monitoredFrost) && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true && plMonitoredSameParty() == true)
                    {
                        CastSpell(_ELITEAPIMonitored.Player.Name, "Erase");
                    }
                    else if ((monitoredEffect == StatusEffect.Choke) && (Form2.config.monitoredChoke) && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true && plMonitoredSameParty() == true)
                    {
                        CastSpell(_ELITEAPIMonitored.Player.Name, "Erase");
                    }
                    else if ((monitoredEffect == StatusEffect.Rasp) && (Form2.config.monitoredRasp) && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true && plMonitoredSameParty() == true)
                    {
                        CastSpell(_ELITEAPIMonitored.Player.Name, "Erase");
                    }
                    else if ((monitoredEffect == StatusEffect.Shock) && (Form2.config.monitoredShock) && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true && plMonitoredSameParty() == true)
                    {
                        CastSpell(_ELITEAPIMonitored.Player.Name, "Erase");
                    }
                    else if ((monitoredEffect == StatusEffect.Drown) && (Form2.config.monitoredDrown) && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true && plMonitoredSameParty() == true)
                    {
                        CastSpell(_ELITEAPIMonitored.Player.Name, "Erase");
                    }
                    else if ((monitoredEffect == StatusEffect.Dia) && (Form2.config.monitoredDia) && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true && plMonitoredSameParty() == true)
                    {
                        CastSpell(_ELITEAPIMonitored.Player.Name, "Erase");
                    }
                    else if ((monitoredEffect == StatusEffect.Bio) && (Form2.config.monitoredBio) && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true && plMonitoredSameParty() == true)
                    {
                        CastSpell(_ELITEAPIMonitored.Player.Name, "Erase");
                    }
                    else if ((monitoredEffect == StatusEffect.STR_Down) && (Form2.config.monitoredStrDown) && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true && plMonitoredSameParty() == true)
                    {
                        CastSpell(_ELITEAPIMonitored.Player.Name, "Erase");
                    }
                    else if ((monitoredEffect == StatusEffect.DEX_Down) && (Form2.config.monitoredDexDown) && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true && plMonitoredSameParty() == true)
                    {
                        CastSpell(_ELITEAPIMonitored.Player.Name, "Erase");
                    }
                    else if ((monitoredEffect == StatusEffect.VIT_Down) && (Form2.config.monitoredVitDown) && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true && plMonitoredSameParty() == true)
                    {
                        CastSpell(_ELITEAPIMonitored.Player.Name, "Erase");
                    }
                    else if ((monitoredEffect == StatusEffect.AGI_Down) && (Form2.config.monitoredAgiDown) && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true && plMonitoredSameParty() == true)
                    {
                        CastSpell(_ELITEAPIMonitored.Player.Name, "Erase");
                    }
                    else if ((monitoredEffect == StatusEffect.INT_Down) && (Form2.config.monitoredIntDown) && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true && plMonitoredSameParty() == true)
                    {
                        CastSpell(_ELITEAPIMonitored.Player.Name, "Erase");
                    }
                    else if ((monitoredEffect == StatusEffect.MND_Down) && (Form2.config.monitoredMndDown) && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true && plMonitoredSameParty() == true)
                    {
                        CastSpell(_ELITEAPIMonitored.Player.Name, "Erase");
                    }
                    else if ((monitoredEffect == StatusEffect.CHR_Down) && (Form2.config.monitoredChrDown) && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true && plMonitoredSameParty() == true)
                    {
                        CastSpell(_ELITEAPIMonitored.Player.Name, "Erase");
                    }
                    else if ((monitoredEffect == StatusEffect.Max_HP_Down) && (Form2.config.monitoredMaxHpDown) && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true && plMonitoredSameParty() == true)
                    {
                        CastSpell(_ELITEAPIMonitored.Player.Name, "Erase");
                    }
                    else if ((monitoredEffect == StatusEffect.Max_MP_Down) && (Form2.config.monitoredMaxMpDown) && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true && plMonitoredSameParty() == true)
                    {
                        CastSpell(_ELITEAPIMonitored.Player.Name, "Erase");
                    }
                    else if ((monitoredEffect == StatusEffect.Accuracy_Down) && (Form2.config.monitoredAccuracyDown) && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true && plMonitoredSameParty() == true)
                    {
                        CastSpell(_ELITEAPIMonitored.Player.Name, "Erase");
                    }
                    else if ((monitoredEffect == StatusEffect.Evasion_Down) && (Form2.config.monitoredEvasionDown) && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true && plMonitoredSameParty() == true)
                    {
                        CastSpell(_ELITEAPIMonitored.Player.Name, "Erase");
                    }
                    else if ((monitoredEffect == StatusEffect.Defense_Down) && (Form2.config.monitoredDefenseDown) && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true && plMonitoredSameParty() == true)
                    {
                        CastSpell(_ELITEAPIMonitored.Player.Name, "Erase");
                    }
                    else if ((monitoredEffect == StatusEffect.Flash) && (Form2.config.monitoredFlash) && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true && plMonitoredSameParty() == true)
                    {
                        CastSpell(_ELITEAPIMonitored.Player.Name, "Erase");
                    }
                    else if ((monitoredEffect == StatusEffect.Magic_Acc_Down) && (Form2.config.monitoredMagicAccDown) && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true && plMonitoredSameParty() == true)
                    {
                        CastSpell(_ELITEAPIMonitored.Player.Name, "Erase");
                    }
                    else if ((monitoredEffect == StatusEffect.Magic_Atk_Down) && (Form2.config.monitoredMagicAtkDown) && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true && plMonitoredSameParty() == true)
                    {
                        CastSpell(_ELITEAPIMonitored.Player.Name, "Erase");
                    }
                    else if ((monitoredEffect == StatusEffect.Helix) && (Form2.config.monitoredHelix) && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true && plMonitoredSameParty() == true)
                    {
                        CastSpell(_ELITEAPIMonitored.Player.Name, "Erase");
                    }
                    else if ((monitoredEffect == StatusEffect.Max_TP_Down) && (Form2.config.monitoredMaxTpDown) && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true && plMonitoredSameParty() == true)
                    {
                        CastSpell(_ELITEAPIMonitored.Player.Name, "Erase");
                    }
                    else if ((monitoredEffect == StatusEffect.Requiem) && (Form2.config.monitoredRequiem) && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true && plMonitoredSameParty() == true)
                    {
                        CastSpell(_ELITEAPIMonitored.Player.Name, "Erase");
                    }
                    else if ((monitoredEffect == StatusEffect.Elegy) && (Form2.config.monitoredElegy) && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true && plMonitoredSameParty() == true)
                    {
                        CastSpell(_ELITEAPIMonitored.Player.Name, "Erase");
                    }
                    else if ((monitoredEffect == StatusEffect.Threnody) && (Form2.config.monitoredThrenody) && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true && plMonitoredSameParty() == true)
                    {
                        CastSpell(_ELITEAPIMonitored.Player.Name, "Erase");
                    }
                }
            }
            // End MONITORED Debuff Removal


            if (Form2.config.EnableAddOn)
            {
                int BreakOut = 0;

                List<EliteAPI.PartyMember> partyMembers = _ELITEAPIPL.Party.GetPartyMembers();

                List<BuffStorage> generated_base_list = ActiveBuffs.ToList();

                lock (generated_base_list)
                {

                    foreach (BuffStorage ailment in generated_base_list)
                    {
                        foreach (EliteAPI.PartyMember ptMember in partyMembers)
                        {
                            if (ailment != null && ptMember != null)
                            {
                                if (ailment.CharacterName != null && ptMember.Name != null && ailment.CharacterName.ToLower() == ptMember.Name.ToLower())
                                {
                                    if (ailment.CharacterBuffs != null)
                                    {
                                        List<string> named_Debuffs = ailment.CharacterBuffs.Split(',').ToList();

                                        if (named_Debuffs != null && named_Debuffs.Count() != 0)
                                        {
                                            named_Debuffs = named_Debuffs.Select(t => t.Trim()).ToList();


                                            // IF SLOW IS NOT ACTIVE, YET NEITHER IS HASTE / FLURRY DESPITE BEING ENABLED
                                            // RESET THE TIMER TO FORCE IT TO BE CAST
                                            if (!DebuffContains(named_Debuffs, "13") && !DebuffContains(named_Debuffs, "33") && !DebuffContains(named_Debuffs, "265") && !DebuffContains(named_Debuffs, "562"))
                                            {
                                                if (ptMember != null)
                                                {
                                                    playerHaste[ptMember.MemberNumber] = new DateTime(1970, 1, 1, 0, 0, 0);
                                                    playerHaste_II[ptMember.MemberNumber] = new DateTime(1970, 1, 1, 0, 0, 0);
                                                    playerFlurry[ptMember.MemberNumber] = new DateTime(1970, 1, 1, 0, 0, 0);
                                                    playerFlurry_II[ptMember.MemberNumber] = new DateTime(1970, 1, 1, 0, 0, 0);
                                                }
                                            }
                                            // IF SUBLIMATION IS NOT ACTIVE, YET NEITHER IS REFRESH DESPITE BEING
                                            // ENABLED RESET THE TIMER TO FORCE IT TO BE CAST
                                            if (!DebuffContains(named_Debuffs, "187") && !DebuffContains(named_Debuffs, "188") && !DebuffContains(named_Debuffs, "43"))
                                            {
                                                if (ptMember != null)
                                                {
                                                    playerRefresh[ptMember.MemberNumber] = new DateTime(1970, 1, 1, 0, 0, 0);  // ERROR
                                                }
                                            }
                                            // IF REGEN IS NOT ACTIVE DESPITE BEING ENABLED RESET THE TIMER TO
                                            // FORCE IT TO BE CAST
                                            if (!DebuffContains(named_Debuffs, "42"))
                                            {
                                                if (ptMember != null)
                                                {
                                                    playerRegen[ptMember.MemberNumber] = new DateTime(1970, 1, 1, 0, 0, 0);
                                                }
                                            }
                                            // IF PROTECT IS NOT ACTIVE DESPITE BEING ENABLED RESET THE TIMER TO
                                            // FORCE IT TO BE CAST
                                            if (!DebuffContains(named_Debuffs, "40"))
                                            {
                                                if (ptMember != null)
                                                {
                                                    playerProtect[ptMember.MemberNumber] = new DateTime(1970, 1, 1, 0, 0, 0);
                                                }
                                            }

                                            // IF SHELL IS NOT ACTIVE DESPITE BEING ENABLED RESET THE TIMER TO
                                            // FORCE IT TO BE CAST
                                            if (!DebuffContains(named_Debuffs, "41"))
                                            {
                                                if (ptMember != null)
                                                {
                                                    playerShell[ptMember.MemberNumber] = new DateTime(1970, 1, 1, 0, 0, 0);
                                                }
                                            }
                                            // IF PHALANX II IS NOT ACTIVE DESPITE BEING ENABLED RESET THE TIMER
                                            // TO FORCE IT TO BE CAST
                                            if (!DebuffContains(named_Debuffs, "116"))
                                            {
                                                if (ptMember != null)
                                                {
                                                    playerPhalanx_II[ptMember.MemberNumber] = new DateTime(1970, 1, 1, 0, 0, 0);
                                                }

                                            }
                                            // IF NO STORM SPELL IS ACTIVE DESPITE BEING ENABLED RESET THE TIMER
                                            // TO FORCE IT TO BE CAST
                                            if (!DebuffContains(named_Debuffs, "178") && !DebuffContains(named_Debuffs, "179") && !DebuffContains(named_Debuffs, "180") && !DebuffContains(named_Debuffs, "181") &&
                                                !DebuffContains(named_Debuffs, "182") && !DebuffContains(named_Debuffs, "183") && !DebuffContains(named_Debuffs, "184") && !DebuffContains(named_Debuffs, "185") &&
                                                !DebuffContains(named_Debuffs, "589") && !DebuffContains(named_Debuffs, "590") && !DebuffContains(named_Debuffs, "591") && !DebuffContains(named_Debuffs, "592") &&
                                                !DebuffContains(named_Debuffs, "593") && !DebuffContains(named_Debuffs, "594") && !DebuffContains(named_Debuffs, "595") && !DebuffContains(named_Debuffs, "596"))
                                            {
                                                if (ptMember != null)
                                                {
                                                    playerStormspell[ptMember.MemberNumber] = new DateTime(1970, 1, 1, 0, 0, 0);
                                                }
                                            }


                                            // ==============================================================================================================================================================================
                                            // PARTY DEBUFF REMOVAL



                                            string character_name = ailment.CharacterName.ToLower();

                                            if (Form2.config.enablePartyDebuffRemoval && !string.IsNullOrEmpty(character_name) && (characterNames_naRemoval.Contains(character_name) || Form2.config.SpecifiednaSpellsenable == false))
                                            {
                                                //DOOM
                                                if (Form2.config.naCurse && DebuffContains(named_Debuffs, "15") && (CheckSpellRecast("Cursna") == 0) && (HasSpell("Cursna")) && JobChecker("Cursna") == true)
                                                {
                                                    CastSpell(ptMember.Name, "Cursna");
                                                    BreakOut = 1;
                                                }
                                                //SLEEP
                                                else if (DebuffContains(named_Debuffs, "2") && (CheckSpellRecast(wakeSleepSpellName) == 0) && (HasSpell(wakeSleepSpellName)))
                                                {
                                                    CastSpell(ptMember.Name, wakeSleepSpellName);
                                                    removeDebuff(ptMember.Name, 2);
                                                    BreakOut = 1;
                                                }
                                                //SLEEP 2
                                                else if (DebuffContains(named_Debuffs, "19") && (CheckSpellRecast(wakeSleepSpellName) == 0) && (HasSpell(wakeSleepSpellName)))
                                                {
                                                    CastSpell(ptMember.Name, wakeSleepSpellName);
                                                    removeDebuff(ptMember.Name, 19);
                                                    BreakOut = 1;
                                                }
                                                //PETRIFICATION
                                                else if (Form2.config.naPetrification && DebuffContains(named_Debuffs, "7") && (CheckSpellRecast("Stona") == 0) && (HasSpell("Stona")) && JobChecker("Stona") == true)
                                                {
                                                    CastSpell(ptMember.Name, "Stona");
                                                    removeDebuff(ptMember.Name, 7);
                                                    BreakOut = 1;
                                                }
                                                //SILENCE
                                                else if (Form2.config.naSilence && DebuffContains(named_Debuffs, "6") && (CheckSpellRecast("Silena") == 0) && (HasSpell("Silena")) && JobChecker("Silena") == true)
                                                {
                                                    CastSpell(ptMember.Name, "Silena");
                                                    removeDebuff(ptMember.Name, 6);
                                                    BreakOut = 1;
                                                }
                                                //PARALYSIS
                                                else if (Form2.config.naParalysis && DebuffContains(named_Debuffs, "4") && (CheckSpellRecast("Paralyna") == 0) && (HasSpell("Paralyna")) && JobChecker("Paralyna") == true)
                                                {
                                                    CastSpell(ptMember.Name, "Paralyna");
                                                    removeDebuff(ptMember.Name, 4);
                                                    BreakOut = 1;
                                                }
                                                // PLAGUE
                                                else if (Form2.config.naDisease && DebuffContains(named_Debuffs, "31") && (CheckSpellRecast("Viruna") == 0) && (HasSpell("Viruna")) && JobChecker("Viruna") == true)
                                                {
                                                    CastSpell(ptMember.Name, "Viruna");
                                                    removeDebuff(ptMember.Name, 31);
                                                    BreakOut = 1;
                                                }
                                                //DISEASE
                                                else if (Form2.config.naDisease && DebuffContains(named_Debuffs, "8") && (CheckSpellRecast("Viruna") == 0) && (HasSpell("Viruna")) && JobChecker("Viruna") == true)
                                                {
                                                    CastSpell(ptMember.Name, "Viruna");
                                                    removeDebuff(ptMember.Name, 8);
                                                    BreakOut = 1;
                                                }
                                                // AMNESIA
                                                else if (Form2.config.Esuna && DebuffContains(named_Debuffs, "16") && (CheckSpellRecast("Esuna") == 0) && (HasSpell("Esuna")) && JobChecker("Esuna") == true && BuffChecker(1, 418))
                                                {
                                                    CastSpell(ptMember.Name, "Esuna");
                                                    removeDebuff(ptMember.Name, 16);
                                                    BreakOut = 1;
                                                }
                                                //CURSE
                                                else if (Form2.config.naCurse && DebuffContains(named_Debuffs, "9") && (CheckSpellRecast("Cursna") == 0) && (HasSpell("Cursna")) && JobChecker("Cursna") == true)
                                                {
                                                    CastSpell(ptMember.Name, "Cursna");
                                                    removeDebuff(ptMember.Name, 9);
                                                    BreakOut = 1;
                                                }
                                                //BLINDNESS
                                                else if (Form2.config.naBlindness && DebuffContains(named_Debuffs, "5") && (CheckSpellRecast("Blindna") == 0) && (HasSpell("Blindna")) && JobChecker("Blindna") == true)
                                                {
                                                    CastSpell(ptMember.Name, "Blindna");
                                                    removeDebuff(ptMember.Name, 5);
                                                    BreakOut = 1;
                                                }
                                                //POISON
                                                else if (Form2.config.naPoison && DebuffContains(named_Debuffs, "3") && (CheckSpellRecast("Poisona") == 0) && (HasSpell("Poisona")) && JobChecker("Poisona") == true)
                                                {
                                                    CastSpell(ptMember.Name, "Poisona");
                                                    removeDebuff(ptMember.Name, 3);
                                                    BreakOut = 1;
                                                }
                                                // SLOW
                                                else if (Form2.config.naErase == true && Form2.config.na_Slow && DebuffContains(named_Debuffs, "13") && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true)
                                                {
                                                    CastSpell(ptMember.Name, "Erase", "Slow → " + ptMember.Name);
                                                    removeDebuff(ptMember.Name, 13);
                                                    BreakOut = 1;
                                                }
                                                // BIO
                                                else if (Form2.config.naErase == true && Form2.config.na_Bio && DebuffContains(named_Debuffs, "135") && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true)
                                                {
                                                    CastSpell(ptMember.Name, "Erase", "Bio → " + ptMember.Name);
                                                    removeDebuff(ptMember.Name, 135);
                                                    BreakOut = 1;
                                                }
                                                // BIND
                                                else if (Form2.config.naErase == true && Form2.config.na_Bind && DebuffContains(named_Debuffs, "11") && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true)
                                                {
                                                    CastSpell(ptMember.Name, "Erase", "Bind → " + ptMember.Name);
                                                    removeDebuff(ptMember.Name, 11);
                                                    BreakOut = 1;
                                                }
                                                // GRAVITY
                                                else if (Form2.config.naErase == true && Form2.config.na_Weight && DebuffContains(named_Debuffs, "12") && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true)
                                                {
                                                    CastSpell(ptMember.Name, "Erase", "Gravity → " + ptMember.Name);
                                                    removeDebuff(ptMember.Name, 12);
                                                    BreakOut = 1;
                                                }
                                                // ACCURACY DOWN
                                                else if (Form2.config.naErase == true && Form2.config.na_AccuracyDown && DebuffContains(named_Debuffs, "146") && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true)
                                                {
                                                    CastSpell(ptMember.Name, "Erase", "Acc. Down → " + ptMember.Name);
                                                    removeDebuff(ptMember.Name, 146);
                                                    BreakOut = 1;
                                                }
                                                // DEFENSE DOWN
                                                else if (Form2.config.naErase == true && Form2.config.na_DefenseDown && DebuffContains(named_Debuffs, "149") && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true)
                                                {
                                                    CastSpell(ptMember.Name, "Erase", "Def. Down → " + ptMember.Name);
                                                    removeDebuff(ptMember.Name, 149);
                                                    BreakOut = 1;
                                                }
                                                // MAGIC DEF DOWN
                                                else if (Form2.config.naErase == true && Form2.config.na_MagicDefenseDown && DebuffContains(named_Debuffs, "167") && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true)
                                                {
                                                    CastSpell(ptMember.Name, "Erase", "Mag. Def. Down → " + ptMember.Name);
                                                    removeDebuff(ptMember.Name, 167);
                                                    BreakOut = 1;
                                                }
                                                // ATTACK DOWN
                                                else if (Form2.config.naErase == true && Form2.config.na_AttackDown && DebuffContains(named_Debuffs, "147") && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true)
                                                {
                                                    CastSpell(ptMember.Name, "Erase", "Attk. Down → " + ptMember.Name);
                                                    removeDebuff(ptMember.Name, 147);
                                                    BreakOut = 1;
                                                }
                                                // HP DOWN
                                                else if (Form2.config.naErase == true && Form2.config.na_MaxHpDown && DebuffContains(named_Debuffs, "144") && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true)
                                                {
                                                    CastSpell(ptMember.Name, "Erase", "HP Down → " + ptMember.Name);
                                                    removeDebuff(ptMember.Name, 144);
                                                    BreakOut = 1;
                                                }
                                                // VIT Down
                                                else if (Form2.config.naErase == true && Form2.config.na_VitDown && DebuffContains(named_Debuffs, "138") && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true)
                                                {
                                                    CastSpell(ptMember.Name, "Erase", "VIT Down → " + ptMember.Name);
                                                    removeDebuff(ptMember.Name, 138);
                                                    BreakOut = 1;
                                                }
                                                // Threnody
                                                else if (Form2.config.naErase == true && Form2.config.na_Threnody && DebuffContains(named_Debuffs, "217") && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true)
                                                {
                                                    CastSpell(ptMember.Name, "Erase", "Threnody → " + ptMember.Name);
                                                    removeDebuff(ptMember.Name, 217);
                                                    BreakOut = 1;
                                                }
                                                // Shock
                                                else if (Form2.config.naErase == true && Form2.config.na_Shock && DebuffContains(named_Debuffs, "132") && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true)
                                                {
                                                    CastSpell(ptMember.Name, "Erase", "Shock → " + ptMember.Name);
                                                    removeDebuff(ptMember.Name, 132);
                                                    BreakOut = 1;
                                                }
                                                // StrDown
                                                else if (Form2.config.naErase == true && Form2.config.na_StrDown && DebuffContains(named_Debuffs, "136") && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true)
                                                {
                                                    CastSpell(ptMember.Name, "Erase", "STR Down → " + ptMember.Name);
                                                    removeDebuff(ptMember.Name, 136);
                                                    BreakOut = 1;
                                                }
                                                // Requiem
                                                else if (Form2.config.naErase == true && Form2.config.na_Requiem && DebuffContains(named_Debuffs, "192") && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true)
                                                {
                                                    CastSpell(ptMember.Name, "Erase", "Requiem → " + ptMember.Name);
                                                    removeDebuff(ptMember.Name, 192);
                                                    BreakOut = 1;
                                                }
                                                // Rasp
                                                else if (Form2.config.naErase == true && Form2.config.na_Rasp && DebuffContains(named_Debuffs, "131") && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true)
                                                {
                                                    CastSpell(ptMember.Name, "Erase", "Rasp → " + ptMember.Name);
                                                    removeDebuff(ptMember.Name, 131);
                                                    BreakOut = 1;
                                                }
                                                // Max TP Down
                                                else if (Form2.config.naErase == true && Form2.config.na_MaxTpDown && DebuffContains(named_Debuffs, "189") && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true)
                                                {
                                                    CastSpell(ptMember.Name, "Erase", "Max TP Down → " + ptMember.Name);
                                                    removeDebuff(ptMember.Name, 189);
                                                    BreakOut = 1;
                                                }
                                                // Max MP Down
                                                else if (Form2.config.naErase == true && Form2.config.na_MaxMpDown && DebuffContains(named_Debuffs, "145") && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true)
                                                {
                                                    CastSpell(ptMember.Name, "Erase", "Max MP Down → " + ptMember.Name);
                                                    removeDebuff(ptMember.Name, 145);
                                                    BreakOut = 1;
                                                }
                                                // Magic Attack Down
                                                else if (Form2.config.naErase == true && Form2.config.na_MagicAttackDown && DebuffContains(named_Debuffs, "175") && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true)
                                                {
                                                    CastSpell(ptMember.Name, "Erase", "Mag. Atk. Down → " + ptMember.Name);
                                                    removeDebuff(ptMember.Name, 175);
                                                    BreakOut = 1;
                                                }
                                                // Magic Acc Down
                                                else if (Form2.config.naErase == true && Form2.config.na_MagicAccDown && DebuffContains(named_Debuffs, "174") && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true)
                                                {
                                                    CastSpell(ptMember.Name, "Erase", "Mag. Acc. Down → " + ptMember.Name);
                                                    removeDebuff(ptMember.Name, 174);
                                                    BreakOut = 1;
                                                }
                                                // Mind Down
                                                else if (Form2.config.naErase == true && Form2.config.na_MndDown && DebuffContains(named_Debuffs, "141") && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true)
                                                {
                                                    CastSpell(ptMember.Name, "Erase", "MND Down → " + ptMember.Name);
                                                    removeDebuff(ptMember.Name, 141);
                                                    BreakOut = 1;
                                                }
                                                // Int Down
                                                else if (Form2.config.naErase == true && Form2.config.na_IntDown && DebuffContains(named_Debuffs, "140") && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true)
                                                {
                                                    CastSpell(ptMember.Name, "Erase", "INT Down → " + ptMember.Name);
                                                    removeDebuff(ptMember.Name, 140);
                                                    BreakOut = 1;
                                                }
                                                // Helix
                                                else if (Form2.config.naErase == true && Form2.config.na_Helix && DebuffContains(named_Debuffs, "186") && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true)
                                                {
                                                    CastSpell(ptMember.Name, "Erase", "Helix → " + ptMember.Name);
                                                    removeDebuff(ptMember.Name, 186);
                                                    BreakOut = 1;
                                                }
                                                // Frost
                                                else if (Form2.config.naErase == true && Form2.config.na_Frost && DebuffContains(named_Debuffs, "129") && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true)
                                                {
                                                    CastSpell(ptMember.Name, "Erase", "Frost → " + ptMember.Name);
                                                    removeDebuff(ptMember.Name, 129);
                                                    BreakOut = 1;
                                                }
                                                // EvasionDown
                                                else if (Form2.config.naErase == true && Form2.config.na_EvasionDown && DebuffContains(named_Debuffs, "148") && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true)
                                                {
                                                    CastSpell(ptMember.Name, "Erase", "Evasion Down → " + ptMember.Name);
                                                    removeDebuff(ptMember.Name, 148);
                                                    BreakOut = 1;
                                                }
                                                // ELEGY
                                                else if (Form2.config.naErase == true && Form2.config.na_Elegy && DebuffContains(named_Debuffs, "194") && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true)
                                                {
                                                    CastSpell(ptMember.Name, "Erase", "Elegy → " + ptMember.Name);
                                                    removeDebuff(ptMember.Name, 194);
                                                    BreakOut = 1;
                                                }
                                                // Drown
                                                else if (Form2.config.naErase == true && Form2.config.na_Drown && DebuffContains(named_Debuffs, "133") && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true)
                                                {
                                                    CastSpell(ptMember.Name, "Erase", "Drown → " + ptMember.Name);
                                                    removeDebuff(ptMember.Name, 133);
                                                    BreakOut = 1;
                                                }
                                                // Dia
                                                else if (Form2.config.naErase == true && Form2.config.na_Dia && DebuffContains(named_Debuffs, "134") && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true)
                                                {
                                                    CastSpell(ptMember.Name, "Erase", "Dia → " + ptMember.Name);
                                                    removeDebuff(ptMember.Name, 134);
                                                    BreakOut = 1;
                                                }
                                                // DexDown
                                                else if (Form2.config.naErase == true && Form2.config.na_DexDown && DebuffContains(named_Debuffs, "137") && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true)
                                                {
                                                    CastSpell(ptMember.Name, "Erase", "DEX Down → " + ptMember.Name);
                                                    removeDebuff(ptMember.Name, 137);
                                                    BreakOut = 1;
                                                }
                                                // Choke
                                                else if (Form2.config.naErase == true && Form2.config.na_Choke && DebuffContains(named_Debuffs, "130") && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true)
                                                {
                                                    CastSpell(ptMember.Name, "Erase", "Choke → " + ptMember.Name);
                                                    removeDebuff(ptMember.Name, 130);
                                                    BreakOut = 1;
                                                }
                                                // ChrDown
                                                else if (Form2.config.naErase == true && Form2.config.na_ChrDown && DebuffContains(named_Debuffs, "142") && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true)
                                                {
                                                    CastSpell(ptMember.Name, "Erase", "CHR Down → " + ptMember.Name);
                                                    removeDebuff(ptMember.Name, 142);
                                                    BreakOut = 1;
                                                }
                                                // Burn
                                                else if (Form2.config.naErase == true && Form2.config.na_Burn && DebuffContains(named_Debuffs, "128") && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true)
                                                {
                                                    CastSpell(ptMember.Name, "Erase", "Burn → " + ptMember.Name);
                                                    removeDebuff(ptMember.Name, 128);
                                                    BreakOut = 1;
                                                }
                                                // Addle
                                                else if (Form2.config.naErase == true && Form2.config.na_Addle && DebuffContains(named_Debuffs, "21") && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true)
                                                {
                                                    CastSpell(ptMember.Name, "Erase", "Addle → " + ptMember.Name);
                                                    removeDebuff(ptMember.Name, 21);
                                                    BreakOut = 1;
                                                }
                                                // AGI Down
                                                else if (Form2.config.naErase == true && Form2.config.na_AgiDown && DebuffContains(named_Debuffs, "139") && (CheckSpellRecast("Erase") == 0) && (HasSpell("Erase")) && JobChecker("Erase") == true)
                                                {
                                                    CastSpell(ptMember.Name, "Erase", "AGI Down → " + ptMember.Name);
                                                    removeDebuff(ptMember.Name, 139);
                                                    BreakOut = 1;
                                                }
                                            }
                                        }


                                    }


                                }

                                if (BreakOut == 1)
                                {
                                    break;
                                }
                            }
                        }
                    } // Closing FOREACH base_list
                }// Closing LOCK
            }





















        }

        private bool DebuffContains(List<string> Debuff_list, string Checked_id)
        {
            if (Debuff_list != null)
            {
                if (Debuff_list.Any(x => x == Checked_id))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        private void CuragaCalculatorAsync(int partyMemberId)
        {
            string lowestHP_Name = _ELITEAPIMonitored.Party.GetPartyMembers()[partyMemberId].Name;

            if (_ELITEAPIMonitored.Party.GetPartyMembers()[partyMemberId].CurrentHP > 0)
            {
                if ((Form2.config.curaga5enabled) && ((((_ELITEAPIMonitored.Party.GetPartyMembers()[partyMemberId].CurrentHP * 100) / _ELITEAPIMonitored.Party.GetPartyMembers()[partyMemberId].CurrentHPP) - _ELITEAPIMonitored.Party.GetPartyMembers()[partyMemberId].CurrentHP) >= Form2.config.curaga5Amount) && (_ELITEAPIPL.Player.MP > 380) && HasSpell("Curaga V") && JobChecker("Curaga V") == true)
                {
                    string cureSpell = CureTiers("Curaga V", false);
                    if (cureSpell != "false")
                    {
                        if (Form2.config.curagaTargetType == 0)
                        {
                            CastSpell(lowestHP_Name, cureSpell);
                        }
                        else
                        {
                            CastSpell(Form2.config.curagaTargetName, cureSpell);
                        }
                    }
                }
                else if (((Form2.config.curaga4enabled && HasSpell("Curaga IV") && JobChecker("Curaga IV") == true) || (Form2.config.Accession && Form2.config.accessionCure && HasSpell("Cure IV") && JobChecker("Cure IV") == true)) && ((((_ELITEAPIMonitored.Party.GetPartyMembers()[partyMemberId].CurrentHP * 100) / _ELITEAPIMonitored.Party.GetPartyMembers()[partyMemberId].CurrentHPP) - _ELITEAPIMonitored.Party.GetPartyMembers()[partyMemberId].CurrentHP) >= Form2.config.curaga4Amount) && (_ELITEAPIPL.Player.MP > 260))
                {
                    string cureSpell = string.Empty;
                    if (HasSpell("Curaga IV"))
                    {
                        cureSpell = CureTiers("Curaga IV", false);
                    }
                    else if (Form2.config.Accession && Form2.config.accessionCure && HasAbility("Accession") && currentSCHCharges >= 1 && (_ELITEAPIPL.Player.MainJob == 20 || _ELITEAPIPL.Player.SubJob == 20))
                    {
                        cureSpell = CureTiers("Cure IV", false);
                    }

                    if (cureSpell != "false" && cureSpell != string.Empty)
                    {
                        if (cureSpell.StartsWith("Cure") && (plStatusCheck(StatusEffect.Light_Arts) || plStatusCheck(StatusEffect.Addendum_White)))
                        {
                            if (!plStatusCheck(StatusEffect.Accession))
                            {

                                JobAbility_Wait("Curaga, Accession", "Accession");
                                return;
                            }
                        }

                        if (Form2.config.curagaTargetType == 0)
                        {
                            CastSpell(lowestHP_Name, cureSpell);
                        }
                        else
                        {
                            CastSpell(Form2.config.curagaTargetName, cureSpell);
                        }
                    }
                }
                else if (((Form2.config.curaga3enabled && HasSpell("Curaga III") && JobChecker("Curaga III") == true) || (Form2.config.Accession && Form2.config.accessionCure && HasSpell("Cure III") && JobChecker("Cure III") == true)) && ((((_ELITEAPIMonitored.Party.GetPartyMembers()[partyMemberId].CurrentHP * 100) / _ELITEAPIMonitored.Party.GetPartyMembers()[partyMemberId].CurrentHPP) - _ELITEAPIMonitored.Party.GetPartyMembers()[partyMemberId].CurrentHP) >= Form2.config.curaga3Amount) && (_ELITEAPIPL.Player.MP > 180))
                {
                    string cureSpell = string.Empty;
                    if (HasSpell("Curaga III"))
                    {
                        cureSpell = CureTiers("Curaga III", false);
                    }
                    else if (Form2.config.Accession && Form2.config.accessionCure && HasAbility("Accession") && currentSCHCharges >= 1 && (_ELITEAPIPL.Player.MainJob == 20 || _ELITEAPIPL.Player.SubJob == 20))
                    {
                        cureSpell = CureTiers("Cure III", false);
                    }

                    if (cureSpell != "false" && cureSpell != string.Empty)
                    {
                        if (cureSpell.StartsWith("Cure") && (plStatusCheck(StatusEffect.Light_Arts) || plStatusCheck(StatusEffect.Addendum_White)))
                        {
                            if (!plStatusCheck(StatusEffect.Accession))
                            {
                                JobAbility_Wait("Curaga, Accession", "Accession");
                                return;
                            }
                        }

                        if (Form2.config.curagaTargetType == 0)
                        {
                            CastSpell(lowestHP_Name, cureSpell);
                        }
                        else
                        {
                            CastSpell(Form2.config.curagaTargetName, cureSpell);
                        }
                    }
                }
                else if (((Form2.config.curaga2enabled && HasSpell("Curaga II") && JobChecker("Curaga II") == true) || (Form2.config.Accession && Form2.config.accessionCure && HasSpell("Cure II") && JobChecker("Cure II") == true)) && ((((_ELITEAPIMonitored.Party.GetPartyMembers()[partyMemberId].CurrentHP * 100) / _ELITEAPIMonitored.Party.GetPartyMembers()[partyMemberId].CurrentHPP) - _ELITEAPIMonitored.Party.GetPartyMembers()[partyMemberId].CurrentHP) >= Form2.config.curaga2Amount) && (_ELITEAPIPL.Player.MP > 120))
                {
                    string cureSpell = string.Empty;
                    if (HasSpell("Curaga II"))
                    {
                        cureSpell = CureTiers("Curaga II", false);
                    }
                    else if (Form2.config.Accession && Form2.config.accessionCure && HasAbility("Accession") && currentSCHCharges >= 1 && (_ELITEAPIPL.Player.MainJob == 20 || _ELITEAPIPL.Player.SubJob == 20))
                    {
                        cureSpell = CureTiers("Cure II", false);
                    }
                    if (cureSpell != "false" && cureSpell != string.Empty)
                    {
                        if (cureSpell.StartsWith("Cure") && (plStatusCheck(StatusEffect.Light_Arts) || plStatusCheck(StatusEffect.Addendum_White)))
                        {
                            if (!plStatusCheck(StatusEffect.Accession))
                            {
                                JobAbility_Wait("Curaga, Accession", "Accession");
                                return;
                            }
                        }

                        if (Form2.config.curagaTargetType == 0)
                        {
                            CastSpell(lowestHP_Name, cureSpell);
                        }
                        else
                        {
                            CastSpell(Form2.config.curagaTargetName, cureSpell);
                        }
                    }
                }
                else if (((Form2.config.curagaEnabled && HasSpell("Curaga") && JobChecker("Curaga") == true) || (Form2.config.Accession && Form2.config.accessionCure && HasSpell("Cure") && JobChecker("Cure") == true)) && ((((_ELITEAPIMonitored.Party.GetPartyMembers()[partyMemberId].CurrentHP * 100) / _ELITEAPIMonitored.Party.GetPartyMembers()[partyMemberId].CurrentHPP) - _ELITEAPIMonitored.Party.GetPartyMembers()[partyMemberId].CurrentHP) >= Form2.config.curagaAmount) && (_ELITEAPIPL.Player.MP > 60))
                {
                    string cureSpell = string.Empty;
                    if (HasSpell("Curaga"))
                    {
                        cureSpell = CureTiers("Curaga", false);
                    }
                    else if (Form2.config.Accession && Form2.config.accessionCure && HasAbility("Accession") && currentSCHCharges >= 1 && (_ELITEAPIPL.Player.MainJob == 20 || _ELITEAPIPL.Player.SubJob == 20))
                    {
                        cureSpell = CureTiers("Cure", false);
                    }

                    if (cureSpell != "false" && cureSpell != string.Empty)
                    {
                        if (cureSpell.StartsWith("Cure") && (plStatusCheck(StatusEffect.Light_Arts) || plStatusCheck(StatusEffect.Addendum_White)))
                        {
                            if (!plStatusCheck(StatusEffect.Accession))
                            {
                                JobAbility_Wait("Curaga, Accession", "Accession");
                                return;
                            }
                        }

                        if (Form2.config.curagaTargetType == 0)
                        {
                            CastSpell(lowestHP_Name, cureSpell);
                        }
                        else
                        {
                            CastSpell(Form2.config.curagaTargetName, cureSpell);
                        }
                    }
                }
            }
        }

        private bool castingPossible(byte partyMemberId)
        {
            if ((_ELITEAPIPL.Entity.GetEntity((int)_ELITEAPIMonitored.Party.GetPartyMembers()[partyMemberId].TargetIndex).Distance < 21) && (_ELITEAPIPL.Entity.GetEntity((int)_ELITEAPIMonitored.Party.GetPartyMembers()[partyMemberId].TargetIndex).Distance > 0) && (_ELITEAPIMonitored.Party.GetPartyMembers()[partyMemberId].CurrentHP > 0) || (_ELITEAPIPL.Party.GetPartyMember(0).ID == _ELITEAPIMonitored.Party.GetPartyMembers()[partyMemberId].ID) && (_ELITEAPIMonitored.Party.GetPartyMembers()[partyMemberId].CurrentHP > 0))
            {
                return true;
            }
            return false;
        }

        private bool plStatusCheck(StatusEffect requestedStatus)
        {
            bool statusFound = false;
            foreach (StatusEffect status in _ELITEAPIPL.Player.Buffs.Cast<StatusEffect>().Where(status => requestedStatus == status))
            {
                statusFound = true;
            }
            return statusFound;
        }

        private bool monitoredStatusCheck(StatusEffect requestedStatus)
        {
            bool statusFound = false;
            foreach (StatusEffect status in _ELITEAPIMonitored.Player.Buffs.Cast<StatusEffect>().Where(status => requestedStatus == status))
            {
                statusFound = true;
            }
            return statusFound;
        }

        public bool BuffChecker(int buffID, int checkedPlayer)
        {
            if (checkedPlayer == 1)
            {
                if (_ELITEAPIMonitored.Player.GetPlayerInfo().Buffs.Any(b => b == buffID))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                if (_ELITEAPIPL.Player.GetPlayerInfo().Buffs.Any(b => b == buffID))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }


        private void CastSpell(string partyMemberName, string spellName, [Optional] string OptionalExtras)
        {
            if (CastingBackground_Check != true)
            {

                EliteAPI.ISpell magic = _ELITEAPIPL.Resources.GetSpell(spellName.Trim(), 0);

                castingSpell = magic.Name[0];

                _ELITEAPIPL.ThirdParty.SendString("/ma \"" + castingSpell + "\" " + partyMemberName);

                if (OptionalExtras != null)
                {
                    currentAction.Text = "Casting: " + castingSpell + " [" + OptionalExtras + "]";
                }
                else
                {
                    currentAction.Text = "Casting: " + castingSpell;
                }

                CastingBackground_Check = true;

                if (Form2.config.trackCastingPackets == true && Form2.config.EnableAddOn == true)
                {
                    if (!ProtectCasting.IsBusy) { ProtectCasting.RunWorkerAsync(); }
                }
                else
                {
                    castingLockLabel.Text = "Casting is LOCKED";
                    if (!ProtectCasting.IsBusy) { ProtectCasting.RunWorkerAsync(); }
                }

            }

        }

        private void hastePlayer(byte partyMemberId)
        {
            CastSpell(_ELITEAPIMonitored.Party.GetPartyMembers()[partyMemberId].Name, "Haste");
            playerHaste[partyMemberId] = DateTime.Now;
        }

        private void haste_IIPlayer(byte partyMemberId)
        {
            CastSpell(_ELITEAPIMonitored.Party.GetPartyMembers()[partyMemberId].Name, "Haste II");
            playerHaste_II[partyMemberId] = DateTime.Now;
        }

        private void AdloquiumPlayer(byte partyMemberId)
        {
            CastSpell(_ELITEAPIMonitored.Party.GetPartyMembers()[partyMemberId].Name, "Adloquium");
            playerAdloquium[partyMemberId] = DateTime.Now;
        }

        private void FlurryPlayer(byte partyMemberId)
        {
            CastSpell(_ELITEAPIMonitored.Party.GetPartyMembers()[partyMemberId].Name, "Flurry");
            playerFlurry[partyMemberId] = DateTime.Now;
        }

        private void Flurry_IIPlayer(byte partyMemberId)
        {
            CastSpell(_ELITEAPIMonitored.Party.GetPartyMembers()[partyMemberId].Name, "Flurry II");
            playerFlurry_II[partyMemberId] = DateTime.Now;
        }

        private void Phalanx_IIPlayer(byte partyMemberId)
        {
            CastSpell(_ELITEAPIMonitored.Party.GetPartyMembers()[partyMemberId].Name, "Phalanx II");
            playerPhalanx_II[partyMemberId] = DateTime.Now;
        }

        private void StormSpellPlayer(byte partyMemberId, string Spell)
        {
            CastSpell(_ELITEAPIMonitored.Party.GetPartyMembers()[partyMemberId].Name, Spell);
            playerStormspell[partyMemberId] = DateTime.Now;
        }

        private void Regen_Player(byte partyMemberId)
        {
            if (Form2.config.cureBeforeRegen)
            {
                CureCalculator(partyMemberId, false);
            }

            string regenSpell = "";
            if (Form2.config.regen3enabled && HasSpell("Regen III") && JobChecker("Regen III") && CheckSpellRecast("Regen III") == 0)
            {
                regenSpell = "Regen III";
            }
            else if (Form2.config.regen2enabled && HasSpell("Regen II") && JobChecker("Regen II") && CheckSpellRecast("Regen II") == 0)
            {
                regenSpell = "Regen II";
            }
            else if (Form2.config.regen1enabled && HasSpell("Regen") && JobChecker("Regen") && CheckSpellRecast("Regen") == 0)
            {
                regenSpell = "Regen";
            }

            if (!string.IsNullOrEmpty(regenSpell))
            {
                CastSpell(_ELITEAPIMonitored.Party.GetPartyMembers()[partyMemberId].Name, regenSpell);
                playerRegen[partyMemberId] = DateTime.Now;
            }
        }

        private void Refresh_Player(byte partyMemberId)
        {
            string[] refresh_spells = { "Refresh", "Refresh II", "Refresh III" };
            CastSpell(_ELITEAPIMonitored.Party.GetPartyMembers()[partyMemberId].Name, refresh_spells[Form2.config.autoRefresh_Spell]);
            playerRefresh[partyMemberId] = DateTime.Now;
        }

        private void protectPlayer(byte partyMemberId)
        {
            string[] protect_spells = { "Protect", "Protect II", "Protect III", "Protect IV", "Protect V" };
            CastSpell(_ELITEAPIMonitored.Party.GetPartyMembers()[partyMemberId].Name, protect_spells[Form2.config.autoProtect_Spell]);
            playerProtect[partyMemberId] = DateTime.Now;
        }

        private void shellPlayer(byte partyMemberId)
        {
            string[] shell_spells = { "Shell", "Shell II", "Shell III", "Shell IV", "Shell V" };

            CastSpell(_ELITEAPIMonitored.Party.GetPartyMembers()[partyMemberId].Name, shell_spells[Form2.config.autoShell_Spell]);
            playerShell[partyMemberId] = DateTime.Now;
        }

        private bool ActiveSpikes()
        {
            if ((Form2.config.plSpikes_Spell == 0) && plStatusCheck(StatusEffect.Blaze_Spikes))
            {
                return true;
            }
            else if ((Form2.config.plSpikes_Spell == 1) && plStatusCheck(StatusEffect.Ice_Spikes))
            {
                return true;
            }
            else if ((Form2.config.plSpikes_Spell == 2) && plStatusCheck(StatusEffect.Shock_Spikes))
            {
                return true;
            }
            return false;
        }

        private bool PLInParty()
        {
            // FALSE IS WANTED WHEN NOT IN PARTY

            if (_ELITEAPIPL.Player.Name == _ELITEAPIMonitored.Player.Name) // MONITORED AND POL ARE BOTH THE SAME THEREFORE IN THE PARTY
            {
                return true;
            }

            var PARTYD = _ELITEAPIPL.Party.GetPartyMembers().Where(p => p.Active != 0 && p.Zone == _ELITEAPIPL.Player.ZoneId);

            List<string> gen = new List<string>();
            foreach (EliteAPI.PartyMember pData in PARTYD)
            {
                if (pData != null && pData.Name != "")
                {
                    gen.Add(pData.Name);
                }
            }

            if (gen.Contains(_ELITEAPIPL.Player.Name) && gen.Contains(_ELITEAPIMonitored.Player.Name))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void GrabPlayerMonitoredData()
        {
            for (int x = 0; x < 2048; x++)
            {
                EliteAPI.XiEntity entity = _ELITEAPIPL.Entity.GetEntity(x);

                if (entity.Name != null && entity.Name == _ELITEAPIMonitored.Player.Name)
                {
                    Monitored_Index = entity.TargetID;
                }
                else if (entity.Name != null && entity.Name == _ELITEAPIPL.Player.Name)
                {
                    PL_Index = entity.TargetID;
                }
            }
        }
        private void RunRdmDebuffs()
        {
            if (!Form2.config.rdmDebuffsEnabled) return;

            var target = _ELITEAPIMonitored.Target.GetTargetInfo();
            if (target.TargetIndex <= 0) return;

            var targetEntity = _ELITEAPIMonitored.Entity.GetEntity((int)target.TargetIndex);
            if (targetEntity == null || targetEntity.HealthPercent <= 0 || targetEntity.HealthPercent > Form2.config.rdmDebuffMobHp) return;

            TimeSpan timeSinceLastDebuff = DateTime.Now - lastDebuffCastTime;
            if (timeSinceLastDebuff.TotalSeconds < 3) return; // Add a small delay between debuffs

            if (Form2.config.rdmDebuffList.Count > 0)
            {
                string spellToCast = Form2.config.rdmDebuffList[rdmCurrentDebuffIndex];
                if (HasSpell(spellToCast) && CheckSpellRecast(spellToCast) == 0)
                {
                    CastSpell("<bt>", spellToCast);
                    lastDebuffCastTime = DateTime.Now;
                    rdmCurrentDebuffIndex = (rdmCurrentDebuffIndex + 1) % Form2.config.rdmDebuffList.Count;
                }
            }
        }
        private async void actionTimer_TickAsync(object sender, EventArgs e)
        {
            string[] shell_spells = { "Shell", "Shell II", "Shell III", "Shell IV", "Shell V" };
            string[] protect_spells = { "Protect", "Protect II", "Protect III", "Protect IV", "Protect V" };

            if (_ELITEAPIPL == null || _ELITEAPIMonitored == null)
            {
                return;
            }

            if (_ELITEAPIPL.Player.LoginStatus != (int)LoginStatus.LoggedIn || _ELITEAPIMonitored.Player.LoginStatus != (int)LoginStatus.LoggedIn)
            {
                return;
            }


            GrabPlayerMonitoredData();

            // Grab current time for calculations below

            currentTime = DateTime.Now;
            // Calculate time since haste was cast on particular player
            playerHasteSpan[0] = currentTime.Subtract(playerHaste[0]);
            playerHasteSpan[1] = currentTime.Subtract(playerHaste[1]);
            playerHasteSpan[2] = currentTime.Subtract(playerHaste[2]);
            playerHasteSpan[3] = currentTime.Subtract(playerHaste[3]);
            playerHasteSpan[4] = currentTime.Subtract(playerHaste[4]);
            playerHasteSpan[5] = currentTime.Subtract(playerHaste[5]);
            playerHasteSpan[6] = currentTime.Subtract(playerHaste[6]);
            playerHasteSpan[7] = currentTime.Subtract(playerHaste[7]);
            playerHasteSpan[8] = currentTime.Subtract(playerHaste[8]);
            playerHasteSpan[9] = currentTime.Subtract(playerHaste[9]);
            playerHasteSpan[10] = currentTime.Subtract(playerHaste[10]);
            playerHasteSpan[11] = currentTime.Subtract(playerHaste[11]);
            playerHasteSpan[12] = currentTime.Subtract(playerHaste[12]);
            playerHasteSpan[13] = currentTime.Subtract(playerHaste[13]);
            playerHasteSpan[14] = currentTime.Subtract(playerHaste[14]);
            playerHasteSpan[15] = currentTime.Subtract(playerHaste[15]);
            playerHasteSpan[16] = currentTime.Subtract(playerHaste[16]);
            playerHasteSpan[17] = currentTime.Subtract(playerHaste[17]);

            playerHaste_IISpan[0] = currentTime.Subtract(playerHaste_II[0]);
            playerHaste_IISpan[1] = currentTime.Subtract(playerHaste_II[1]);
            playerHaste_IISpan[2] = currentTime.Subtract(playerHaste_II[2]);
            playerHaste_IISpan[3] = currentTime.Subtract(playerHaste_II[3]);
            playerHaste_IISpan[4] = currentTime.Subtract(playerHaste_II[4]);
            playerHaste_IISpan[5] = currentTime.Subtract(playerHaste_II[5]);
            playerHaste_IISpan[6] = currentTime.Subtract(playerHaste_II[6]);
            playerHaste_IISpan[7] = currentTime.Subtract(playerHaste_II[7]);
            playerHaste_IISpan[8] = currentTime.Subtract(playerHaste_II[8]);
            playerHaste_IISpan[9] = currentTime.Subtract(playerHaste_II[9]);
            playerHaste_IISpan[10] = currentTime.Subtract(playerHaste_II[10]);
            playerHaste_IISpan[11] = currentTime.Subtract(playerHaste_II[11]);
            playerHaste_IISpan[12] = currentTime.Subtract(playerHaste_II[12]);
            playerHaste_IISpan[13] = currentTime.Subtract(playerHaste_II[13]);
            playerHaste_IISpan[14] = currentTime.Subtract(playerHaste_II[14]);
            playerHaste_IISpan[15] = currentTime.Subtract(playerHaste_II[15]);
            playerHaste_IISpan[16] = currentTime.Subtract(playerHaste_II[16]);
            playerHaste_IISpan[17] = currentTime.Subtract(playerHaste_II[17]);

            playerFlurrySpan[0] = currentTime.Subtract(playerFlurry[0]);
            playerFlurrySpan[1] = currentTime.Subtract(playerFlurry[1]);
            playerFlurrySpan[2] = currentTime.Subtract(playerFlurry[2]);
            playerFlurrySpan[3] = currentTime.Subtract(playerFlurry[3]);
            playerFlurrySpan[4] = currentTime.Subtract(playerFlurry[4]);
            playerFlurrySpan[5] = currentTime.Subtract(playerFlurry[5]);
            playerFlurrySpan[6] = currentTime.Subtract(playerFlurry[6]);
            playerFlurrySpan[7] = currentTime.Subtract(playerFlurry[7]);
            playerFlurrySpan[8] = currentTime.Subtract(playerFlurry[8]);
            playerFlurrySpan[9] = currentTime.Subtract(playerFlurry[9]);
            playerFlurrySpan[10] = currentTime.Subtract(playerFlurry[10]);
            playerFlurrySpan[11] = currentTime.Subtract(playerFlurry[11]);
            playerFlurrySpan[12] = currentTime.Subtract(playerFlurry[12]);
            playerFlurrySpan[13] = currentTime.Subtract(playerFlurry[13]);
            playerFlurrySpan[14] = currentTime.Subtract(playerFlurry[14]);
            playerFlurrySpan[15] = currentTime.Subtract(playerFlurry[15]);
            playerFlurrySpan[16] = currentTime.Subtract(playerFlurry[16]);
            playerFlurrySpan[17] = currentTime.Subtract(playerFlurry[17]);

            playerFlurry_IISpan[0] = currentTime.Subtract(playerFlurry_II[0]);
            playerFlurry_IISpan[1] = currentTime.Subtract(playerFlurry_II[1]);
            playerFlurry_IISpan[2] = currentTime.Subtract(playerFlurry_II[2]);
            playerFlurry_IISpan[3] = currentTime.Subtract(playerFlurry_II[3]);
            playerFlurry_IISpan[4] = currentTime.Subtract(playerFlurry_II[4]);
            playerFlurry_IISpan[5] = currentTime.Subtract(playerFlurry_II[5]);
            playerFlurry_IISpan[6] = currentTime.Subtract(playerFlurry_II[6]);
            playerFlurry_IISpan[7] = currentTime.Subtract(playerFlurry_II[7]);
            playerFlurry_IISpan[8] = currentTime.Subtract(playerFlurry_II[8]);
            playerFlurry_IISpan[9] = currentTime.Subtract(playerFlurry_II[9]);
            playerFlurry_IISpan[10] = currentTime.Subtract(playerFlurry_II[10]);
            playerFlurry_IISpan[11] = currentTime.Subtract(playerFlurry_II[11]);
            playerFlurry_IISpan[12] = currentTime.Subtract(playerFlurry_II[12]);
            playerFlurry_IISpan[13] = currentTime.Subtract(playerFlurry_II[13]);
            playerFlurry_IISpan[14] = currentTime.Subtract(playerFlurry_II[15]);
            playerFlurry_IISpan[16] = currentTime.Subtract(playerFlurry_II[16]);
            playerFlurry_IISpan[17] = currentTime.Subtract(playerFlurry_II[17]);

            // Calculate time since protect was cast on particular player
            playerProtect_Span[0] = currentTime.Subtract(playerProtect[0]);
            playerProtect_Span[1] = currentTime.Subtract(playerProtect[1]);
            playerProtect_Span[2] = currentTime.Subtract(playerProtect[2]);
            playerProtect_Span[3] = currentTime.Subtract(playerProtect[3]);
            playerProtect_Span[4] = currentTime.Subtract(playerProtect[4]);
            playerProtect_Span[5] = currentTime.Subtract(playerProtect[5]);
            playerProtect_Span[6] = currentTime.Subtract(playerProtect[6]);
            playerProtect_Span[7] = currentTime.Subtract(playerProtect[7]);
            playerProtect_Span[8] = currentTime.Subtract(playerProtect[8]);
            playerProtect_Span[9] = currentTime.Subtract(playerProtect[9]);
            playerProtect_Span[10] = currentTime.Subtract(playerProtect[10]);
            playerProtect_Span[11] = currentTime.Subtract(playerProtect[11]);
            playerProtect_Span[12] = currentTime.Subtract(playerProtect[12]);
            playerProtect_Span[13] = currentTime.Subtract(playerProtect[13]);
            playerProtect_Span[14] = currentTime.Subtract(playerProtect[14]);
            playerProtect_Span[15] = currentTime.Subtract(playerProtect[15]);
            playerProtect_Span[16] = currentTime.Subtract(playerProtect[16]);
            playerProtect_Span[17] = currentTime.Subtract(playerProtect[17]);

            // Calculate time since Stormspell was cast on particular player
            playerStormspellSpan[0] = currentTime.Subtract(playerStormspell[0]);
            playerStormspellSpan[1] = currentTime.Subtract(playerStormspell[1]);
            playerStormspellSpan[2] = currentTime.Subtract(playerStormspell[2]);
            playerStormspellSpan[3] = currentTime.Subtract(playerStormspell[3]);
            playerStormspellSpan[4] = currentTime.Subtract(playerStormspell[4]);
            playerStormspellSpan[5] = currentTime.Subtract(playerStormspell[5]);
            playerStormspellSpan[6] = currentTime.Subtract(playerStormspell[6]);
            playerStormspellSpan[7] = currentTime.Subtract(playerStormspell[7]);
            playerStormspellSpan[8] = currentTime.Subtract(playerStormspell[8]);
            playerStormspellSpan[9] = currentTime.Subtract(playerStormspell[9]);
            playerStormspellSpan[10] = currentTime.Subtract(playerStormspell[10]);
            playerStormspellSpan[11] = currentTime.Subtract(playerStormspell[11]);
            playerStormspellSpan[12] = currentTime.Subtract(playerStormspell[12]);
            playerStormspellSpan[13] = currentTime.Subtract(playerStormspell[13]);
            playerStormspellSpan[14] = currentTime.Subtract(playerStormspell[14]);
            playerStormspellSpan[15] = currentTime.Subtract(playerStormspell[15]);
            playerStormspellSpan[16] = currentTime.Subtract(playerStormspell[16]);
            playerStormspellSpan[17] = currentTime.Subtract(playerStormspell[17]);

            // Calculate time since shell was cast on particular player
            playerShell_Span[0] = currentTime.Subtract(playerShell[0]);
            playerShell_Span[1] = currentTime.Subtract(playerShell[1]);
            playerShell_Span[2] = currentTime.Subtract(playerShell[2]);
            playerShell_Span[3] = currentTime.Subtract(playerShell[3]);
            playerShell_Span[4] = currentTime.Subtract(playerShell[4]);
            playerShell_Span[5] = currentTime.Subtract(playerShell[5]);
            playerShell_Span[6] = currentTime.Subtract(playerShell[6]);
            playerShell_Span[7] = currentTime.Subtract(playerShell[7]);
            playerShell_Span[8] = currentTime.Subtract(playerShell[8]);
            playerShell_Span[9] = currentTime.Subtract(playerShell[9]);
            playerShell_Span[10] = currentTime.Subtract(playerShell[10]);
            playerShell_Span[11] = currentTime.Subtract(playerShell[11]);
            playerShell_Span[12] = currentTime.Subtract(playerShell[12]);
            playerShell_Span[13] = currentTime.Subtract(playerShell[13]);
            playerShell_Span[14] = currentTime.Subtract(playerShell[14]);
            playerShell_Span[15] = currentTime.Subtract(playerShell[15]);
            playerShell_Span[16] = currentTime.Subtract(playerShell[16]);
            playerShell_Span[17] = currentTime.Subtract(playerShell[17]);

            // Calculate time since phalanx II was cast on particular player
            playerPhalanx_IISpan[0] = currentTime.Subtract(playerPhalanx_II[0]);
            playerPhalanx_IISpan[1] = currentTime.Subtract(playerPhalanx_II[1]);
            playerPhalanx_IISpan[2] = currentTime.Subtract(playerPhalanx_II[2]);
            playerPhalanx_IISpan[3] = currentTime.Subtract(playerPhalanx_II[3]);
            playerPhalanx_IISpan[4] = currentTime.Subtract(playerPhalanx_II[4]);
            playerPhalanx_IISpan[5] = currentTime.Subtract(playerPhalanx_II[5]);

            // Calculate time since regen was cast on particular player
            playerRegen_Span[0] = currentTime.Subtract(playerRegen[0]);
            playerRegen_Span[1] = currentTime.Subtract(playerRegen[1]);
            playerRegen_Span[2] = currentTime.Subtract(playerRegen[2]);
            playerRegen_Span[3] = currentTime.Subtract(playerRegen[3]);
            playerRegen_Span[4] = currentTime.Subtract(playerRegen[4]);
            playerRegen_Span[5] = currentTime.Subtract(playerRegen[5]);

            // Calculate time since Refresh was cast on particular player
            playerRefresh_Span[0] = currentTime.Subtract(playerRefresh[0]);
            playerRefresh_Span[1] = currentTime.Subtract(playerRefresh[1]);
            playerRefresh_Span[2] = currentTime.Subtract(playerRefresh[2]);
            playerRefresh_Span[3] = currentTime.Subtract(playerRefresh[3]);
            playerRefresh_Span[4] = currentTime.Subtract(playerRefresh[4]);
            playerRefresh_Span[5] = currentTime.Subtract(playerRefresh[5]);

            // Calculate time since Songs were cast on particular player
            playerSong1_Span[0] = currentTime.Subtract(playerSong1[0]);
            playerSong2_Span[0] = currentTime.Subtract(playerSong2[0]);
            playerSong3_Span[0] = currentTime.Subtract(playerSong3[0]);
            playerSong4_Span[0] = currentTime.Subtract(playerSong4[0]);

            // Calculate time since Adloquium were cast on particular player
            playerAdloquium_Span[0] = currentTime.Subtract(playerAdloquium[0]);
            playerAdloquium_Span[1] = currentTime.Subtract(playerAdloquium[1]);
            playerAdloquium_Span[2] = currentTime.Subtract(playerAdloquium[2]);
            playerAdloquium_Span[3] = currentTime.Subtract(playerAdloquium[3]);
            playerAdloquium_Span[4] = currentTime.Subtract(playerAdloquium[4]);
            playerAdloquium_Span[5] = currentTime.Subtract(playerAdloquium[5]);
            playerAdloquium_Span[6] = currentTime.Subtract(playerAdloquium[6]);
            playerAdloquium_Span[7] = currentTime.Subtract(playerAdloquium[7]);
            playerAdloquium_Span[8] = currentTime.Subtract(playerAdloquium[8]);
            playerAdloquium_Span[9] = currentTime.Subtract(playerAdloquium[9]);
            playerAdloquium_Span[10] = currentTime.Subtract(playerAdloquium[10]);
            playerAdloquium_Span[11] = currentTime.Subtract(playerAdloquium[11]);
            playerAdloquium_Span[12] = currentTime.Subtract(playerAdloquium[12]);
            playerAdloquium_Span[13] = currentTime.Subtract(playerAdloquium[13]);
            playerAdloquium_Span[14] = currentTime.Subtract(playerAdloquium[14]);
            playerAdloquium_Span[15] = currentTime.Subtract(playerAdloquium[15]);
            playerAdloquium_Span[16] = currentTime.Subtract(playerAdloquium[16]);
            playerAdloquium_Span[17] = currentTime.Subtract(playerAdloquium[17]);


            Last_SongCast_Timer_Span[0] = currentTime.Subtract(Last_SongCast_Timer[0]);

            // Calculate time since Piannisimo Songs were cast on particular player
            pianissimo1_1_Span[0] = currentTime.Subtract(playerPianissimo1_1[0]);
            pianissimo2_1_Span[0] = currentTime.Subtract(playerPianissimo2_1[0]);
            pianissimo1_2_Span[0] = currentTime.Subtract(playerPianissimo1_2[0]);
            pianissimo2_2_Span[0] = currentTime.Subtract(playerPianissimo2_2[0]);

            // Set array values for GUI "Enabled" checkboxes
            CheckBox[] enabledBoxes = new CheckBox[18];
            enabledBoxes[0] = player0enabled;
            enabledBoxes[1] = player1enabled;
            enabledBoxes[2] = player2enabled;
            enabledBoxes[3] = player3enabled;
            enabledBoxes[4] = player4enabled;
            enabledBoxes[5] = player5enabled;
            enabledBoxes[6] = player6enabled;
            enabledBoxes[7] = player7enabled;
            enabledBoxes[8] = player8enabled;
            enabledBoxes[9] = player9enabled;
            enabledBoxes[10] = player10enabled;
            enabledBoxes[11] = player11enabled;
            enabledBoxes[12] = player12enabled;
            enabledBoxes[13] = player13enabled;
            enabledBoxes[14] = player14enabled;
            enabledBoxes[15] = player15enabled;
            enabledBoxes[16] = player16enabled;
            enabledBoxes[17] = player17enabled;

            // Set array values for GUI "High Priority" checkboxes
            CheckBox[] highPriorityBoxes = new CheckBox[18];
            highPriorityBoxes[0] = player0priority;
            highPriorityBoxes[1] = player1priority;
            highPriorityBoxes[2] = player2priority;
            highPriorityBoxes[3] = player3priority;
            highPriorityBoxes[4] = player4priority;
            highPriorityBoxes[5] = player5priority;
            highPriorityBoxes[6] = player6priority;
            highPriorityBoxes[7] = player7priority;
            highPriorityBoxes[8] = player8priority;
            highPriorityBoxes[9] = player9priority;
            highPriorityBoxes[10] = player10priority;
            highPriorityBoxes[11] = player11priority;
            highPriorityBoxes[12] = player12priority;
            highPriorityBoxes[13] = player13priority;
            highPriorityBoxes[14] = player14priority;
            highPriorityBoxes[15] = player15priority;
            highPriorityBoxes[16] = player16priority;
            highPriorityBoxes[17] = player17priority;


            int songs_currently_up1 = _ELITEAPIMonitored.Player.GetPlayerInfo().Buffs.Where(b => b == 197 || b == 198 || b == 195 || b == 199 || b == 200 || b == 215 || b == 196 || b == 214 || b == 216 || b == 218 || b == 222).Count();



            // IF ENABLED PAUSE ON KO
            if (Form2.config.pauseOnKO && (_ELITEAPIPL.Player.Status == 2 || _ELITEAPIPL.Player.Status == 3))
            {
                pauseButton.Text = "Paused!";
                pauseButton.ForeColor = Color.Red;
                actionTimer.Enabled = false;
                ActiveBuffs.Clear();
                pauseActions = true;
                if (Form2.config.FFXIDefaultAutoFollow == false)
                {
                    _ELITEAPIPL.AutoFollow.IsAutoFollowing = false;
                }
            }

            // IF YOU ARE DEAD BUT RERAISE IS AVAILABLE THEN ACCEPT RAISE
            if (Form2.config.AcceptRaise == true && (_ELITEAPIPL.Player.Status == 2 || _ELITEAPIPL.Player.Status == 3))
            {
                if (_ELITEAPIPL.Menu.IsMenuOpen && _ELITEAPIPL.Menu.HelpName == "Revival" && _ELITEAPIPL.Menu.MenuIndex == 1 && ((Form2.config.AcceptRaiseOnlyWhenNotInCombat == true && _ELITEAPIMonitored.Player.Status != 1) || Form2.config.AcceptRaiseOnlyWhenNotInCombat == false))
                {
                    await Task.Delay(2000);
                    currentAction.Text = "Accepting Raise or Reraise.";
                    _ELITEAPIPL.ThirdParty.KeyPress(EliteMMO.API.Keys.NUMPADENTER);
                    await Task.Delay(5000);
                    currentAction.Text = string.Empty;
                }
            }


            // If CastingLock is not FALSE and you're not Terrorized, Petrified, or Stunned run the actions
            if (JobAbilityLock_Check != true && CastingBackground_Check != true && !plStatusCheck(StatusEffect.Terror) && !plStatusCheck(StatusEffect.Petrification) && !plStatusCheck(StatusEffect.Stun))
            {
                RunRdmDebuffs();

                // FIRST IF YOU ARE SILENCED OR DOOMED ATTEMPT REMOVAL NOW
                if (plStatusCheck(StatusEffect.Silence) && Form2.config.plSilenceItemEnabled)
                {
                    // Check to make sure we have echo drops
                    if ((GetInventoryItemCount(_ELITEAPIPL, GetItemId(plSilenceitemName)) > 0 || GetTempItemCount(_ELITEAPIPL, GetItemId(plSilenceitemName)) > 0))
                    {
                        Item_Wait(plSilenceitemName);
                    }

                }
                else if ((plStatusCheck(StatusEffect.Doom) && Form2.config.plDoomEnabled) /* Add more options from UI HERE*/)
                {
                    // Check to make sure we have holy water
                    if (GetInventoryItemCount(_ELITEAPIPL, GetItemId(plDoomItemName)) > 0 || GetTempItemCount(_ELITEAPIPL, GetItemId(plDoomItemName)) > 0)
                    {
                        _ELITEAPIPL.ThirdParty.SendString(string.Format("/item \"{0}\" <me>", plDoomItemName));
                        await Task.Delay(TimeSpan.FromSeconds(2));
                    }
                }

                else if (Form2.config.DivineSeal && _ELITEAPIPL.Player.MPP <= 11 && (GetAbilityRecast("Divine Seal") == 0) && !_ELITEAPIPL.Player.Buffs.Contains((short)StatusEffect.Weakness))
                {
                    JobAbility_Wait("Divine Seal", "Divine Seal");
                }
                else if (Form2.config.Convert && (_ELITEAPIPL.Player.MP <= Form2.config.convertMP) && (GetAbilityRecast("Convert") == 0) && !_ELITEAPIPL.Player.Buffs.Contains((short)StatusEffect.Weakness))
                {
                    _ELITEAPIPL.ThirdParty.SendString("/ja \"Convert\" <me>");
                    return;
                }
                else if (Form2.config.RadialArcana && (_ELITEAPIPL.Player.MP <= Form2.config.RadialArcanaMP) && (GetAbilityRecast("Radial Arcana") == 0) && !_ELITEAPIPL.Player.Buffs.Contains((short)StatusEffect.Weakness))
                {
                    // Check if a pet is already active
                    if (_ELITEAPIPL.Player.Pet.HealthPercent >= 1 && _ELITEAPIPL.Player.Pet.Distance <= 9)
                    {
                        JobAbility_Wait("Radial Arcana", "Radial Arcana");
                    }
                    else if (_ELITEAPIPL.Player.Pet.HealthPercent >= 1 && _ELITEAPIPL.Player.Pet.Distance >= 9 && (GetAbilityRecast("Full Circle") == 0))
                    {
                        _ELITEAPIPL.ThirdParty.SendString("/ja \"Full Circle\" <me>");
                        await Task.Delay(2000);
                        string SpellCheckedResult = ReturnGeoSpell(Form2.config.RadialArcana_Spell, 2);
                        CastSpell("<me>", SpellCheckedResult);
                    }
                    else
                    {
                        string SpellCheckedResult = ReturnGeoSpell(Form2.config.RadialArcana_Spell, 2);
                        CastSpell("<me>", SpellCheckedResult);
                    }
                }
                else if (Form2.config.FullCircle)
                {


                    // When out of range Distance is 59 Yalms regardless, Must be within 15 yalms to gain
                    // the effect

                    //Check if "pet" is active and out of range of the monitored player
                    if (_ELITEAPIPL.Player.Pet.HealthPercent >= 1)
                    {
                        if (Form2.config.Fullcircle_GEOTarget == true && Form2.config.LuopanSpell_Target != "")
                        {

                            ushort PetsIndex = _ELITEAPIPL.Player.PetIndex;

                            EliteAPI.XiEntity PetsEntity = _ELITEAPIPL.Entity.GetEntity(PetsIndex);

                            int FullCircle_CharID = 0;

                            for (int x = 0; x < 2048; x++)
                            {
                                EliteAPI.XiEntity entity = _ELITEAPIPL.Entity.GetEntity(x);

                                if (entity.Name != null && entity.Name.ToLower().Equals(Form2.config.LuopanSpell_Target.ToLower()))
                                {
                                    FullCircle_CharID = Convert.ToInt32(entity.TargetID);
                                    break;
                                }
                            }

                            if (FullCircle_CharID != 0)
                            {
                                EliteAPI.XiEntity FullCircleEntity = _ELITEAPIPL.Entity.GetEntity(FullCircle_CharID);

                                float fX = PetsEntity.X - FullCircleEntity.X;
                                float fY = PetsEntity.Y - FullCircleEntity.Y;
                                float fZ = PetsEntity.Z - FullCircleEntity.Z;

                                float generatedDistance = (float)Math.Sqrt((fX * fX) + (fY * fY) + (fZ * fZ));

                                if (generatedDistance >= 10)
                                {
                                    FullCircle_Timer.Enabled = true;
                                }
                            }

                        }
                        else if (Form2.config.Fullcircle_GEOTarget == false && _ELITEAPIMonitored.Player.Status == 1)
                        {
                            ushort PetsIndex = _ELITEAPIPL.Player.PetIndex;

                            EliteAPI.XiEntity PetsEntity = _ELITEAPIMonitored.Entity.GetEntity(PetsIndex);

                            if (PetsEntity.Distance >= 10)
                            {
                                FullCircle_Timer.Enabled = true;
                            }
                        }

                    }
                }
                else if ((Form2.config.Troubadour) && (GetAbilityRecast("Troubadour") == 0) && (HasAbility("Troubadour")) && songs_currently_up1 == 0)
                {
                    JobAbility_Wait("Troubadour", "Troubadour");
                }
                else if ((Form2.config.Nightingale) && (GetAbilityRecast("Nightingale") == 0) && (HasAbility("Nightingale")) && songs_currently_up1 == 0)
                {
                    JobAbility_Wait("Nightingale", "Nightingale");
                }

                if (_ELITEAPIPL.Player.MP <= (int)Form2.config.mpMinCastValue && _ELITEAPIPL.Player.MP != 0)
                {
                    if (Form2.config.lowMPcheckBox && !islowmp && !Form2.config.healLowMP)
                    {
                        _ELITEAPIPL.ThirdParty.SendString("/tell " + _ELITEAPIMonitored.Player.Name + " MP is low!");
                        islowmp = true;
                        return;
                    }
                    islowmp = true;
                    return;
                }
                if (_ELITEAPIPL.Player.MP > (int)Form2.config.mpMinCastValue && _ELITEAPIPL.Player.MP != 0)
                {
                    if (Form2.config.lowMPcheckBox && islowmp && !Form2.config.healLowMP)
                    {
                        _ELITEAPIPL.ThirdParty.SendString("/tell " + _ELITEAPIMonitored.Player.Name + " MP OK!");
                        islowmp = false;
                    }
                }

                if (Form2.config.healLowMP == true && _ELITEAPIPL.Player.MP <= Form2.config.healWhenMPBelow && _ELITEAPIPL.Player.Status == 0)
                {
                    if (Form2.config.lowMPcheckBox && !islowmp)
                    {
                        _ELITEAPIPL.ThirdParty.SendString("/tell " + _ELITEAPIMonitored.Player.Name + " MP is seriously low, /healing.");
                        islowmp = true;
                    }
                    _ELITEAPIPL.ThirdParty.SendString("/heal");
                }
                else if (Form2.config.standAtMP == true && _ELITEAPIPL.Player.MPP >= Form2.config.standAtMP_Percentage && _ELITEAPIPL.Player.Status == 33)
                {
                    if (Form2.config.lowMPcheckBox && !islowmp)
                    {
                        _ELITEAPIPL.ThirdParty.SendString("/tell " + _ELITEAPIMonitored.Player.Name + " MP has recovered.");
                        islowmp = false;
                    }
                    _ELITEAPIPL.ThirdParty.SendString("/heal");
                }

                // Only perform actions if PL is stationary PAUSE GOES HERE
                if ((_ELITEAPIPL.Player.X == plX) && (_ELITEAPIPL.Player.Y == plY) && (_ELITEAPIPL.Player.Z == plZ) && (_ELITEAPIPL.Player.LoginStatus == (int)LoginStatus.LoggedIn) && JobAbilityLock_Check != true && CastingBackground_Check != true && curePlease_autofollow == false && ((_ELITEAPIPL.Player.Status == (uint)Status.Standing) || (_ELITEAPIPL.Player.Status == (uint)Status.Fighting)))
                {
                    // IF SILENCED THIS NEEDS TO BE REMOVED BEFORE ANY MAGIC IS ATTEMPTED
                    if (Form2.config.plSilenceItem == 0)
                    {
                        plSilenceitemName = "Catholicon";
                    }
                    else if (Form2.config.plSilenceItem == 1)
                    {
                        plSilenceitemName = "Echo Drops";
                    }
                    else if (Form2.config.plSilenceItem == 2)
                    {
                        plSilenceitemName = "Remedy";
                    }
                    else if (Form2.config.plSilenceItem == 3)
                    {
                        plSilenceitemName = "Remedy Ointment";
                    }
                    else if (Form2.config.plSilenceItem == 4)
                    {
                        plSilenceitemName = "Vicar's Drink";
                    }

                    foreach (StatusEffect plEffect in _ELITEAPIPL.Player.Buffs)
                    {
                        if (plEffect == StatusEffect.Silence && Form2.config.plSilenceItemEnabled)
                        {
                            // Check to make sure we have echo drops
                            if (GetInventoryItemCount(_ELITEAPIPL, GetItemId(plSilenceitemName)) > 0 || GetTempItemCount(_ELITEAPIPL, GetItemId(plSilenceitemName)) > 0)
                            {
                                _ELITEAPIPL.ThirdParty.SendString(string.Format("/item \"{0}\" <me>", plSilenceitemName));
                                await Task.Delay(4000);
                                break;
                            }
                        }
                    }

                    List<byte> cures_required = new List<byte>();

                    int MemberOf_curaga = GeneratePT_structure();


                    /////////////////////////// PL CURE //////////////////////////////////////////////////////////////////////////////////////////////////////////////////


                    if (_ELITEAPIPL.Player.HP > 0 && (_ELITEAPIPL.Player.HPP <= Form2.config.monitoredCurePercentage) && Form2.config.enableOutOfPartyHealing == true && PLInParty() == false)
                    {
                        CureCalculator_PL(false);
                    }



                    /////////////////////////// CURAGA //////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                    IOrderedEnumerable<EliteAPI.PartyMember> cParty_curaga = _ELITEAPIMonitored.Party.GetPartyMembers().Where(p => p.Active != 0 && p.Zone == _ELITEAPIPL.Player.ZoneId).OrderBy(p => p.CurrentHPP);

                    int memberOF_curaga = GeneratePT_structure();

                    if (memberOF_curaga != 0 && memberOF_curaga != 4)
                    {
                        foreach (EliteAPI.PartyMember pData in cParty_curaga)
                        {
                            if (memberOF_curaga == 1 && pData.MemberNumber >= 0 && pData.MemberNumber <= 5)
                            {
                                if (castingPossible(pData.MemberNumber) && (_ELITEAPIMonitored.Party.GetPartyMembers()[pData.MemberNumber].Active >= 1) && (enabledBoxes[pData.MemberNumber].Checked) && (_ELITEAPIMonitored.Party.GetPartyMembers()[pData.MemberNumber].CurrentHP > 0))
                                {
                                    if ((_ELITEAPIMonitored.Party.GetPartyMembers()[pData.MemberNumber].CurrentHPP <= Form2.config.curagaCurePercentage) && (castingPossible(pData.MemberNumber)))
                                    {
                                        cures_required.Add(pData.MemberNumber);
                                    }
                                }
                            }
                            else if (memberOF_curaga == 2 && pData.MemberNumber >= 6 && pData.MemberNumber <= 11)
                            {
                                if (castingPossible(pData.MemberNumber) && (_ELITEAPIMonitored.Party.GetPartyMembers()[pData.MemberNumber].Active >= 1) && (enabledBoxes[pData.MemberNumber].Checked) && (_ELITEAPIMonitored.Party.GetPartyMembers()[pData.MemberNumber].CurrentHP > 0))
                                {
                                    if ((_ELITEAPIMonitored.Party.GetPartyMembers()[pData.MemberNumber].CurrentHPP <= Form2.config.curagaCurePercentage) && (castingPossible(pData.MemberNumber)))
                                    {
                                        cures_required.Add(pData.MemberNumber);
                                    }
                                }
                            }
                            else if (memberOF_curaga == 3 && pData.MemberNumber >= 12 && pData.MemberNumber <= 17)
                            {
                                if (castingPossible(pData.MemberNumber) && (_ELITEAPIMonitored.Party.GetPartyMembers()[pData.MemberNumber].Active >= 1) && (enabledBoxes[pData.MemberNumber].Checked) && (_ELITEAPIMonitored.Party.GetPartyMembers()[pData.MemberNumber].CurrentHP > 0))
                                {
                                    if ((_ELITEAPIMonitored.Party.GetPartyMembers()[pData.MemberNumber].CurrentHPP <= Form2.config.curagaCurePercentage) && (castingPossible(pData.MemberNumber)))
                                    {
                                        cures_required.Add(pData.MemberNumber);
                                    }
                                }
                            }
                        }

                        if (cures_required.Count >= Form2.config.curagaRequiredMembers)
                        {
                            int lowestHP_id = cures_required.First();
                            CuragaCalculatorAsync(lowestHP_id);
                        }
                    }

                    /////////////////////////// CURE //////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                    //var playerHpOrder = _ELITEAPIMonitored.Party.GetPartyMembers().Where(p => p.Active >= 1).OrderBy(p => p.CurrentHPP).Select(p => p.Index);
                    IEnumerable<byte> playerHpOrder = _ELITEAPIMonitored.Party.GetPartyMembers().OrderBy(p => p.CurrentHPP).OrderBy(p => p.Active == 0).Select(p => p.MemberNumber);

                    // First run a check on the monitored target
                    byte playerMonitoredHp = _ELITEAPIMonitored.Party.GetPartyMembers().Where(p => p.Name == _ELITEAPIMonitored.Player.Name).OrderBy(p => p.Active == 0).Select(p => p.MemberNumber).FirstOrDefault();

                    if (Form2.config.enableMonitoredPriority && _ELITEAPIMonitored.Party.GetPartyMembers()[playerMonitoredHp].Name == _ELITEAPIMonit