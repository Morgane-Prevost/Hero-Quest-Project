using HeroQuest.Board;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public enum HeroType
{
    Barbarian = 0,
    Elf = 1,
    Dwarf = 2,
    Wizard = 3
}

public class Hero : MonoBehaviour, IPotionTarget
{
    #region Hero Properties
    // Hero properties
    public string heroName { get; set; }
    HeroType heroType { get; set; }
    public int bodyPoints { get; set; }
    public int mindPoints { get; set; }
    public HeroData Data { get; private set; }

    public int currentBodyPoints { get; private set; }
    public int attackDices { get; set; }
    public int defenseDices { get; set; }

    public int movementDices { get; set; }

    public WeaponData weapon;
    public List<ArmorData> armors;
    public int gold { get; set; }

    GridPawn gridPawn;

    private List<CardSO> inventory = new List<CardSO>();
    public IReadOnlyList<CardSO> Inventory => inventory;
    private bool hasAToolBag = false;

    #endregion

    [SerializeField] private LayerMask obstacleMask;

    private float maxDistance = 50f;
    private float eyeHeight = .2f;
    private int closeRangeCells = 1;

    private int pendingAttackDiceBonus;
    private int pendingDefenseDiceBonus;
    private int pendingExtraAttacks;

    public void Initialize(HeroData heroData, string name, Vector2Int cell)
    {
        heroName = name;
        Data = heroData;
        heroType = heroData.type;
        bodyPoints = heroData.bodyPoints;
        currentBodyPoints = bodyPoints;
        mindPoints = heroData.mindPoints;
        attackDices = heroData.attackDices;
        defenseDices = heroData.defenseDices;
        movementDices = heroData.movementDices;

        // Needs to be modified to create and add the weapon and armor cards before assigning them to the hero. For now, we will just assign the references from the HeroData.
        weapon = heroData.weapon;
        armors = heroData.armor;

        gold = heroData.gold;


        gridPawn = GetComponent<GridPawn>();
        gridPawn.PlaceAt(cell);
    }

    private void Start()
    {
        obstacleMask = LayerMask.GetMask("Obstacle");
    }

    public void Move()
    {
        int finalMovement = 0;

        if (armors.Exists(armor => armor.movementPenalty != 0))
        {
            foreach (ArmorData armor in armors)
            {
                finalMovement -= armor.movementPenalty;
            }
        }

        // Roll the movement dice and display where the hero can move on the board
        for (int i = 0; i < movementDices; i++)
        {
            finalMovement += ActionManager.Instance.RollRedDice();
        }

        StartCoroutine(MoveRoutine(gridPawn, finalMovement));
    }
    private IEnumerator MoveRoutine(GridPawn hero, int movePoints)
    {
        Vector2Int? chosen = null;
        MovementSystem.Instance.BeginMove(hero, movePoints, c => chosen = c);

        yield return new WaitUntil(() => chosen.HasValue);

        gridPawn.PlaceAt(chosen.Value);
    }

    public void AttackAction()
    {
        StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        List<GameObject> targets = new List<GameObject>();

        foreach (Monster monster in MonsterManager.Instance.ActiveMonsters)
        {
            targets.Add(monster.gameObject);
        }

        List<GameObject> visibleTargets = GetVisibleTargets(targets);

        foreach (GameObject target in visibleTargets)
        {
            target.layer = LayerMask.NameToLayer("HighLight");
        }

        if (weapon.canVisionAttack)
        {
            if (!weapon.canCloseRangeAttack)
            {
                foreach (GameObject target in visibleTargets)
                {
                    if (IsMonsterCloseRanged(visibleTargets) && !IsTargetDiagonal(target))
                    {
                        target.layer = LayerMask.NameToLayer("Monster");
                        visibleTargets.Remove(target);
                    }
                }
                GameObject selectedTarget = null;
                yield return CellPicker.Instance.SelectTarget(visibleTargets, t => selectedTarget = t);
                Debug.Log("Selected target: " + (selectedTarget != null ? selectedTarget.name : "None"));
                if (selectedTarget == null)
                {
                    foreach (GameObject target in visibleTargets)
                    {
                        target.layer = LayerMask.NameToLayer("Monster");
                    }
                    yield break; // cancelled or no target
                }

                Attack(selectedTarget);
                foreach (GameObject target in visibleTargets)
                {
                    target.layer = LayerMask.NameToLayer("Monster");
                }
                yield break;
            }
            else if (weapon.canBeThrown)
            {
                GameObject selectedTarget = null;
                yield return CellPicker.Instance.SelectTarget(visibleTargets, t => selectedTarget = t);
                if (selectedTarget == null)
                {
                    foreach (GameObject target in visibleTargets)
                    {
                        target.layer = LayerMask.NameToLayer("Monster");
                    }
                    yield break; // cancelled or no target
                }
                if (IsMonsterCloseRanged(visibleTargets) && !IsTargetDiagonal(selectedTarget))
                {
                    Attack(selectedTarget);
                    foreach (GameObject target in visibleTargets)
                    {
                        target.layer = LayerMask.NameToLayer("Monster");
                    }
                    yield break;
                }
                else
                {
                    Attack(selectedTarget);
                    foreach (GameObject target in visibleTargets)
                    {
                        target.layer = LayerMask.NameToLayer("Monster");
                    }
                    weapon = null;
                    yield break;
                }
            }
        }
        else
        {
            if (!weapon.canDiagoAttack)
            {
                foreach (GameObject target in visibleTargets)
                {
                    if (IsMonsterCloseRanged(visibleTargets) && IsTargetDiagonal(target))
                    {
                        visibleTargets.Remove(target);
                    }
                }
                GameObject selectedTarget = null;
                yield return CellPicker.Instance.SelectTarget(visibleTargets, t => selectedTarget = t);
                if (selectedTarget == null)
                {
                    foreach (GameObject target in visibleTargets)
                    {
                        target.layer = LayerMask.NameToLayer("Monster");
                    }
                    yield break; // cancelled or no target
                }

                Attack(selectedTarget);
                foreach (GameObject target in visibleTargets)
                {
                    target.layer = LayerMask.NameToLayer("Monster");
                }
                yield break;
            }
            else
            {
                GameObject selectedTarget = null;
                yield return CellPicker.Instance.SelectTarget(visibleTargets, t => selectedTarget = t);
                if (selectedTarget == null)
                {
                    foreach (GameObject target in visibleTargets)
                    {
                        target.layer = LayerMask.NameToLayer("Monster");
                    }
                    yield break; // cancelled or no target
                }

                Attack(selectedTarget);
                foreach (GameObject target in visibleTargets)
                {
                    target.layer = LayerMask.NameToLayer("Monster");
                }
                yield break;
            }
        }
    }
    
    public void SpellAction()
    {
        // Look at every enemy in the field of view that can be attacked

        // Wait for the player to select an enemy to attack

        // Roll the attack dices and calculate the damage

        // Apply the damage to the enemy
    }

    public void LookForTreasureAction()
    {
        if (canLookForTreasure())
        {
            // Look for treasure in the room
            // If treasure is found draw a treasure card
            // If it draws a trap card apply it 
            // Else give the treasure to the hero
        }
        else
        {
            // Display a message that the hero cannot look for treasure
        }
    }

    public void LookForSecretDoorAction()
    {
        // Check if there is monster in the FOV
        // Check if there is a secret door in the room, reveal it else display a message that there is no secret door
    }

    public void LookForTrapAction()
    {
        // Check if there is a monster in the FOV
        // Check if there is a trap in the room, mark it else display a message that there is no trap
    }

    public void DisarmTrapAction()
    {
        if (heroType == HeroType.Dwarf)
        {
            if (ActionManager.Instance.RollWhiteDice() == WhiteDiceFaces.BlackShield)
            {
                //Activates the trap and takes damage
            }
            else
            {
                //Disarms the trap
            }
        }
        else if (hasAToolBag)
        {
            if (ActionManager.Instance.RollWhiteDice() == WhiteDiceFaces.Skull)
            {
                //Activates the trap and takes damage
            }
            else
            {
                //Disarms the trap
            }
        }
        else
        {
            // Display a message that the hero cannot disarm the trap
        }
    }

    private bool canLookForTreasure()
    {
        //Check the list if the room is in the list && if there is monster in the room
        //return false else return true
        return true;
    }

    private List<GameObject> GetVisibleTargets(List<GameObject> targets)
    {
        var visible = new List<GameObject>();
        Vector3 origin = transform.position + Vector3.up * eyeHeight;

        foreach (var target in targets)
        {
            if (target == null || target == gameObject) continue;

            Vector3 targetPoint = target.transform.position + Vector3.up * eyeHeight;
            Vector3 dir = targetPoint - origin;
            float dist = dir.magnitude;

            if (dist > maxDistance) continue;

            bool blocked = Physics.Raycast(origin, dir.normalized, dist, obstacleMask,
                                           QueryTriggerInteraction.Ignore);

            Color c = blocked ? Color.red : Color.green;
            Debug.DrawLine(origin, targetPoint, c, 1f);

            if (!blocked)
                visible.Add(target);
        }

        return visible;
    }

    private void Attack(GameObject target)
    {
        int damage = ActionManager.Instance.Fight(this, target.GetComponent<Monster>(), true);
        if (damage > 0)
        {
            target.GetComponent<Monster>().TakeDamage(damage);
        }
        else
        {
            Debug.Log("No damage inflicted to the monster.");
        }
    }
    private bool IsMonsterCloseRanged(List<GameObject> visibleTargets)
    {
        if (gridPawn == null || visibleTargets == null) return false;

        Vector2Int myCell = gridPawn.Cell;

        foreach (var target in visibleTargets)
        {
            if (target == null) continue;

            var pawn = target.GetComponent<GridPawn>();
            if (pawn == null) continue;

            if (IsInCloseRange(myCell, pawn.Cell))
                return true;
        }

        return false;
    }
    private bool IsInCloseRange(Vector2Int from, Vector2Int to)
    {
        Vector2Int delta = to - from;
        int dx = Mathf.Abs(delta.x);
        int dy = Mathf.Abs(delta.y);

        int distance = weapon.canDiagoAttack
            ? Mathf.Max(dx, dy)   // Chebyshev
            : dx + dy;            // Manhattan

        return distance <= closeRangeCells;
    }
    private bool IsTargetDiagonal(GameObject target)
    {
        var pawn = target != null ? target.GetComponent<GridPawn>() : null;
        if (gridPawn == null || pawn == null) return false;

        Vector2Int delta = pawn.Cell - gridPawn.Cell;

        return delta.x != 0 && delta.y != 0;
    }

    #region Inventory Management
    // Inventory management methods

    public void equipWeapon(WeaponData newWeapon)
    {
        if (canEquipWeapon(newWeapon))
        {
            RemoveCardFromInventory(newWeapon);
            weapon = newWeapon;
            attackDices = newWeapon.attackDice;
        }
        else
        {
            Debug.LogWarning("Cannot equip weapon: " + newWeapon.displayName);
        }
    }
    public void unequipWeapon()
    {
        AddCardToInventory(weapon);
        weapon = null;
        attackDices = 0;
    }

    public void EquipArmor(ArmorData newArmor)
    {
        if (canEquipArmorPiece(newArmor))
        {
            RemoveCardFromInventory(newArmor);
            armors.Add(newArmor);
            defenseDices += newArmor.defenseDice;
        }
        else
        {
            Debug.LogWarning("Cannot equip armor: " + newArmor.displayName);
        }
    }
    public void unEquipArmor(ArmorData armor)
    {
        if (armors.Contains(armor))
        {
            AddCardToInventory(armor);
            armors.Remove(armor);
            defenseDices -= armor.defenseDice;
        }
    }
    
    public bool canEquipWeapon(WeaponData weaponToCheck)
    {
        if (weaponToCheck == null || weapon != null) return false;
        if (weaponToCheck is IArtifact artifactWeapon && !artifactWeapon.Info.CanBeUsedBy(heroType)) return false;
        if (!weaponToCheck.useWithWizard && heroType == HeroType.Wizard) return false;
        if (!weaponToCheck.useWithShield && armors.Exists(armor => armor.armorType == ArmorType.Shield)) return false;
        return true;
    }
    public bool canEquipArmorPiece(ArmorData armorToCheck)
    {
        if (armorToCheck != null && armors.Contains(armorToCheck)) return false;
        if (armorToCheck is IArtifact artifactArmor && !artifactArmor.Info.CanBeUsedBy(heroType)) return false;
        if (!armorToCheck.useWithWizard && heroType == HeroType.Wizard) return false;
        if (!armorToCheck.useWithStaff && weapon.weaponType == WeaponType.Staff) return false;
        if (!armorToCheck.useWithAxe && weapon.weaponType == WeaponType.Axe) return false;

        foreach (ArmorData armor in armors)
        {
            if (!armorToCheck.useWithOtherArmor && !armor.useWithOtherArmor) return false;
        }
        return true;
    }

    public void AddCardToInventory(CardSO card)
    {
        if (card != null) inventory.Add(card);
        if (card is PassiveData passiveData)
        {
            if (passiveData.passiveObjectName == PassiveObjectName.Toolbag) hasAToolBag = true;
        }
    }
    public void RemoveCardFromInventory(CardSO card)
    {
        if (card != null && inventory.Contains(card)) inventory.Remove(card);
    }

    public void GiveCardToHero(CardSO card, Hero otherHero)
    {
        if (card != null && inventory.Contains(card))
        {
            RemoveCardFromInventory(card);
            otherHero.AddCardToInventory(card);
        }
    }

    public bool UsePotion(PotionCardSO potion, IRandomPicker picker)
    {
        if (potion == null || !inventory.Contains(potion)) return false;

        potion.Use(this, picker);
        RemoveCardFromInventory(potion);
        return true;
    }

    // Inventory management methods
    #endregion

    #region IPotionTarget
    public void Heal(int amount)
    {
        // bodyPoints holds the starting total
        currentBodyPoints = Mathf.Min(currentBodyPoints + amount, MaxBodyPoints);
    }
    public void AddAttackDiceNextAttack(int extraDice) => pendingAttackDiceBonus += extraDice;
    public void AddDefenseDiceNextDefense(int extraDice) => pendingDefenseDiceBonus += extraDice;
    public void AttackTwiceNextAttack() => pendingExtraAttacks++;

    // Called by the combat code when the roll really happens: returns the bonus and resets it (single use).
    public int ConsumeAttackDiceBonus() => TakeAndReset(ref pendingAttackDiceBonus);
    public int ConsumeDefenseDiceBonus() => TakeAndReset(ref pendingDefenseDiceBonus);
    public int ConsumeExtraAttacks() => TakeAndReset(ref pendingExtraAttacks);

    private static int TakeAndReset(ref int value)
    {
        int taken = value;
        value = 0;
        return taken;
    }

    #endregion

    #region Artifact bonuses
    // Computed on demand from the inventory: nothing to resync if an artifact changes hands.
    public int MaxBodyPoints => bodyPoints + SumStatBonus(a => a.bodyPointsBonus);
    public int MaxMindPoints => mindPoints + SumStatBonus(a => a.mindPointsBonus);
    // Combat must use this instead of defenseDices (then add the potion bonus)
    public int EffectiveDefenseDice => defenseDices + SumStatBonus(a => a.defenseDiceBonus);

    private int SumStatBonus(System.Func<StatBonusArtifactSO, int> pick)
    {
        int total = 0;
        foreach (CardSO card in inventory)
            if (card is StatBonusArtifactSO bonus && bonus.Info.CanBeUsedBy(heroType))
                total += pick(bonus);
        return total;
    }
    #endregion
}
