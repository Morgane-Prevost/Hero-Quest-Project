#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HeroQuest.Quests.EditorTools
{
    /// <summary>
    /// Menu: HeroQuest > Générer les quêtes 1 à 14 (écrase).
    /// Creates one QuestData asset per quest in Assets/Quests, with all conditions/actions as sub-assets.
    /// Texts are written in our own words (same convention as CardSO.description).
    ///
    /// !! The zone numbers, element ids ("q3_ulag"...) and tile coordinates below are PLACEHOLDERS:
    /// they must match YOUR level JSON files (zone = room number of the grid map, ids = "id" fields of
    /// doors / furniture / monsters). Anything not found (JSON, card, HeroData) is logged as a warning.
    /// </summary>
    public static class QuestSampleBuilder
    {
        const string OutputFolder = "Assets/Quests";

        // ── Names of your CardSO assets (searched by name in the project) ──
        const string CardStaff = "Staff", CardShield = "Shield";
        const string CardHeal4 = "FixedHealing4", CardHeal2 = "FixedHealing2";   // FixedHealingPotionCardSO assets
        const string CardStonePotion = "StonePotion";

        // ── ArtifactInfo.id of your artifacts (ArtifactManager.Grant) ──
        const string ArtTalisman = "knowledge_talisman", ArtBorinArmor = "borin_armor", ArtWand = "magic_wand",
                     ArtElixir = "life_elixir", ArtReturnRing = "return_ring", ArtOrcBane = "orc_bane",
                     ArtEnchanterStaff = "enchanter_staff", ArtMysticSword = "mystic_sword",
                     ArtIncantationRing = "incantation_ring", ArtWizardCloak = "wizard_cloak";

        // ── Monster types (MonsterData.monsterName; comparison ignores case, spaces and plural) ──
        const string Goblin = "Goblin", Orc = "Orc", Abomination = "Abomination", ChaosWarrior = "Chaos Warrior",
                     Gargoyle = "Gargoyle", Mummy = "Mummy", Zombie = "Zombie", Skeleton = "Skeleton";

        static QuestData q;

        [MenuItem("HeroQuest/Générer les quêtes 1 à 14 (écrase)")]
        static void BuildAll()
        {
            Q1(); Q2(); Q3(); Q4(); Q5(); Q6(); Q7(); Q8(); Q9(); Q10(); Q11(); Q12(); Q13(); Q14();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[QuestBuilder] 14 quêtes générées dans " + OutputFolder + ". Vérifie les warnings et remplace les ids / zones / cases par ceux de tes JSON.");
        }

        // ═════════════ Q1 ═════════════
        static void Q1()
        {
            const int zRack = 2, zEmptyChest = 3, zChest84 = 4, zChest120 = 5;   // ADAPT
            Begin(1, "L'épreuve", "Première épreuve des apprentis héros : descendre dans les catacombes de la tombe de Fellmarg, trouver et détruire Verag, la gargouille qui y rôde. Seule l'entraide permettra de survivre.");
            q.wanderingMonsterType = Orc;
            q.introNote = "Cette quête ne contient ni piège ni porte secrète. Elle n'en reste pas moins périlleuse : votre survie dépend de votre travail d'équipe.";
            Obj("kill_verag", "Détruire Verag");
            q.namedMonsters.Add(new NamedMonsterDef { elementId = "q1_mummy", displayName = "Gardienne de Fellmarg", attackDice = 4 });

            R("note_A", new[] { Search(zRack, "q1_rack") }, new[] { Note("A", "Les armes du râtelier sont rouillées ou brisées : elles ne servent à rien."), Consume() }, once: false);
            R("note_B", new[] { Search(zEmptyChest, "q1_chest_empty") }, new[] { Note("B", "Ce coffre est vide."), Consume() }, once: false);
            FirstSearch("note_D", zChest84, "q1_chest_84", "D", "Le premier héros qui fouille ici trouve 84 pièces d'or dans le coffre.", Loot(gold: 84));
            FirstSearch("note_E", zChest120, "q1_chest_120", "E", "Le premier héros qui fouille ici trouve 120 pièces d'or dans le coffre.", Loot(gold: 120));
            R("verag_dead", new[] { Killed("q1_verag") }, new[] { Complete("kill_verag") });
            End();
        }

        // ═════════════ Q2 ═════════════
        static void Q2()
        {
            const int zCell = 6, zChestTrap = 7, zChestPotion = 8;                // ADAPT
            Begin(2, "À la rescousse de Sir Ragnar", "Sir Ragnar, chevalier du roi, est prisonnier d'Ulag. Retrouvez-le et ramenez-le vivant à l'escalier : 240 pièces d'or à partager, mais rien si le chevalier meurt pendant l'évasion.");
            q.wanderingMonsterType = Orc;
            q.sharedGoldReward = 240; q.rewardRequiresObjectiveId = "rescue_ragnar";
            q.noTreasureZones.Add(zCell);
            Obj("rescue_ragnar", "Ramener Sir Ragnar vivant à l'escalier");
            q.allies.Add(new AllyDef { id = "ragnar", displayName = "Sir Ragnar", template = FindAsset<HeroData>("Ragnar"), spawnCell = new Vector2Int(0, 0) });   // ADAPT: body 2, defense 2, attack 0, 1 red die

            R("ragnar_found", new[] { DoorOpened("q2_cell_door") },
              new[] { Note("", "Une alarme retentit ! Sir Ragnar est libéré. Tous les monstres, portes et meubles restants apparaissent et toutes les portes sont ouvertes. Le héros qui a ouvert la cellule déplace le chevalier après son propre tour (1 dé rouge). Sir Ragnar n'attaque pas, défend avec 2 dés et n'a que 2 points de vie.", "Sir Ragnar est trouvé"), Reveal(), SpawnAlly("ragnar") });
            R("ragnar_on_stairs", new[] { ActorOnStairs("ragnar") }, new[] { Complete("rescue_ragnar") });
            R("ragnar_dead", new[] { AllyDied("ragnar") }, new[] { Note("", "Sir Ragnar est mort : la récompense est perdue.", "Quête échouée"), EndQuest(QuestResult.Defeat) });

            TrappedChest("chest_A", zChestTrap, "q2_chest_trap", 1, "A", "Ce coffre cache une aiguille empoisonnée : tout héros qui fouille avant le désarmement perd 1 point de vie. Le coffre est vide.", Consume());
            FirstSearch("note_B", zChestPotion, "q2_chest_potion", "B", "Le premier héros qui fouille trouve 60 pièces d'or et une potion de guérison (jusqu'à 4 points de vie rendus).", Loot(gold: 60, cards: new[] { CardHeal4 }));
            End();
        }

        // ═════════════ Q3 ═════════════
        static void Q3()
        {
            const int zArmory = 9, zBuffet = 10;                                  // ADAPT
            Begin(3, "Le repaire du seigneur de guerre orc", "Le prince Magnus veut la mort d'Ulag, seigneur de guerre orc, responsable de la capture de Sir Ragnar. Récompense : 180 pièces d'or à partager. Les trésors de sa forteresse reviennent à celui qui les trouve.");
            q.wanderingMonsterType = Orc;
            q.sharedGoldReward = 180;
            Obj("kill_ulag", "Tuer Ulag");
            q.namedMonsters.Add(new NamedMonsterDef { elementId = "q3_ulag", displayName = "Ulag", movement = 10, attackDice = 4, defenseDice = 5, bodyPoints = 2, mindPoints = 3 });

            FirstSearch("note_A", zArmory, "q3_armory", "A", "L'armurerie des orcs. Le premier héros qui fouille y trouve un bâton, à garder ou à donner à un autre héros (identique à la carte d'équipement).", Loot(cards: new[] { CardStaff }));
            FirstSearch("note_B", zBuffet, "q3_buffet", "B", "Le premier héros qui fouille trouve 24 pièces d'or et une potion de guérison dans le buffet (jusqu'à 4 points de vie rendus).", Loot(gold: 24, cards: new[] { CardHeal4 }));
            R("ulag_dead", new[] { Killed("q3_ulag") }, new[] { Complete("kill_ulag") });
            End();
        }

        // ═════════════ Q4 ═════════════
        static void Q4()
        {
            Begin(4, "L'or du prince Magnus", "Trois coffres du roi ont été volés par une bande d'orcs menée par le guerrier du Chaos Gulthor. Récupérez-les : 240 pièces d'or à partager.");
            q.wanderingMonsterType = Abomination;
            q.sharedGoldReward = 240;
            q.introNote = "Trois coffres scellés contiennent 250 pièces d'or destinées au prince et au roi : les héros ne peuvent pas les garder. Un héros ne porte qu'un coffre à la fois et ne lance alors qu'un seul dé rouge de déplacement.";
            Obj("recover_chests", "Rapporter les trois coffres du roi");
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
            Begin(5, "Le labyrinthe de Melar", "L'enchanteur Melar a caché son Talisman de la connaissance dans son laboratoire, au cœur d'un labyrinthe truffé de pièges et de monstres. Trouvez-le.");
            q.wanderingMonsterType = Zombie;
            Obj("get_talisman", "Trouver le Talisman de la connaissance");
            q.namedMonsters.Add(new NamedMonsterDef { elementId = "q5_gargoyle", dormant = true, invulnerableUntilActed = true });

            FirstSearch("note_A", zAlchemy, "q5_bench", "A", "Une fiole à moitié pleine sur l'établi : une potion de guérison (jusqu'à 2 points de vie rendus).", Loot(cards: new[] { CardHeal2 }));
            R("note_B", new[] { DoorOpened("q5_door_next") }, new[] { Note("B", "La gargouille, jusque-là statue de pierre, s'anime ! Elle ne peut être blessée avant de s'être déplacée ou d'avoir attaqué un héros."), Activate("q5_gargoyle") });
            TrappedChest("chest_C", zChest, "q5_chest_gas", 2, "C", "Piège à gaz toxique : 2 points de vie perdus si on fouille avant le désarmement. Le coffre contient 144 pièces d'or, et aucun autre trésor dans la pièce.", Loot(gold: 144));
            FirstSearch("note_D", zTalisman, "q5_shelf", "D", "Le premier héros qui fouille découvre le Talisman de la connaissance (artefact).", Loot(artifacts: new[] { ArtTalisman }));
            R("talisman_obj", new[] { Search(zTalisman, "q5_shelf") }, new[] { Complete("get_talisman") }, once: true);
            R("note_E", new[] { Search(zThrone, "q5_throne") }, new[] { Note("E", "Fouiller les murs ne révèle rien, mais fouiller les trésors fait trouver la clé de Melar. Dès qu'elle est touchée, elle disparaît et le trône glisse, dévoilant une porte secrète."), Reveal("q5_secret_door"), Consume() });
            End();
        }

        // ═════════════ Q6 ═════════════
        static void Q6()
        {
            const int zCell = 15, zBuffet = 16;                                   // ADAPT
            Begin(6, "La descendance du seigneur de guerre orc", "Grak, fils d'Ulag, vous a capturés pour se venger. Vous avez crocheté votre cellule : retrouvez votre équipement et évadez-vous par l'escalier.");
            q.startMode = HeroStartMode.InRoom; q.startZone = zCell;
            q.exitMode = ExitMode.EachHeroEscapesViaStairs;
            q.noTreasureZones.Add(zCell);
            q.startEquipmentLocked = true; q.unarmedAttackDice = 1; q.unarmedDefenseDice = 2;
            q.wanderingMonsterType = Abomination;
            q.introTitle = "Quête 6";
            q.introNote = "Vous commencez dans la cellule, où l'on ne peut pas fouiller. Votre équipement est confisqué : sans lui, impossible de l'utiliser ou de lancer des sorts. Sans arme ni armure, on lance 1 dé en attaque et 2 en défense.";
            q.namedMonsters.Add(new NamedMonsterDef
            {
                elementId = "q6_grak", displayName = "Grak", movement = 8, attackDice = 4, defenseDice = 4, bodyPoints = 3, mindPoints = 3,
                castsSpellInsteadOfAttacking = true, spellIds = new List<string> { "horreur", "sommeil", "tornade" }
            });

            R("equipment_found", new[] { Search(zBuffet, "q6_buffet") },
              new[] { Note("A", "Votre équipement est dans ce buffet. Le héros qui fouille le trouve ; chaque héros doit ensuite entrer dans la pièce pour récupérer le sien et retrouver ses sorts."), SetFlag("equipment_found"), RestoreEquipment(), Consume() });
            R("recover_equipment", new[] { Entered(zBuffet), Flag("equipment_found") }, new[] { RestoreEquipment() }, once: false, perActor: true);
            R("note_B", new[] { HeroEscaped() }, new[] { Note("B", "Cet escalier mène hors du donjon : tout héros qui l'atteint s'est échappé.") });
            R("grak_cloak", new[] { Killed("q6_grak") }, new[] { Loot(artifacts: new[] { ArtWizardCloak }, consume: false, to: RecipientType.SpecificHeroType, heroType: HeroType.Wizard) });
            End();
        }

        // ═════════════ Q7 ═════════════
        static void Q7()
        {
            const int zArmory = 17, zNeedleChest = 18, zWardoz = 19;              // ADAPT
            Begin(7, "L'enchanteur disparu", "Wardoz, enchanteur du roi, a disparu : assassiné, ou séduit par la magie du Chaos ? Découvrez la vérité et revenez à l'escalier : 100 pièces d'or chacun.");
            q.wanderingMonsterType = Mummy;
            q.goldRewardPerHero = 100;
            q.introNote = "Les guerriers du Chaos de cette quête sont de pierre : ils défendent avec 1 dé de plus.";
            q.monsterModifiers.Add(new MonsterModifier { monsterType = ChaosWarrior, defenseDiceBonus = 1 });
            Obj("find_wardoz", "Découvrir ce qu'est devenu Wardoz");
            q.namedMonsters.Add(new NamedMonsterDef { elementId = "q7_wardoz", displayName = "Wardoz" });

            FirstSearch("note_B", zArmory, "q7_armory", "B", "Une armurerie : le premier héros qui fouille y trouve l'Armure de Borin (artefact).", Loot(artifacts: new[] { ArtBorinArmor }));
            TrappedChest("chest_C", zNeedleChest, "q7_chest_needle", 2, "C", "Aiguille empoisonnée : 2 points de vie perdus si on fouille avant le désarmement. Le coffre contient une fiole de liquide violet inconnu (ne pas révéler sa nature) : une fois bue, le héros devient une statue pendant 5 de ses tours, immobile et invulnérable.", Loot(cards: new[] { CardStonePotion }));
            R("wardoz_dead", new[] { Killed("q7_wardoz") }, new[] { SetFlag("wardoz_dead"), Complete("find_wardoz") });
            R("note_D", new[] { Search(zWardoz, "q7_wardoz_chest"), Flag("wardoz_dead") }, new[] { Note("D", "Wardoz, changé en zombie par la magie du Chaos, est détruit. Le premier héros qui fouille trouve 144 pièces d'or et des papiers sur son pupitre qui prouvent ce qui lui est arrivé."), Loot(gold: 144) });
            End();
        }

        // ═════════════ Q8 ═════════════
        static void Q8()
        {
            const int zChest = 20;                                                // ADAPT
            Begin(8, "Le mage du feu", "Les orcs de la Montagne noire utilisent la magie du feu. Balur, le mage du feu, les aide : aucun sort de feu ne l'affecte. Allez le vaincre dans son repaire : 100 pièces d'or chacun.");
            q.wanderingMonsterType = Abomination;
            q.goldRewardPerHero = 100;
            q.introNote = "Balur connaît six sorts, un seul lancer chacun : boule de feu, tempête de feu, tornade, invocation d'orcs, horreur et téléportation (vers la case XX de la salle centrale). S'il s'est téléporté, ne placez pas sa figurine dans cette salle avant qu'un héros n'en ouvre la porte. Les sorts de feu ne l'affectent pas.";
            Obj("kill_balur", "Vaincre Balur");
            q.namedMonsters.Add(new NamedMonsterDef
            {
                elementId = "q8_balur", displayName = "Balur", movement = 8, attackDice = 2, defenseDice = 5, bodyPoints = 3, mindPoints = 7,
                spellsOncePerQuest = true, spellImmunities = new List<string> { "fire" },
                spellIds = new List<string> { "boule_de_feu", "tempete_de_feu", "tornade", "invocation_orcs", "horreur", "teleportation" }
            });
            R("balur_dead", new[] { Killed("q8_balur") }, new[] { Complete("kill_balur") });
            FirstSearch("chest", zChest, "q8_chest", "", "Le coffre contient 150 pièces d'or et la Baguette magique (artefact).", Loot(gold: 150, artifacts: new[] { ArtWand }));
            End();
        }

        // ═════════════ Q9 ═════════════
        static void Q9()
        {
            const int zStart = 21, zChest1 = 22, zChest2 = 23, zGasChest = 24;    // ADAPT
            Begin(9, "Course contre la montre", "Un guide vous a abandonnés dans le noir d'un labyrinthe souterrain : c'était un piège. Échappez-vous en atteignant l'escalier, ou vous périrez ici.");
            q.startMode = HeroStartMode.InRoom; q.startZone = zStart;
            q.wanderingMonsterType = Abomination;
            FirstSearch("chest_1", zChest1, "q9_chest_1", "B", "Ce coffre contient 100 pièces d'or.", Loot(gold: 100));
            FirstSearch("chest_2", zChest2, "q9_chest_2", "B", "Ce coffre contient 100 pièces d'or.", Loot(gold: 100));
            TrappedChest("chest_C", zGasChest, "q9_chest_gas", 3, "C", "Piège à gaz toxique : 3 points de vie perdus si on fouille avant le désarmement. Le coffre contient l'Élixir de vie (artefact).", Loot(artifacts: new[] { ArtElixir }));
            End();
        }

        // ═════════════ Q10 ═════════════
        static void Q10()
        {
            const int zRing = 25, zMine = 26;                                     // ADAPT
            Begin(10, "Le château mystérieux", "Le mage Ollar a bâti un château magique au-dessus d'une mine d'or, gardé par des monstres figés dans le temps. Trouvez l'entrée de la mine.");
            q.wanderingMonsterNote = "Le fantôme d'Ollar apparaît, ricane comme un fou, puis disparaît. Rien d'autre ne se passe.";
            q.introNote = "Les portes sont magiques : un héros qui en franchit une s'arrête aussitôt, lance 2 dés rouges et est téléporté sur la case portant ce total (occupée : voir le livre de quêtes). Un seul passage de porte par tour. Un total de 2 ou 12 ramène à l'escalier. La quête s'achève quand tous les monstres sont morts ou que tous les héros sont revenus à l'escalier.";

            var tp = Add<TeleportByDiceAction>("Téléportation 2d6");
            tp.table.Add(new TeleportByDiceAction.Entry { roll = 2, toStairs = true });
            tp.table.Add(new TeleportByDiceAction.Entry { roll = 12, toStairs = true });
            // TODO: add the 9 other entries (roll 3..11 -> cell) in the asset's inspector.
            R("teleport_doors", new[] { DoorCrossed() }, new[] { tp }, once: false);

            R("warrior_1", new[] { Killed("q10_warrior_1") }, new[] { SetFlag("warrior_1") });
            R("warrior_2", new[] { Killed("q10_warrior_2") }, new[] { SetFlag("warrior_2") });
            R("note_A", new[] { Search(zRing, ""), AllFlags("warrior_1", "warrior_2") }, new[] { Note("A", "Les deux guerriers du Chaos sont vaincus : l'un d'eux portait l'Anneau du retour (artefact)."), Loot(artifacts: new[] { ArtReturnRing }) });
            R("note_B", new[] { Entered(zMine) }, new[] { Note("B", "Entrée de la mine : on peut y prendre 5000 pièces d'or. Le porteur ne peut ni attaquer ni se défendre ; s'il pose l'or, il retourne dans la mine. À la fin, Zargon révèle que l'or est faux.") });
            R("all_dead", new[] { AllMonstersDead() }, new[] { EndQuest(QuestResult.Victory) });
            End();
        }

        // ═════════════ Q11 ═════════════
        static void Q11()
        {
            const int zArmory = 27, zGargoyle = 28;                               // ADAPT
            Begin(11, "Le Bastion de la Terreur", "Des orcs et gobelins alliés de Zargon pillent l'est. Prenez d'assaut leur forteresse et tuez tous les monstres. Chaque héros est payé pour ses victimes : 10 po par gobelin, 20 par orc, 30 par abomination, 50 par guerrier du Chaos.");
            q.wanderingMonsterType = Abomination;
            q.killBounties.Add(new KillBounty { monsterType = Goblin, gold = 10 });
            q.killBounties.Add(new KillBounty { monsterType = Orc, gold = 20 });
            q.killBounties.Add(new KillBounty { monsterType = Abomination, gold = 30 });
            q.killBounties.Add(new KillBounty { monsterType = ChaosWarrior, gold = 50 });
            Obj("kill_all", "Tuer tous les monstres de la forteresse");
            q.namedMonsters.Add(new NamedMonsterDef { elementId = "q11_gargoyle", dormant = true, invulnerableUntilActed = true });
            R("all_dead", new[] { AllMonstersDead() }, new[] { Complete("kill_all") });

            FirstSearch("note_A", zArmory, "q11_armory", "A", "Une armurerie : le premier héros qui fouille y trouve un bouclier (identique à la carte d'équipement). Les autres armes sont inutilisables.", Loot(cards: new[] { CardShield }));
            R("note_B_trap", new[] { Search(zGargoyle, "q11_chest"), Flag("disarmed:q11_chest", false), Flag("gargoyle_awake", false) },
              new[] { Note("B", "Le coffre est piégé : la gargouille de pierre prend vie et attaque immédiatement. Elle ne peut être blessée avant de s'être déplacée ou d'avoir attaqué un héros."), Activate("q11_gargoyle"), SetFlag("gargoyle_awake") });
            R("note_C", new[] { Killed("q11_warrior_sword") }, new[] { Note("C", "Ce guerrier du Chaos portait une épée magique : le héros qui l'a tué peut prendre la Pourfendeuse d'orcs (artefact)."), Loot(artifacts: new[] { ArtOrcBane }, consume: false) });
            End();
        }

        // ═════════════ Q12 ═════════════
        static void Q12()
        {
            const int zTomb = 29, zLibrary = 30;                                  // ADAPT
            Begin(12, "Barak Tor — L'antre du Seigneur sorcier", "Le roi a besoin de l'Étoile de l'Ouest pour unir le royaume avant la guerre. Elle repose à Barak Tor, là où dort le Seigneur sorcier. Récompense : 200 pièces d'or à partager.");
            q.wanderingMonsterType = Skeleton;
            q.sharedGoldReward = 200; q.rewardRequiresObjectiveId = "get_star";
            q.introNote = "Certaines portes sont fausses et ne s'ouvrent pas. L'éboulement piégé près de la sortie ne se déclenche qu'après le passage du dernier héros et coupe définitivement le retour vers l'escalier.";
            Obj("get_star", "Récupérer l'Étoile de l'Ouest");
            q.namedMonsters.Add(new NamedMonsterDef { elementId = "q12_zombie" });
            q.namedMonsters.Add(new NamedMonsterDef
            {
                elementId = "q12_sorcerer", displayName = "Le Seigneur sorcier", movement = 1, attackDice = 2, dormant = true,
                vulnerableOnlyToArtifactId = ArtMysticSword, immuneToAllSpells = true,
                spellIds = new List<string> { "invocation_morts_vivants", "horreur", "controle", "boule_de_feu" }
            });

            R("note_B", new[] { Killed("q12_zombie") }, new[] { Note("B", "L'Étoile de l'Ouest était dans la main de ce zombie."), Complete("get_star") });
            R("note_D", new[] { Entered(zTomb) }, new[] { Note("D", "Vous avez brisé le sceau magique qui retenait le Seigneur sorcier. Il est réveillé : fuyez, car seule l'Épée mystique peut le blesser."), Reveal("q12_sorcerer"), Activate("q12_sorcerer") });
            FirstSearch("note_E", zLibrary, "q12_library", "E", "Derrière la bibliothèque : le Bâton de l'enchanteur (artefact).", Loot(artifacts: new[] { ArtEnchanterStaff }));

            var rock = Add<AllHeroesVisitedTileCondition>("Dernier héros passé");
            rock.tile = new Vector2Int(0, 0);                                     // TODO: rockfall cell
            var seal = Add<SealPassageAction>("Éboulement");                      // TODO: cells to block
            R("note_C", new QuestCondition[] { rock }, new QuestAction[] { Note("C", "L'éboulement s'effondre derrière le dernier héros : le chemin du retour est bloqué à jamais."), seal });
            End();
        }

        // ═════════════ Q13 ═════════════
        static void Q13()
        {
            const int zSword = 31, zChest = 32;                                   // ADAPT
            Begin(13, "La quête de l'Épée mystique", "Le Seigneur sorcier est réveillé et menace le royaume. Trouvez dans un vieux temple en ruine l'Épée mystique, seule arme capable de le blesser, et rapportez-la.");
            q.wanderingMonsterType = ChaosWarrior;
            q.introNote = "Règle d'éboulement spéciale : tout héros qui s'arrête sur une case d'éboulement lance 1 dé rouge et perd 1 point de vie sur 4, 5 ou 6 (avec un casque : seulement sur 6). Les monstres n'y sont pas sensibles et le passage n'est pas bloqué.";
            Obj("get_sword", "Trouver l'Épée mystique");

            var tiles = Add<HeroOnTileCondition>("Cases d'éboulement");           // TODO: fill the rockfall cells
            var roll = Add<RollDamageActorAction>("Dé d'éboulement"); roll.damage = 1; roll.threshold = 4; roll.helmetThreshold = 6;
            R("rockfall", new QuestCondition[] { tiles }, new QuestAction[] { roll }, once: false);

            FirstSearch("note_A", zSword, "q13_altar", "A", "Le premier héros qui fouille découvre l'Épée mystique (artefact).", Loot(artifacts: new[] { ArtMysticSword }));
            R("sword_obj", new[] { Search(zSword, "q13_altar") }, new[] { Complete("get_sword") });
            FirstSearch("note_B", zChest, "q13_chest", "B", "Ce coffre contient 200 pièces d'or.", Loot(gold: 200));
            End();
        }

        // ═════════════ Q14 ═════════════
        static void Q14()
        {
            Begin(14, "Retour à Barak Tor", "Armés de l'Épée mystique, retournez à Barak Tor vaincre le Seigneur sorcier avant qu'il ne lance ses morts-vivants contre le roi.");
            q.wanderingMonsterType = Mummy;
            q.introNote = "La tombe du Seigneur sorcier est vide. Il est plus fort qu'avant : aucun sort ni aucune arme autre que l'Épée mystique ne peut l'atteindre.";
            q.victoryNote = "Le roi accorde à tous les héros survivants le titre de Champion.";
            Obj("kill_lord", "Vaincre le Seigneur sorcier");
            q.namedMonsters.Add(new NamedMonsterDef
            {
                elementId = "q14_sorcerer", displayName = "Le Seigneur sorcier", movement = 10, attackDice = 5, defenseDice = 6, bodyPoints = 4, mindPoints = 6,
                vulnerableOnlyToArtifactId = ArtMysticSword, immuneToAllSpells = true,
                spellIds = new List<string> { "invocation_morts_vivants", "horreur", "horreur", "boule_de_feu", "controle", "tornade" }
            });
            R("lord_dead", new[] { Killed("q14_sorcerer") },
              new[] { Note("", "Le Seigneur sorcier s'évanouit dans un nuage de fumée âcre. Quand elle se dissipe, un objet métallique roule au sol : l'Anneau d'incantation (artefact).", "Victoire sur le Seigneur sorcier"), Loot(artifacts: new[] { ArtIncantationRing }, consume: false), Complete("kill_lord") });
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
            q.levelJson = FindAsset<TextAsset>("quest_" + number.ToString("00"));   // your LevelData JSON
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
        static QuestCondition Search(int zone, string element) { var c = Add<TreasureSearchedCondition>("Fouille " + element); c.zone = zone; c.elementId = element; return c; }
        static QuestCondition Entered(int zone) { var c = Add<RoomEnteredCondition>("Entrée salle " + zone); c.zone = zone; return c; }
        static QuestCondition Killed(string element) { var c = Add<MonsterKilledCondition>("Tué : " + element); c.elementId = element; return c; }
        static QuestCondition DoorOpened(string id) { var c = Add<DoorOpenedCondition>("Porte ouverte " + id); c.doorId = id; return c; }
        static QuestCondition DoorCrossed() => Add<DoorCrossedCondition>("Porte franchie");
        static QuestCondition HeroEscaped() => Add<HeroEscapedCondition>("Héros évadé");
        static QuestCondition AllMonstersDead() => Add<AllMonstersDeadCondition>("Tous les monstres morts");
        static QuestCondition ActorOnStairs(string id) { var c = Add<ActorOnStairsCondition>("Sur l'escalier : " + id); c.actorId = id; return c; }
        static QuestCondition AllyDied(string id) { var c = Add<AllyDiedCondition>("Allié mort : " + id); c.allyId = id; return c; }
        static QuestCondition ItemPicked(string id) { var c = Add<ItemPickedUpCondition>("Ramassé " + id); c.itemId = id; return c; }
        static QuestCondition Flag(string flag, bool expected = true) { var c = Add<FlagCondition>("Flag " + flag + "=" + expected); c.flag = flag; c.expected = expected; return c; }
        static QuestCondition AllFlags(params string[] flags) { var c = Add<AllFlagsCondition>("Flags " + string.Join("+", flags)); c.flags.AddRange(flags); return c; }

        // Actions
        static QuestAction Note(string letter, string text, string title = "Note") { var a = Add<ShowNoteAction>("Note " + letter + title); a.noteLetter = letter; a.title = title; a.text = text; return a; }
        static QuestAction Consume() => Add<ConsumeTreasureSearchAction>("Fouille consommée");
        static QuestAction SetFlag(string flag) { var a = Add<SetFlagAction>("Set " + flag); a.flag = flag; return a; }
        static QuestAction Complete(string id) { var a = Add<CompleteObjectiveAction>("Objectif " + id); a.objectiveId = id; return a; }
        static QuestAction Reveal(string element = null)
        {
            if (string.IsNullOrEmpty(element)) return Add<RevealEverythingAction>("Tout révéler");
            var a = Add<RevealElementAction>("Révéler " + element); a.elementId = element; return a;
        }
        static QuestAction Activate(string element) { var a = Add<ActivateMonsterAction>("Activer " + element); a.elementId = element; return a; }
        static QuestAction SpawnAlly(string id) { var a = Add<SpawnAllyAction>("Allié " + id); a.allyId = id; return a; }
        static QuestAction Damage(int n) { var a = Add<DamageActorAction>("Dégâts " + n); a.amount = n; return a; }
        static QuestAction LimitDice(int n) { var a = Add<LimitMovementDiceAction>("Limite dés " + n); a.maxDice = n; return a; }
        static QuestAction RestoreEquipment() => Add<RestoreEquipmentAction>("Rendre l'équipement");
        static QuestAction EndQuest(QuestResult r) { var a = Add<EndQuestAction>("Fin " + r); a.result = r; return a; }

        static QuestAction Loot(int gold = 0, string[] cards = null, string[] artifacts = null, bool consume = true,
                                RecipientType to = RecipientType.Actor, HeroType heroType = HeroType.Barbarian)
        {
            var a = Add<GrantLootAction>("Butin");
            a.gold = gold; a.recipient = to; a.heroType = heroType; a.replacesTreasureDraw = consume;
            if (cards != null)
                foreach (string n in cards) { var c = FindAsset<CardSO>(n); if (c != null) a.cards.Add(c); }
            if (artifacts != null) a.artifactIds.AddRange(artifacts);
            return a;
        }

        static T FindAsset<T>(string name) where T : Object
        {
            foreach (string guid in AssetDatabase.FindAssets(name + " t:" + typeof(T).Name))
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null) return asset;
            }
            Debug.LogWarning("[QuestBuilder] " + typeof(T).Name + " '" + name + "' introuvable (à assigner à la main).");
            return null;
        }
    }
}
#endif
