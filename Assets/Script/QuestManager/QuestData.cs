using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace HeroQuest.Quests
{
    public enum HeroStartMode { OnStairs, InRoom }
    public enum ExitMode { AllSurvivorsReturnToStairs, EachHeroEscapesViaStairs }
    public enum RecipientType { Actor, AllHeroes, SpecificHeroType }

    [CreateAssetMenu(menuName = "HeroQuest/Quest", fileName = "Quest_")]
    public class QuestData : ScriptableObject
    {
        [Header("Menu de sélection")]
        public string questId;
        public int questNumber;
        public string title;
        [TextArea(5, 15)] public string description;   // shown in the start menu when the quest is selected
        public Sprite thumbnail;

        [Header("Niveau (JSON LevelData : murs, portes, escalier, meubles, monstres, pièges)")]
        [FormerlySerializedAs("mapJson")] public TextAsset levelJson;

        [Header("Départ & sortie (l'escalier vient du JSON du niveau)")]
        public HeroStartMode startMode = HeroStartMode.OnStairs;   // quests 6, 9: InRoom
        public int startZone;                                      // room number when InRoom
        public ExitMode exitMode = ExitMode.AllSurvivorsReturnToStairs;   // quest 6: EachHeroEscapesViaStairs

        [Header("Règles spéciales")]
        public List<int> noTreasureZones = new List<int>();        // rooms where nobody can search (cell)
        public bool startEquipmentLocked;                          // quest 6
        public int unarmedAttackDice = 1;
        public int unarmedDefenseDice = 2;
        public string wanderingMonsterType;                        // "Orc", "Abomination"... (MonsterData.monsterName)
        [TextArea(2, 5)] public string wanderingMonsterNote;       // used when there is no real wandering monster (quest 10)
        public List<MonsterModifier> monsterModifiers = new List<MonsterModifier>();
        public List<KillBounty> killBounties = new List<KillBounty>();

        [Header("Narration")]
        public string introTitle = "Quête";
        [TextArea(3, 10)] public string introNote;
        [TextArea(3, 10)] public string victoryNote;
        [TextArea(3, 10)] public string defeatNote;

        [Header("Personnages de la quête")]
        public List<NamedMonsterDef> namedMonsters = new List<NamedMonsterDef>();
        public List<AllyDef> allies = new List<AllyDef>();

        [Header("Contenu")]
        public List<QuestObjective> objectives = new List<QuestObjective>();
        public List<QuestRule> rules = new List<QuestRule>();

        [Header("Récompenses")]
        public int sharedGoldReward;                  // split between the heroes
        public int goldRewardPerHero;                 // each hero
        public string rewardRequiresObjectiveId;      // empty = always paid on victory (quest 2: "rescue_ragnar")
    }

    /// <summary>Quest-wide change on every monster of a type (quest 7: warriors defend with +1 die).</summary>
    [Serializable]
    public class MonsterModifier
    {
        public string monsterType;
        public int attackDiceBonus, defenseDiceBonus, bodyBonus;
    }

    /// <summary>Gold paid to the hero who kills a monster of this type (quest 11).</summary>
    [Serializable]
    public class KillBounty { public string monsterType; public int gold; }

    /// <summary>
    /// Special monster of a quest (Ulag, Grak, Balur...). elementId = id of the monster in the LEVEL JSON.
    /// Values left at -1 are not changed (the MonsterData of its type is used).
    /// </summary>
    [Serializable]
    public class NamedMonsterDef
    {
        public string elementId;
        public string displayName;
        public int movement = -1, attackDice = -1, defenseDice = -1, bodyPoints = -1, mindPoints = -1;

        public bool dormant;                          // statue: Zargon's AI skips it until ActivateMonster
        public bool invulnerableUntilActed;           // cannot be hurt before it has moved or attacked
        public bool invulnerable;
        public string vulnerableOnlyToArtifactId;     // Sorcerer Lord: "mystic_sword"

        public bool immuneToAllSpells;
        public List<string> spellImmunities = new List<string>();   // "fire"
        public List<string> spellIds = new List<string>();          // read by the spell system (coming)
        public bool castsSpellInsteadOfAttacking;
        public bool spellsOncePerQuest;
    }

    /// <summary>NPC escorted by the heroes (Sir Ragnar). Built from a HeroData so monsters can target it like a hero.</summary>
    [Serializable]
    public class AllyDef
    {
        public string id;
        public string displayName;
        public HeroData template;                     // bodyPoints 2, defenseDices 2, attackDices 0, movementDices 1
        public Vector2Int spawnCell;
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
        public bool once = true;            // "the first hero who..."
        public bool oncePerActor;           // "each hero must..."
        public List<QuestCondition> conditions = new List<QuestCondition>();
        public List<QuestAction> actions = new List<QuestAction>();
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
