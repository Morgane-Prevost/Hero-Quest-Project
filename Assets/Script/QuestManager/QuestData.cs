using System;
using System.Collections.Generic;
using UnityEngine;

namespace HeroQuest.Quests
{
    public enum HeroStartMode { OnStairs, InRoom }
    public enum ExitMode { AllSurvivorsReturnToStairs, EachHeroEscapesViaStairs }
    public enum RecipientType { Actor, AllHeroes, HeroClass }

    [CreateAssetMenu(menuName = "HeroQuest/Quest", fileName = "Quest_")]
    public class QuestData : ScriptableObject
    {
        [Header("Menu de sélection")]
        public string questId;
        public int questNumber;
        public string title;
        [TextArea(5, 15)] public string description;   // texte d'histoire du parchemin, affiché dans le menu
        public Sprite thumbnail;

        [Header("Carte")]
        public TextAsset mapJson;
        public Vector2Int stairsPosition;              // escalier : posé au début, sortie à la fin

        [Header("Départ & sortie")]
        public HeroStartMode startMode = HeroStartMode.OnStairs;   // quête 6 : InRoom ("Cellule")
        public string startRoomId;
        public ExitMode exitMode = ExitMode.AllSurvivorsReturnToStairs; // quête 6 : EachHeroEscapesViaStairs

        [Header("Règles spéciales")]
        public List<string> noTreasureRooms = new();   // pièces où on ne peut pas fouiller (cellule)
        public bool startEquipmentLocked;              // équipement confisqué au départ
        public int unarmedAttackDice = 1;
        public int unarmedDefenseDice = 2;
        public string wanderingMonsterId;              // monstre errant de la quête

        [Header("Narration")]
        public string introTitle = "Quête";
        [TextArea(3, 10)] public string introNote;     // section "NOTES" générale, lue au lancement
        [TextArea(3, 10)] public string victoryNote;
        [TextArea(3, 10)] public string defeatNote;

        [Header("Monstres nommés")]
        public List<NamedMonsterDef> namedMonsters = new();

        [Header("Contenu")]
        public List<QuestObjective> objectives = new();
        public List<QuestRule> rules = new();

        [Header("Récompense")]
        public int sharedGoldReward;                   // partagé entre les héros (ex : 180 po)
    }

    [Serializable]
    public class NamedMonsterDef
    {
        public string instanceId;        // id utilisé dans les événements MonsterKilled ("ulag", "grak")
        public Vector2Int tile;          // case de départ dans la map (pour retrouver la figurine)
        public string displayName;
        public string baseMonsterId;     // figurine/type de base ("orc")
        public int movement, attackDice, defenseDice, bodyPoints, mindPoints;
        public List<ScriptableObject> spells = new();   // cartes Sort de la Terreur (SO)
        public bool castsSpellInsteadOfAttacking;       // 1 sort par tour au lieu d'attaquer
    }

    [Serializable]
    public class QuestObjective
    {
        public string id;
        public string description;
        public bool optional;
    }

    [Serializable]
    public class QuestRule
    {
        public string ruleId;
        public bool once = true;            // se déclenche une seule fois ("le premier héros qui...")
        public bool oncePerActor;           // une fois PAR héros ("chaque héros doit...")
        public List<QuestCondition> conditions = new();
        public List<QuestAction> actions = new();
    }

    public abstract class QuestCondition : ScriptableObject
    {
        public abstract bool Evaluate(QuestContext ctx, QuestEvent e);
    }

    public abstract class QuestAction : ScriptableObject
    {
        public abstract void Execute(QuestContext ctx);
    }
}
