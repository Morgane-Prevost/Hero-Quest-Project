#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HeroQuest.Quests.EditorTools
{
    /// <summary>
    /// Menu: HeroQuest > Generate quests 1 to 14 (overwrites).
    /// Creates one QuestData asset per quest in Assets/Quests, with all conditions/actions as sub-assets.
    /// Texts are written in our own words (same convention as CardSO.description).
    ///
    /// !! Quest 1 is linked to the real level JSON (quest_01). For quests 2 to 14, the zone numbers,
    /// element ids ("q3_ulag"...) and tile coordinates below are still PLACEHOLDERS: they must match
    /// YOUR level JSON files (zone = room number of the grid map, ids = "id" fields of doors /
    /// furniture / monsters). Anything not found (JSON, card, HeroData) is logged as a warning.
    /// Tip: LevelBuilder > context menu "Log Element Zones" prints the real zones of every element.
    /// </summary>
    public static class QuestSampleBuilder
    {
        const string OutputFolder = "Assets/Quests";

        // ── Names of your CardSO assets (searched by exact name in the project) ──
        const string CardStaff = "Staff", CardShield = "Shield";
        const string CardHeal4 = "FixedHealing4", CardHeal2 = "FixedHealing2";   // FixedHealingPotionCardSO assets
        const string CardStonePotion = "StonePotion";

        // ── ArtifactInfo.id of your artifacts (ArtifactManager.Grant) ──
        const string ArtTalisman = "knowledge_talisman", ArtBorinArmor = "borin_armor", ArtWand = "magic_wand",
                     ArtElixir = "life_elixir", ArtReturnRing = "return_ring", ArtOrcBane = "orc_bane",
                     ArtEnchanterStaff = "enchanter_staff", ArtMysticSword = "mystic_sword",
                     ArtIncantationRing = "incantation_ring", ArtWizardCloak = "wizard_cloak";

        // ── Monster types (MonsterData.monsterName; comparison ignores case, spaces and plural) ──
        const string Goblin = "Goblin", Orc = "Orc", Abomination = "Abomination", ChaosWarrior = "Dread Warrior",
                     Gargoyle = "Gargoyle", Mummy = "Mummy", Zombie = "Zombie", Skeleton = "Skeleton";

        static QuestData q;

        [MenuItem("HeroQuest/Generate quests 1 to 14 (overwrites)")]
        static void BuildAll()
        {
            Q1(); Q2(); Q3(); Q4(); Q5(); Q6(); Q7(); Q8(); Q9(); Q10(); Q11(); Q12(); Q13(); Q14();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[QuestBuilder] 14 quests generated in " + OutputFolder + ". Check the warnings and replace the ids / zones / tiles with the ones from your JSON files.");
        }

        // ═════════════ Q1 ═════════════
        static void Q1()
        {
            // ADAPT with the output of LevelBuilder > "Log Element Zones"
            const int zRack = 18, zEmptyChest = 19, zChest84 = 3, zChest120 = 11;
            // ids of quest_01.json
            const string idRack = "weapons_rack_a", idChestEmpty = "chest_b",
                         idChest84 = "chest_d", idChest120 = "chest_e",
                         idVerag = "gargoyle_verag",
                         idGuardian = "mummy_1";   // TO CONFIRM: the mummy guarding Fellmarg's tomb

            Begin(1, "The Trial", "The apprentice heroes' first test: go down into the catacombs of Fellmarg's tomb, then find and destroy Verag, the gargoyle that prowls there. Only teamwork will keep you alive.");
            q.wanderingMonsterType = Orc;
            q.introNote = "This quest has no traps and no secret doors. It is still perilous: your survival depends on working together.";
            Obj("kill_verag", "Destroy Verag");
            q.namedMonsters.Add(new NamedMonsterDef { elementId = idGuardian, displayName = "Guardian of Fellmarg", attackDice = 4 });
            q.namedMonsters.Add(new NamedMonsterDef { elementId = idVerag, displayName = "Verag" });

            R("note_A", new[] { Search(zRack, idRack) }, new[] { Note("A", "The weapons on the rack are rusty or broken: they are useless."), Consume() }, once: false);
            R("note_B", new[] { Search(zEmptyChest, idChestEmpty) }, new[] { Note("B", "This chest is empty."), Consume() }, once: false);
            FirstSearch("note_D", zChest84, idChest84, "D", "The first hero to search here finds 84 gold coins in the chest.", Loot(gold: 84));
            FirstSearch("note_E", zChest120, idChest120, "E", "The first hero to search here finds 120 gold coins in the chest.", Loot(gold: 120));
            R("verag_dead", new[] { Killed(idVerag) }, new[] { Complete("kill_verag") });
            End();
        }

        // ═════════════ Q2 ═════════════
        static void Q2()
        {
            const int zCell = 6, zChestTrap = 7, zChestPotion = 8;                // ADAPT
            Begin(2, "Rescuing Sir Ragnar", "Sir Ragnar, a knight of the king, is being held prisoner by Ulag. Find him and bring him back alive to the stairs: 240 gold coins to share, but nothing if the knight dies during the escape.");
            q.wanderingMonsterType = Orc;
            q.sharedGoldReward = 240; q.rewardRequiresObjectiveId = "rescue_ragnar";
            q.noTreasureZones.Add(zCell);
            Obj("rescue_ragnar", "Bring Sir Ragnar back alive to the stairs");
            q.allies.Add(new AllyDef { id = "ragnar", displayName = "Sir Ragnar", template = FindAsset<HeroData>("Ragnar"), spawnCell = new Vector2Int(0, 0) });   // ADAPT: body 2, defense 2, attack 0, 1 red die

            R("ragnar_found", new[] { DoorOpened("q2_cell_door") },
              new[] { Note("", "An alarm sounds! Sir Ragnar is freed. All remaining monsters, doors and furniture appear and every door is opened. The hero who opened the cell moves the knight after his own turn (1 red die). Sir Ragnar does not attack, defends with 2 dice and has only 2 body points.", "Sir Ragnar found"), Reveal(), SpawnAlly("ragnar") });
            R("ragnar_on_stairs", new[] { ActorOnStairs("ragnar") }, new[] { Complete("rescue_ragnar") });
            R("ragnar_dead", new[] { AllyDied("ragnar") }, new[] { Note("", "Sir Ragnar is dead: the reward is lost.", "Quest failed"), EndQuest(QuestResult.Defeat) });

            TrappedChest("chest_A", zChestTrap, "q2_chest_trap", 1, "A", "This chest hides a poisoned needle: any hero who searches before it is disarmed loses 1 body point. The chest is empty.", Consume());
            FirstSearch("note_B", zChestPotion, "q2_chest_potion", "B", "The first hero to search finds 60 gold coins and a healing potion (restores up to 4 body points).", Loot(gold: 60, cards: new[] { CardHeal4 }));
            End();
        }

        // ═════════════ Q3 ═════════════
        static void Q3()
        {
            const int zArmory = 9, zBuffet = 10;                                  // ADAPT
            Begin(3, "The Lair of the Orc Warlord", "Prince Magnus wants Ulag, the orc warlord who captured Sir Ragnar, dead. Reward: 180 gold coins to share. The treasures of his fortress belong to whoever finds them.");
            q.wanderingMonsterType = Orc;
            q.sharedGoldReward = 180;
            Obj("kill_ulag", "Kill Ulag");
            q.namedMonsters.Add(new NamedMonsterDef { elementId = "q3_ulag", displayName = "Ulag", movement = 10, attackDice = 4, defenseDice = 5, bodyPoints = 2, mindPoints = 3 });

            FirstSearch("note_A", zArmory, "q3_armory", "A", "The orcs' armory. The first hero to search finds a staff here, to keep or to give to another hero (same as the equipment card).", Loot(cards: new[] { CardStaff }));
            FirstSearch("note_B", zBuffet, "q3_buffet", "B", "The first hero to search finds 24 gold coins and a healing potion in the sideboard (restores up to 4 body points).", Loot(gold: 24, cards: new[] { CardHeal4 }));
            R("ulag_dead", new[] { Killed("q3_ulag") }, new[] { Complete("kill_ulag") });
            End();
        }

        // ═════════════ Q4 ═════════════
        static void Q4()
        {
            Begin(4, "Prince Magnus's Gold", "Three of the king's chests were stolen by a band of orcs led by the Chaos Warrior Gulthor. Recover them: 240 gold coins to share.");
            q.wanderingMonsterType = Abomination;
            q.sharedGoldReward = 240;
            q.introNote = "Three sealed chests hold 250 gold coins meant for the prince and the king: the heroes cannot keep them. A hero carries only one chest at a time and then rolls only a single red die for movement.";
            Obj("recover_chests", "Bring back the king's three chests");
            q.namedMonsters.Add(new NamedMonsterDef { elementId = "q4_gulthor", displayName = "Gulthor" });

            for (int i = 1; i <= 3; i++)
                R("pick_chest_" + i, new[] { ItemPicked("q4_chest_" + i) }, new[] { SetFlag("chest_" + i), LimitDice(1) }, once: false);
            R("all_chests", new[] { AllFlags("chest_1", "chest_2", "chest_3") }, new[] { Complete("recover_chests") });
            End();
        }

        // ═════════════ Q5 ═════════════
        static void Q5()
        {
            const int zAlchemy = 11, zChest = 12, zTalisman = 13, zThrone = 14;   // ADAPT
            Begin(5, "Melar's Maze", "The enchanter Melar hid his Talisman of Knowledge in his laboratory, at the heart of a maze riddled with traps and monsters. Find it.");
            q.wanderingMonsterType = Zombie;
            Obj("get_talisman", "Find the Talisman of Knowledge");
            q.namedMonsters.Add(new NamedMonsterDef { elementId = "q5_gargoyle", dormant = true, invulnerableUntilActed = true });

            FirstSearch("note_A", zAlchemy, "q5_bench", "A", "A half-full vial on the workbench: a healing potion (restores up to 2 body points).", Loot(cards: new[] { CardHeal2 }));
            R("note_B", new[] { DoorOpened("q5_door_next") }, new[] { Note("B", "The gargoyle, a stone statue until now, comes to life! It cannot be hurt before it has moved or attacked a hero."), Activate("q5_gargoyle") });
            TrappedChest("chest_C", zChest, "q5_chest_gas", 2, "C", "Toxic gas trap: 2 body points lost if searched before it is disarmed. The chest holds 144 gold coins, and there is no other treasure in the room.", Loot(gold: 144));
            FirstSearch("note_D", zTalisman, "q5_shelf", "D", "The first hero to search discovers the Talisman of Knowledge (artifact).", Loot(artifacts: new[] { ArtTalisman }));
            R("talisman_obj", new[] { Search(zTalisman, "q5_shelf") }, new[] { Complete("get_talisman") }, once: true);
            R("note_E", new[] { Search(zThrone, "q5_throne") }, new[] { Note("E", "Searching the walls reveals nothing, but searching for treasure turns up Melar's key. As soon as it is touched it vanishes and the throne slides aside, revealing a secret door."), Reveal("q5_secret_door"), Consume() });
            End();
        }

        // ═════════════ Q6 ═════════════
        static void Q6()
        {
            const int zCell = 15, zBuffet = 16;                                   // ADAPT
            Begin(6, "The Orc Warlord's Offspring", "Grak, Ulag's son, has captured you to take his revenge. You have picked the lock of your cell: find your equipment and escape through the stairs.");
            q.startMode = HeroStartMode.InRoom; q.startZone = zCell;
            q.exitMode = ExitMode.EachHeroEscapesViaStairs;
            q.noTreasureZones.Add(zCell);
            q.startEquipmentLocked = true; q.unarmedAttackDice = 1; q.unarmedDefenseDice = 2;
            q.wanderingMonsterType = Abomination;
            q.introTitle = "Quest 6";
            q.introNote = "You start in the cell, where searching is not allowed. Your equipment has been confiscated: without it you cannot use it or cast spells. With no weapon or armor you roll 1 die to attack and 2 to defend.";
            q.namedMonsters.Add(new NamedMonsterDef
            {
                elementId = "q6_grak", displayName = "Grak", movement = 8, attackDice = 4, defenseDice = 4, bodyPoints = 3, mindPoints = 3,
                castsSpellInsteadOfAttacking = true, spellIds = new List<string> { "horreur", "sommeil", "tornade" }
            });

            R("equipment_found", new[] { Search(zBuffet, "q6_buffet") },
              new[] { Note("A", "Your equipment is in this sideboard. The hero who searches finds it; each hero must then enter the room to recover his own and get his spells back."), SetFlag("equipment_found"), RestoreEquipment(), Consume() });
            R("recover_equipment", new[] { Entered(zBuffet), Flag("equipment_found") }, new[] { RestoreEquipment() }, once: false, perActor: true);
            R("note_B", new[] { HeroEscaped() }, new[] { Note("B", "These stairs lead out of the dungeon: any hero who reaches them has escaped.") });
            R("grak_cloak", new[] { Killed("q6_grak") }, new[] { Loot(artifacts: new[] { ArtWizardCloak }, consume: false, to: RecipientType.SpecificHeroType, heroType: HeroType.Wizard) });
            End();
        }

        // ═════════════ Q7 ═════════════
        static void Q7()
        {
            const int zArmory = 17, zNeedleChest = 18, zWardoz = 19;              // ADAPT
            Begin(7, "The Missing Enchanter", "Wardoz, the king's enchanter, has vanished: murdered, or seduced by Chaos magic? Uncover the truth and return to the stairs: 100 gold coins each.");
            q.wanderingMonsterType = Mummy;
            q.goldRewardPerHero = 100;
            q.introNote = "The Chaos Warriors in this quest are made of stone: they defend with 1 extra die.";
            q.monsterModifiers.Add(new MonsterModifier { monsterType = ChaosWarrior, defenseDiceBonus = 1 });
            Obj("find_wardoz", "Find out what became of Wardoz");
            q.namedMonsters.Add(new NamedMonsterDef { elementId = "q7_wardoz", displayName = "Wardoz" });

            FirstSearch("note_B", zArmory, "q7_armory", "B", "An armory: the first hero to search finds Borin's Armor (artifact) here.", Loot(artifacts: new[] { ArtBorinArmor }));
            TrappedChest("chest_C", zNeedleChest, "q7_chest_needle", 2, "C", "Poisoned needle: 2 body points lost if searched before it is disarmed. The chest holds a vial of unknown purple liquid (do not reveal its nature): once drunk, the hero turns into a statue for 5 of his turns, motionless and invulnerable.", Loot(cards: new[] { CardStonePotion }));
            R("wardoz_dead", new[] { Killed("q7_wardoz") }, new[] { SetFlag("wardoz_dead"), Complete("find_wardoz") });
            R("note_D", new[] { Search(zWardoz, "q7_wardoz_chest"), Flag("wardoz_dead") }, new[] { Note("D", "Wardoz, turned into a zombie by Chaos magic, is destroyed. The first hero to search finds 144 gold coins and papers on his desk proving what happened to him."), Loot(gold: 144) });
            End();
        }

        // ═════════════ Q8 ═════════════
        static void Q8()
        {
            const int zChest = 20;                                                // ADAPT
            Begin(8, "The Fire Mage", "The orcs of the Black Mountain use fire magic. Balur, the fire mage, helps them: no fire spell can affect him. Go and defeat him in his lair: 100 gold coins each.");
            q.wanderingMonsterType = Abomination;
            q.goldRewardPerHero = 100;
            q.introNote = "Balur knows six spells, one cast each: fireball, firestorm, tornado, summon orcs, dread and teleport (to square XX of the central room). If he has teleported, do not place his miniature in that room until a hero opens its door. Fire spells do not affect him.";
            Obj("kill_balur", "Defeat Balur");
            q.namedMonsters.Add(new NamedMonsterDef
            {
                elementId = "q8_balur", displayName = "Balur", movement = 8, attackDice = 2, defenseDice = 5, bodyPoints = 3, mindPoints = 7,
                spellsOncePerQuest = true, spellImmunities = new List<string> { "fire" },
                spellIds = new List<string> { "boule_de_feu", "tempete_de_feu", "tornade", "invocation_orcs", "horreur", "teleportation" }
            });
            R("balur_dead", new[] { Killed("q8_balur") }, new[] { Complete("kill_balur") });
            FirstSearch("chest", zChest, "q8_chest", "", "The chest contains 150 gold coins and the Magic Wand (artifact).", Loot(gold: 150, artifacts: new[] { ArtWand }));
            End();
        }

        // ═════════════ Q9 ═════════════
        static void Q9()
        {
            const int zStart = 21, zChest1 = 22, zChest2 = 23, zGasChest = 24;    // ADAPT
            Begin(9, "Race Against the Clock", "A guide abandoned you in the dark of an underground maze: it was a trap. Escape by reaching the stairs, or you will perish here.");
            q.startMode = HeroStartMode.InRoom; q.startZone = zStart;
            q.wanderingMonsterType = Abomination;
            FirstSearch("chest_1", zChest1, "q9_chest_1", "B", "This chest contains 100 gold coins.", Loot(gold: 100));
            FirstSearch("chest_2", zChest2, "q9_chest_2", "B", "This chest contains 100 gold coins.", Loot(gold: 100));
            TrappedChest("chest_C", zGasChest, "q9_chest_gas", 3, "C", "Toxic gas trap: 3 body points lost if searched before it is disarmed. The chest contains the Elixir of Life (artifact).", Loot(artifacts: new[] { ArtElixir }));
            End();
        }

        // ═════════════ Q10 ═════════════
        static void Q10()
        {
            const int zRing = 25, zMine = 26;                                     // ADAPT
            Begin(10, "The Mysterious Castle", "The mage Ollar built a magic castle above a gold mine, guarded by monsters frozen in time. Find the entrance to the mine.");
            q.wanderingMonsterNote = "Ollar's ghost appears, cackles like a madman, then disappears. Nothing else happens.";
            q.introNote = "The doors are magic: a hero who walks through one stops immediately, rolls 2 red dice and is teleported to the square bearing that total (if occupied: see the quest book). Only one door passage per turn. A total of 2 or 12 sends him back to the stairs. The quest ends when all monsters are dead or all heroes have returned to the stairs.";

            var tp = Add<TeleportByDiceAction>("Teleport 2d6");
            tp.table.Add(new TeleportByDiceAction.Entry { roll = 2, toStairs = true });
            tp.table.Add(new TeleportByDiceAction.Entry { roll = 12, toStairs = true });
            // TODO: add the 9 other entries (roll 3..11 -> cell) in the asset's inspector.
            R("teleport_doors", new[] { DoorCrossed() }, new[] { tp }, once: false);

            R("warrior_1", new[] { Killed("q10_warrior_1") }, new[] { SetFlag("warrior_1") });
            R("warrior_2", new[] { Killed("q10_warrior_2") }, new[] { SetFlag("warrior_2") });
            R("note_A", new[] { Search(zRing, ""), AllFlags("warrior_1", "warrior_2") }, new[] { Note("A", "Both Chaos Warriors are defeated: one of them was carrying the Ring of Return (artifact)."), Loot(artifacts: new[] { ArtReturnRing }) });
            R("note_B", new[] { Entered(zMine) }, new[] { Note("B", "Entrance to the mine: 5000 gold coins can be taken from here. The carrier can neither attack nor defend; if he drops the gold, he is sent back into the mine. At the end, Zargon reveals that the gold is fake.") });
            R("all_dead", new[] { AllMonstersDead() }, new[] { EndQuest(QuestResult.Victory) });
            End();
        }

        // ═════════════ Q11 ═════════════
        static void Q11()
        {
            const int zArmory = 27, zGargoyle = 28;                               // ADAPT
            Begin(11, "The Fortress of Terror", "Orcs and goblins allied with Zargon are plundering the east. Storm their fortress and kill every monster. Each hero is paid for his victims: 10 gold per goblin, 20 per orc, 30 per abomination, 50 per Chaos Warrior.");
            q.wanderingMonsterType = Abomination;
            q.killBounties.Add(new KillBounty { monsterType = Goblin, gold = 10 });
            q.killBounties.Add(new KillBounty { monsterType = Orc, gold = 20 });
            q.killBounties.Add(new KillBounty { monsterType = Abomination, gold = 30 });
            q.killBounties.Add(new KillBounty { monsterType = ChaosWarrior, gold = 50 });
            Obj("kill_all", "Kill every monster in the fortress");
            q.namedMonsters.Add(new NamedMonsterDef { elementId = "q11_gargoyle", dormant = true, invulnerableUntilActed = true });
            R("all_dead", new[] { AllMonstersDead() }, new[] { Complete("kill_all") });

            FirstSearch("note_A", zArmory, "q11_armory", "A", "An armory: the first hero to search finds a shield here (same as the equipment card). The other weapons are unusable.", Loot(cards: new[] { CardShield }));
            R("note_B_trap", new[] { Search(zGargoyle, "q11_chest"), Flag("disarmed:q11_chest", false), Flag("gargoyle_awake", false) },
              new[] { Note("B", "The chest is trapped: the stone gargoyle comes to life and attacks immediately. It cannot be hurt before it has moved or attacked a hero."), Activate("q11_gargoyle"), SetFlag("gargoyle_awake") });
            R("note_C", new[] { Killed("q11_warrior_sword") }, new[] { Note("C", "This Chaos Warrior carried a magic sword: the hero who killed him may take the Orc's Bane (artifact)."), Loot(artifacts: new[] { ArtOrcBane }, consume: false) });
            End();
        }

        // ═════════════ Q12 ═════════════
        static void Q12()
        {
            const int zTomb = 29, zLibrary = 30;                                  // ADAPT
            Begin(12, "Barak Tor — The Lair of the Sorcerer Lord", "The king needs the Star of the West to unite the kingdom before war breaks out. It lies in Barak Tor, where the Sorcerer Lord sleeps. Reward: 200 gold coins to share.");
            q.wanderingMonsterType = Skeleton;
            q.sharedGoldReward = 200; q.rewardRequiresObjectiveId = "get_star";
            q.introNote = "Some doors are false and will not open. The trapped rockfall near the exit only triggers after the last hero has passed and permanently cuts off the way back to the stairs.";
            Obj("get_star", "Retrieve the Star of the West");
            q.namedMonsters.Add(new NamedMonsterDef { elementId = "q12_zombie" });
            q.namedMonsters.Add(new NamedMonsterDef
            {
                elementId = "q12_sorcerer", displayName = "The Sorcerer Lord", movement = 1, attackDice = 2, dormant = true,
                vulnerableOnlyToArtifactId = ArtMysticSword, immuneToAllSpells = true,
                spellIds = new List<string> { "invocation_morts_vivants", "horreur", "controle", "boule_de_feu" }
            });

            R("note_B", new[] { Killed("q12_zombie") }, new[] { Note("B", "The Star of the West was in this zombie's hand."), Complete("get_star") });
            R("note_D", new[] { Entered(zTomb) }, new[] { Note("D", "You have broken the magic seal holding the Sorcerer Lord. He is awake: flee, for only the Mystic Sword can hurt him."), Reveal("q12_sorcerer"), Activate("q12_sorcerer") });
            FirstSearch("note_E", zLibrary, "q12_library", "E", "Behind the bookcase: the Enchanter's Staff (artifact).", Loot(artifacts: new[] { ArtEnchanterStaff }));

            var rock = Add<AllHeroesVisitedTileCondition>("Last hero has passed");
            rock.tile = new Vector2Int(0, 0);                                     // TODO: rockfall cell
            var seal = Add<SealPassageAction>("Rockfall");                        // TODO: cells to block
            R("note_C", new QuestCondition[] { rock }, new QuestAction[] { Note("C", "The rockfall collapses behind the last hero: the way back is blocked forever."), seal });
            End();
        }

        // ═════════════ Q13 ═════════════
        static void Q13()
        {
            const int zSword = 31, zChest = 32;                                   // ADAPT
            Begin(13, "The Quest for the Mystic Sword", "The Sorcerer Lord is awake and threatens the kingdom. Find the Mystic Sword, the only weapon able to hurt him, in an old ruined temple and bring it back.");
            q.wanderingMonsterType = ChaosWarrior;
            q.introNote = "Special rockfall rule: any hero who stops on a rockfall square rolls 1 red die and loses 1 body point on a 4, 5 or 6 (with a helmet: only on a 6). Monsters are not affected and the passage is not blocked.";
            Obj("get_sword", "Find the Mystic Sword");

            var tiles = Add<HeroOnTileCondition>("Rockfall squares");             // TODO: fill the rockfall cells
            var roll = Add<RollDamageActorAction>("Rockfall die"); roll.damage = 1; roll.threshold = 4; roll.helmetThreshold = 6;
            R("rockfall", new QuestCondition[] { tiles }, new QuestAction[] { roll }, once: false);

            FirstSearch("note_A", zSword, "q13_altar", "A", "The first hero to search discovers the Mystic Sword (artifact).", Loot(artifacts: new[] { ArtMysticSword }));
            R("sword_obj", new[] { Search(zSword, "q13_altar") }, new[] { Complete("get_sword") });
            FirstSearch("note_B", zChest, "q13_chest", "B", "This chest contains 200 gold coins.", Loot(gold: 200));
            End();
        }

        // ═════════════ Q14 ═════════════
        static void Q14()
        {
            Begin(14, "Return to Barak Tor", "Armed with the Mystic Sword, return to Barak Tor and defeat the Sorcerer Lord before he unleashes his undead against the king.");
            q.wanderingMonsterType = Mummy;
            q.introNote = "The Sorcerer Lord's tomb is empty. He is stronger than before: no spell and no weapon other than the Mystic Sword can reach him.";
            q.victoryNote = "The king grants every surviving hero the title of Champion.";
            Obj("kill_lord", "Defeat the Sorcerer Lord");
            q.namedMonsters.Add(new NamedMonsterDef
            {
                elementId = "q14_sorcerer", displayName = "The Sorcerer Lord", movement = 10, attackDice = 5, defenseDice = 6, bodyPoints = 4, mindPoints = 6,
                vulnerableOnlyToArtifactId = ArtMysticSword, immuneToAllSpells = true,
                spellIds = new List<string> { "invocation_morts_vivants", "horreur", "horreur", "boule_de_feu", "controle", "tornade" }
            });
            R("lord_dead", new[] { Killed("q14_sorcerer") },
              new[] { Note("", "The Sorcerer Lord vanishes in a cloud of acrid smoke. When it clears, a metal object rolls across the floor: the Ring of Incantation (artifact).", "Victory over the Sorcerer Lord"), Loot(artifacts: new[] { ArtIncantationRing }, consume: false), Complete("kill_lord") });
            End();
        }

        // ═════════════ Helpers ═════════════
        static void Begin(int number, string title, string story)
        {
            if (!AssetDatabase.IsValidFolder(OutputFolder)) AssetDatabase.CreateFolder("Assets", "Quests");
            string path = OutputFolder + "/Quest_" + number.ToString("00") + ".asset";
            AssetDatabase.DeleteAsset(path);
            q = ScriptableObject.CreateInstance<QuestData>();
            AssetDatabase.CreateAsset(q, path);
            q.questId = "quest_" + number.ToString("00");
            q.questNumber = number;
            q.title = title;
            q.description = story;
            q.levelJson = FindLevelJson(number);   // quest_01 / Quest_1 (exact name, so Quest_1 never matches Quest_10..14)
        }

        static void End() { EditorUtility.SetDirty(q); AssetDatabase.SaveAssets(); }

        static T Add<T>(string name) where T : ScriptableObject
        {
            var o = ScriptableObject.CreateInstance<T>();
            o.name = name;
            AssetDatabase.AddObjectToAsset(o, q);
            return o;
        }

        static void R(string id, QuestCondition[] conditions, QuestAction[] actions, bool once = true, bool perActor = false)
            => q.rules.Add(new QuestRule
            {
                ruleId = id, once = once, oncePerActor = perActor,
                conditions = new List<QuestCondition>(conditions), actions = new List<QuestAction>(actions)
            });

        static void Obj(string id, string description) => q.objectives.Add(new QuestObjective { id = id, description = description });

        // "The first hero who searches here finds ..."
        static void FirstSearch(string id, int zone, string element, string letter, string text, QuestAction loot)
            => R(id, new[] { Search(zone, element) }, new[] { Note(letter, text), loot });

        // Trapped chest: damage if the trap is not disarmed yet; the chest is looted once.
        static void TrappedChest(string id, int zone, string element, int damage, string letter, string text, QuestAction loot)
        {
            string looted = "looted_" + id, disarmed = "disarmed:" + element;
            R(id + "_armed", new[] { Search(zone, element), Flag(looted, false), Flag(disarmed, false) },
              new[] { Note(letter, text), Damage(damage), loot, SetFlag(looted) });
            R(id + "_disarmed", new[] { Search(zone, element), Flag(looted, false), Flag(disarmed, true) },
              new[] { Note(letter, text), loot, SetFlag(looted) });
        }

        // Conditions
        static QuestCondition Search(int zone, string element) { var c = Add<TreasureSearchedCondition>("Search " + element); c.zone = zone; c.elementId = element; return c; }
        static QuestCondition Entered(int zone) { var c = Add<RoomEnteredCondition>("Entered room " + zone); c.zone = zone; return c; }
        static QuestCondition Killed(string element) { var c = Add<MonsterKilledCondition>("Killed: " + element); c.elementId = element; return c; }
        static QuestCondition DoorOpened(string id) { var c = Add<DoorOpenedCondition>("Door opened " + id); c.doorId = id; return c; }
        static QuestCondition DoorCrossed() => Add<DoorCrossedCondition>("Door crossed");
        static QuestCondition HeroEscaped() => Add<HeroEscapedCondition>("Hero escaped");
        static QuestCondition AllMonstersDead() => Add<AllMonstersDeadCondition>("All monsters dead");
        static QuestCondition ActorOnStairs(string id) { var c = Add<ActorOnStairsCondition>("On the stairs: " + id); c.actorId = id; return c; }
        static QuestCondition AllyDied(string id) { var c = Add<AllyDiedCondition>("Ally died: " + id); c.allyId = id; return c; }
        static QuestCondition ItemPicked(string id) { var c = Add<ItemPickedUpCondition>("Picked up " + id); c.itemId = id; return c; }
        static QuestCondition Flag(string flag, bool expected = true) { var c = Add<FlagCondition>("Flag " + flag + "=" + expected); c.flag = flag; c.expected = expected; return c; }
        static QuestCondition AllFlags(params string[] flags) { var c = Add<AllFlagsCondition>("Flags " + string.Join("+", flags)); c.flags.AddRange(flags); return c; }

        // Actions
        static QuestAction Note(string letter, string text, string title = "Note") { var a = Add<ShowNoteAction>("Note " + letter + title); a.noteLetter = letter; a.title = title; a.text = text; return a; }
        static QuestAction Consume() => Add<ConsumeTreasureSearchAction>("Search consumed");
        static QuestAction SetFlag(string flag) { var a = Add<SetFlagAction>("Set " + flag); a.flag = flag; return a; }
        static QuestAction Complete(string id) { var a = Add<CompleteObjectiveAction>("Objective " + id); a.objectiveId = id; return a; }
        static QuestAction Reveal(string element = null)
        {
            if (string.IsNullOrEmpty(element)) return Add<RevealEverythingAction>("Reveal everything");
            var a = Add<RevealElementAction>("Reveal " + element); a.elementId = element; return a;
        }
        static QuestAction Activate(string element) { var a = Add<ActivateMonsterAction>("Activate " + element); a.elementId = element; return a; }
        static QuestAction SpawnAlly(string id) { var a = Add<SpawnAllyAction>("Ally " + id); a.allyId = id; return a; }
        static QuestAction Damage(int n) { var a = Add<DamageActorAction>("Damage " + n); a.amount = n; return a; }
        static QuestAction LimitDice(int n) { var a = Add<LimitMovementDiceAction>("Dice limit " + n); a.maxDice = n; return a; }
        static QuestAction RestoreEquipment() => Add<RestoreEquipmentAction>("Give equipment back");
        static QuestAction EndQuest(QuestResult r) { var a = Add<EndQuestAction>("End " + r); a.result = r; return a; }

        static QuestAction Loot(int gold = 0, string[] cards = null, string[] artifacts = null, bool consume = true,
                                RecipientType to = RecipientType.Actor, HeroType heroType = HeroType.Barbarian)
        {
            var a = Add<GrantLootAction>("Loot");
            a.gold = gold; a.recipient = to; a.heroType = heroType; a.replacesTreasureDraw = consume;
            if (cards != null)
                foreach (string n in cards) { var c = FindAsset<CardSO>(n); if (c != null) a.cards.Add(c); }
            if (artifacts != null) a.artifactIds.AddRange(artifacts);
            return a;
        }

        // Level JSON of quest n: exact file name only (quest_01 or Quest_1), never a substring match.
        static TextAsset FindLevelJson(int n)
        {
            string[] names = { "quest_" + n.ToString("00"), "Quest_" + n };
            foreach (string wanted in names)
            {
                var t = FindExact<TextAsset>(wanted);
                if (t != null) return t;
            }
            Debug.LogWarning("[QuestBuilder] Level JSON of quest " + n + " not found (assign it by hand).");
            return null;
        }

        static T FindAsset<T>(string name) where T : Object
        {
            var asset = FindExact<T>(name);
            if (asset == null) Debug.LogWarning("[QuestBuilder] " + typeof(T).Name + " '" + name + "' not found (assign it by hand).");
            return asset;
        }

        // FindAssets matches substrings ("Staff" would also match "StaffOfFire"): keep only the exact file name.
        static T FindExact<T>(string name) where T : Object
        {
            foreach (string guid in AssetDatabase.FindAssets(name + " t:" + typeof(T).Name))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.Equals(System.IO.Path.GetFileNameWithoutExtension(path), name, System.StringComparison.OrdinalIgnoreCase)) continue;
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null) return asset;
            }
            return null;
        }
    }
}
#endif
