# Miraculix

Miraculix is a comprehensive assistance tool for the game Final Fantasy XI, designed to automate and simplify many aspects of gameplay. It provides intelligent healing, buff management, and combat support for various jobs, with a focus on healers and support roles.

## Key Features

- **Automated Healing:**
  - Intelligently selects the appropriate tier of Cure or Curaga spells based on the health deficits of party members.
  - Prioritizes healing for designated high-priority members or the main monitored character.
  - Supports healing for both party members and "out-of-party" players.

- **Buff Management:**
  - Automatically maintains essential buffs like Haste, Protect, Shell, and Regen on party members and out-of-party players.
  - Selects the highest available tier of a spell that the player has learned.
  - Handles job-specific buffs for various jobs, including Scholar, Geomancer, and Bard.

- **Debuff Removal:**
  - Automatically removes a wide range of debuffs from the player, party members, and out-of-party players.
  - Uses the appropriate spells (e.g., Paralyna, Silena, Cursna) or items (e.g., Echo Drops) to cleanse status ailments.

- **Intelligent Profiling System:**
  - The application now features a three-tiered profiling system to adapt its behavior based on the party's current situation. The current profile is displayed on the main UI.
  - **Normal:** Standard mode where all features are active. Buffs and debuffs are cast as needed.
  - **Degraded:** Activated when multiple party members are below their cure threshold. In this mode, healing is prioritized, and only essential buffs (Group 1: Regen, Refresh, Haste) are cast. Offensive debuffing is paused.
  - **Critical:** Activated when any party member's HP drops below 50%. In this mode, all actions except for healing are suspended to focus entirely on party survival.

- **Combat Assistance:**
  - Auto-debuffing system for Red Mage to maintain debuffs on the current battle target.
  - Automated hate management spells to assist with tanking.
  - Auto-targeting system to engage enemies based on party status.
  - **Delayed Auto-Target:** Optionally adds a configurable delay before automatically targeting a new enemy, giving the player time to complete manual actions.

- **Job-Specific Logic:**
  - **Scholar (SCH):** Manages Light Arts, Dark Arts, and Stratagems to enhance spellcasting.
  - **Geomancer (GEO):** Automates the casting of Indi and Geo spells, including the use of job abilities like Entrust and Full Circle.
  - **Bard (BRD):** Manages a song rotation to maintain party buffs, with support for Pianissimo and other job abilities.
  - **Red Mage (RDM):** In addition to healing and buffing, includes an auto-debuffing system for combat.

- **Customizable Settings:**
  - A detailed settings form allows for the fine-tuning of all automated actions.
  - Character- and job-specific settings can be saved and loaded automatically.
  - UI options for managing party members, out-of-party players, and their specific buff/debuff preferences.

## How to Use

1. **Launch the Application:** Run `Miraculix.exe`.
2. **Select Game Instances:**
   - In the "Player Character (PL)" dropdown, select the character that will be casting the spells.
   - In the "Monitored Player" dropdown, select the character whose party you want to monitor. This can be the same as the Player Character.
   - Click the "Set" buttons to attach the application to the selected game instances.
3. **Configure Settings:**
   - Click the "Settings" button to open the configuration window.
   - Adjust the settings for healing, buffing, debuffing, and job abilities to your preferences.
   - Save your settings. They can be saved per character/job combination.
4. **Enable Automation:**
   - On the main form, use the checkboxes and context menus to enable or disable specific automated actions for each party member.
   - Use the "Outside Party" section to add and manage players who are not in your immediate party.
5. **Start/Pause:**
   - The application will begin its automated actions as soon as it is attached to the game instances and un-paused.
   - Use the "Pause" button to temporarily halt all actions.