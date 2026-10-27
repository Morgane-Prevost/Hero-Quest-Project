#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HeroQuest.Quests.EditorTools
{
    /// <summary>
    /// Menu Unity : HeroQuest > Générer quêtes 3 et 6.
    /// Crée un asset QuestData par quête, avec toutes ses conditions/actions en sous-assets.
    /// ⚠ Régénérer écrase les assets existants.
    /// </summary>
    public static class QuestSampleBuilder
    {
        // ───────── À REMPLACER par tes vrais noms / ids ─────────
        const string OutputFolder = "Assets/Quests";

        // JSON de map (nom du fichier TextAsset)
        const string MapQuest3 = "quest_03";
        const string MapQuest6 = "quest_06";

        // Noms de tes SO de cartes (recherche par nom dans le projet)
        const string CardStaff        = "Baton";
        const string CardHealPotion   = "PotionGuerison";
        const string CardWizardCloak  = "CapeMagicien";
        const string SpellFear        = "Horreur";
        const string SpellSleep       = "Sommeil";
        const string SpellTornado     = "Tornade";

        // Ids de pièces tels que dans tes JSON de map
        const string Q3_RoomArmory = "room_A";   // note A : armurerie
        const string Q3_RoomBuffet = "room_B";   // note B : pièce au buffet
        const string Q6_RoomCell   = "cellule";
        const string Q6_RoomBuffet = "room_A";   // note A : buffet avec l'équipement

        // Ids de classes de héros
        const string HeroWizard = "wizard";

        static readonly Vector2Int StairsQ3 = new(0, 0);  // TODO coordonnées réelles
        static readonly Vector2Int StairsQ6 = new(0, 0);  // TODO
        static readonly Vector2Int UlagTile = new(0, 0);  // TODO
        static readonly Vector2Int GrakTile = new(0, 0);  // TODO

        [MenuItem("HeroQuest/Générer quêtes 3 et 6 (écrase)")]
        static void BuildAll()
        {
            BuildQuest3();
            BuildQuest6();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[QuestBuilder] Quêtes 3 et 6 générées dans " + OutputFolder +
                      ". Vérifie les warnings : les SO / JSON introuvables sont à assigner à la main.");
        }

        // ═════════════════════ QUÊTE 3 ═════════════════════
        static void BuildQuest3()
        {
            var q = NewQuest("Quest_03_RepaireOrc");
            q.questId = "quest_03"; q.questNumber = 3;
            q.title = "Le repaire du seigneur de guerre orc";
            q.description =
                "« Le prince Magnus a ordonné la recherche et la mise à mort d'Ulag, le seigneur de guerre orc, " +
                "pour son rôle dans l'emprisonnement de Sir Ragnar. Une récompense de 180 pièces d'or est offerte " +
                "aux héros (à partager entre eux) pour la mort d'Ulag. Tout trésor trouvé dans la forteresse " +
                "d'Ulag appartiendra au héros qui l'a trouvé. »";
            q.mapJson = FindText(MapQuest3);
            q.stairsPosition = StairsQ3;
            q.startMode = HeroStartMode.OnStairs;
            q.exitMode = ExitMode.AllSurvivorsReturnToStairs;
            q.sharedGoldReward = 180;

            q.objectives.Add(new QuestObjective { id = "kill_ulag", description = "Tuer Ulag, le seigneur de guerre orc" });

            q.namedMonsters.Add(new NamedMonsterDef
            {
                instanceId = "ulag", displayName = "Ulag", baseMonsterId = "orc",   // figurine : orc à la grosse épée
                tile = UlagTile,
                movement = 10, attackDice = 4, defenseDice = 5, bodyPoints = 2, mindPoints = 3
            });

            // Note A : armurerie -> bâton au premier héros qui fouille
            var searchA = Add<TreasureSearchedCondition>(q, "Fouille armurerie"); searchA.roomId = Q3_RoomArmory;
            var noteA = Add<ShowNoteAction>(q, "Note A"); noteA.noteLetter = "A";
            noteA.text = "Ceci est l'armurerie des orcs. Le premier héros à y chercher des trésors y trouvera un bâton " +
                         "pouvant être ajouté à sa feuille de personnage ou donné à un autre héros. " +
                         "Le bâton est identique à celui des cartes Équipement.";
            var lootA = Add<GrantLootAction>(q, "Butin A - bâton"); AddCard(lootA, CardStaff);
            q.rules.Add(Rule("note_A", new QuestCondition[] { searchA }, new QuestAction[] { noteA, lootA }));

            // Note B : buffet -> 24 po + potion de guérison au premier héros qui fouille
            var searchB = Add<TreasureSearchedCondition>(q, "Fouille buffet"); searchB.roomId = Q3_RoomBuffet;
            var noteB = Add<ShowNoteAction>(q, "Note B"); noteB.noteLetter = "B";
            noteB.text = "Le premier héros à chercher des trésors dans la pièce trouve 24 pièces d'or et une potion de " +
                         "guérison dans le buffet. Une fois bue, la potion fait regagner jusqu'à 4 points de vie perdus.";
            var lootB = Add<GrantLootAction>(q, "Butin B - 24 po + potion"); lootB.gold = 24; AddCard(lootB, CardHealPotion);
            q.rules.Add(Rule("note_B", new QuestCondition[] { searchB }, new QuestAction[] { noteB, lootB }));

            // Objectif : Ulag mort
            var ulagDead = Add<MonsterKilledCondition>(q, "Ulag mort"); ulagDead.monsterId = "ulag";
            var done = Add<CompleteObjectiveAction>(q, "Objectif Ulag"); done.objectiveId = "kill_ulag";
            q.rules.Add(Rule("ulag_dead", new QuestCondition[] { ulagDead }, new QuestAction[] { done }));

            Save(q);
        }

        // ═════════════════════ QUÊTE 6 ═════════════════════
        static void BuildQuest6()
        {
            var q = NewQuest("Quest_06_DescendanceOrc");
            q.questId = "quest_06"; q.questNumber = 6;
            q.title = "La descendance du seigneur de guerre orc";
            q.description =
                "« Grak, l'ignoble progéniture d'Ulag, a juré de venger son père. Après plusieurs mois de recherches, " +
                "il réussit enfin à vous trouver et à vous capturer dans une embuscade. Vous voici donc maintenant " +
                "tous prisonniers de ses cachots pendant qu'il détermine le meilleur moment et la meilleure façon de " +
                "vous faire payer. Profitant du sommeil de votre geôlier, vous parvenez à crocheter la serrure de " +
                "votre cellule à l'aide d'un vieil os de rat. Vous devez donc maintenant retrouver votre équipement " +
                "et vous évader par l'escalier. »";
            q.mapJson = FindText(MapQuest6);
            q.stairsPosition = StairsQ6;

            // Départ dans la cellule (et non sur l'escalier), évasion individuelle par l'escalier
            q.startMode = HeroStartMode.InRoom; q.startRoomId = Q6_RoomCell;
            q.exitMode = ExitMode.EachHeroEscapesViaStairs;
            q.noTreasureRooms.Add(Q6_RoomCell);
            q.startEquipmentLocked = true; q.unarmedAttackDice = 1; q.unarmedDefenseDice = 2;
            q.wanderingMonsterId = "abomination";

            q.introTitle = "Quête 6 - Notes";
            q.introNote =
                "Les héros commencent la quête dans la pièce marquée « Cellule ». Les héros ne peuvent pas chercher " +
                "des trésors dans la cellule. Tout l'équipement (armes, armures, potions, etc.) des héros a été " +
                "confisqué. Ils ne peuvent donc pas l'utiliser ni même lancer des sorts avant de l'avoir récupéré. " +
                "Un héros sans arme ni armure peut lancer 1 dé en attaque et 2 dés en défense.";

            // Grak : orc coriace armé d'un bâton, porte la Cape de magicien, lance 1 sort de la Terreur par tour
            var grak = new NamedMonsterDef
            {
                instanceId = "grak", displayName = "Grak", baseMonsterId = "orc", tile = GrakTile,
                movement = 8, attackDice = 4, defenseDice = 4, bodyPoints = 3, mindPoints = 3,
                castsSpellInsteadOfAttacking = true
            };
            foreach (var s in new[] { SpellFear, SpellSleep, SpellTornado }) { var so = Find(s); if (so != null) grak.spells.Add(so); }
            q.namedMonsters.Add(grak);

            // Note A : le buffet contient l'équipement (la fouille ne tire PAS de carte)
            var searchBuffet = Add<TreasureSearchedCondition>(q, "Fouille buffet"); searchBuffet.roomId = Q6_RoomBuffet;
            var noteA = Add<ShowNoteAction>(q, "Note A"); noteA.noteLetter = "A";
            noteA.text = "L'équipement des héros se trouve dans le buffet situé dans cette pièce. Quand un héros cherche " +
                         "des trésors, il trouve l'équipement. Chaque héros doit ensuite entrer dans la pièce pour " +
                         "récupérer son équipement. Une fois entrés, les héros retrouvent leurs pouvoirs magiques et " +
                         "peuvent à nouveau lancer des sorts.";
            var found = Add<SetFlagAction>(q, "Flag équipement trouvé"); found.flag = "equipment_found";
            var restoreSearcher = Add<RestoreEquipmentAction>(q, "Restaure (celui qui fouille)");
            var consume = Add<ConsumeTreasureSearchAction>(q, "Fouille consommée");
            q.rules.Add(Rule("equipment_found", new QuestCondition[] { searchBuffet },
                new QuestAction[] { noteA, found, restoreSearcher, consume }));

            // Chaque héros qui entre ensuite dans la pièce récupère son équipement
            var enter = Add<RoomEnteredCondition>(q, "Entre dans la pièce du buffet"); enter.roomId = Q6_RoomBuffet;
            var flagOn = Add<FlagCondition>(q, "Équipement trouvé ?"); flagOn.flag = "equipment_found";
            var restoreEnterer = Add<RestoreEquipmentAction>(q, "Restaure (héros qui entre)");
            q.rules.Add(Rule("recover_equipment", new QuestCondition[] { enter, flagOn },
                new QuestAction[] { restoreEnterer }, once: false, perActor: true));

            // Note B : escalier = liberté (affichée à la première évasion)
            var escaped = Add<HeroEscapedCondition>(q, "Un héros s'échappe");
            var noteB = Add<ShowNoteAction>(q, "Note B"); noteB.noteLetter = "B";
            noteB.text = "Ces escaliers mènent en dehors du donjon et à la liberté. Tout héros parvenant à atteindre " +
                         "cette case a réussi à s'échapper.";
            q.rules.Add(Rule("note_B", new QuestCondition[] { escaped }, new QuestAction[] { noteB }));

            // Si Grak est tué, sa Cape de magicien revient à l'Enchanteur (sinon au héros qui l'a tué)
            var grakDead = Add<MonsterKilledCondition>(q, "Grak mort"); grakDead.monsterId = "grak";
            var cape = Add<GrantLootAction>(q, "Cape de magicien -> Enchanteur");
            cape.recipient = RecipientType.HeroClass; cape.heroClassId = HeroWizard;
            cape.replacesTreasureDraw = false; AddCard(cape, CardWizardCloak);
            q.rules.Add(Rule("grak_cloak", new QuestCondition[] { grakDead }, new QuestAction[] { cape }));

            Save(q);
        }

        // ───────── Helpers ─────────
        static QuestData NewQuest(string fileName)
        {
            if (!AssetDatabase.IsValidFolder(OutputFolder)) AssetDatabase.CreateFolder("Assets", "Quests");
            string path = $"{OutputFolder}/{fileName}.asset";
            AssetDatabase.DeleteAsset(path);
            var q = ScriptableObject.CreateInstance<QuestData>();
            AssetDatabase.CreateAsset(q, path);
            return q;
        }

        static T Add<T>(QuestData q, string name) where T : ScriptableObject
        {
            var o = ScriptableObject.CreateInstance<T>();
            o.name = name;
            AssetDatabase.AddObjectToAsset(o, q);
            return o;
        }

        static QuestRule Rule(string id, QuestCondition[] conds, QuestAction[] acts, bool once = true, bool perActor = false)
            => new QuestRule
            {
                ruleId = id, once = once, oncePerActor = perActor,
                conditions = new List<QuestCondition>(conds), actions = new List<QuestAction>(acts)
            };

        static void AddCard(GrantLootAction a, string name) { var c = Find(name); if (c != null) a.cards.Add(c); }

        static void Save(QuestData q) { EditorUtility.SetDirty(q); AssetDatabase.SaveAssets(); }

        static ScriptableObject Find(string name)
        {
            foreach (var guid in AssetDatabase.FindAssets(name + " t:ScriptableObject"))
            {
                var o = AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (o != null && !(o is QuestData) && !(o is QuestCondition) && !(o is QuestAction)) return o;
            }
            Debug.LogWarning($"[QuestBuilder] SO introuvable : '{name}' (à assigner à la main dans l'inspecteur)");
            return null;
        }

        static TextAsset FindText(string name)
        {
            foreach (var guid in AssetDatabase.FindAssets(name + " t:TextAsset"))
            {
                var t = AssetDatabase.LoadAssetAtPath<TextAsset>(AssetDatabase.GUIDToAssetPath(guid));
                if (t != null) return t;
            }
            Debug.LogWarning($"[QuestBuilder] JSON de map introuvable : '{name}' (à assigner à la main)");
            return null;
        }
    }
}
#endif
