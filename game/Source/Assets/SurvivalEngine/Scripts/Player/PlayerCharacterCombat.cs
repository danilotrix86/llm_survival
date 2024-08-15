using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SurvivalEngine
{
    public enum PlayerAttackBehavior
    {
        AutoAttack = 0, //When clicking on target, will start attacking constantly
        ClickToHit = 10, //When clicking on object, will only perform one attack hit
        NoAttack = 20, //Character cant attack
    }

    /// <summary>
    /// Class that manages the player character attacks, hp and death
    /// </summary>

    [RequireComponent(typeof(PlayerCharacter))]
    public class PlayerCharacterCombat : MonoBehaviour
    {
        [Header("Combat")]
        public PlayerAttackBehavior attack_type;
        public int hand_damage = 5;
        public int base_armor = 0;
        public float attack_range = 1.2f; //How far can you attack (melee)
        public float attack_cooldown = 1f; //Seconds of waiting in between each attack
        public float attack_windup = 0.7f; //Timing (in secs) between the start of the attack and the hit
        public float attack_windout = 0.4f; //Timing (in secs) between the hit and the end of the attack
        public float attack_energy = 1f; //Energy cost to attack

        [Header("FX")]
        public GameObject hit_fx;
        public GameObject death_fx;
        public AudioClip hit_sound;
        public AudioClip death_sound;

        public UnityAction<Destructible, bool> onAttack;
        public UnityAction<Destructible> onAttackHit;
        public UnityAction onDamaged;
        public UnityAction onDeath;

        private PlayerCharacter character;
        private PlayerCharacterAttribute character_attr;

        private Coroutine attack_routine = null;
        private float attack_timer = 0f;
        private bool is_dead = false;
        private bool is_attacking = false;

        private void Awake()
        {
            character = GetComponent<PlayerCharacter>();
            character_attr = GetComponent<PlayerCharacterAttribute>();
        }

        void Start()
        {

        }

        void Update()
        {
            if (TheGame.Get().IsPaused())
                return;

            if (IsDead())
                return;

            //Attack when target is in range
            if(!character.IsBusy())
                attack_timer += Time.deltaTime;

            Destructible auto_move_attack = character.GetAutoAttackTarget();
            if (auto_move_attack != null && !character.IsBusy() && IsAttackTargetInRange(auto_move_attack))
            {
                character.FaceTorward(auto_move_attack.transform.position);
                character.PauseAutoMove(); //Reached target, dont keep moving

                if (attack_timer > GetAttackCooldown())
                {
                    Attack(auto_move_attack);
                }
            }
        }

        public void TakeDamage(int damage)
        {
            if (is_dead)
                return;

            if (character.Attributes.GetBonusEffectTotal(BonusType.Invulnerable) > 0.5f)
                return;

            int dam = damage - GetArmor();
            dam = Mathf.Max(dam, 1);

            int invuln = Mathf.RoundToInt(dam * character.Attributes.GetBonusEffectTotal(BonusType.Invulnerable));
            dam = dam - invuln;

            if (dam <= 0)
                return;

            character_attr.AddAttribute(AttributeType.Health, -dam);

            //Durability
            character.Inventory.UpdateAllEquippedItemsDurability(false, -1f);

            character.StopSleep();

            TheCamera.Get().Shake();
            TheAudio.Get().PlaySFX("player", hit_sound);
            if (hit_fx != null)
                Instantiate(hit_fx, transform.position, Quaternion.identity);

            if (onDamaged != null)
                onDamaged.Invoke();
        }

        public void Kill()
        {
            if (is_dead)
                return;

            character.StopMove();
            is_dead = true;

            TheAudio.Get().PlaySFX("player", death_sound);
            if (death_fx != null)
                Instantiate(death_fx, transform.position, Quaternion.identity);

            if (onDeath != null)
                onDeath.Invoke();
        }

        public void Attack(Destructible target)
        {
            DoAttack(target);
        }

        public void Attack()
        {
            DoAttackNoTarget();
        }

        //Perform one attack
        private void DoAttack(Destructible target)
        {
            if (!character.IsBusy() && attack_timer > GetAttackCooldown())
            {
                attack_timer = -10f;
                attack_routine = StartCoroutine(AttackRun(target));
            }
        }

        private void DoAttackNoTarget()
        {
            if (!character.IsBusy() && attack_timer > GetAttackCooldown())
            {
                attack_timer = -10f;
                attack_routine = StartCoroutine(AttackRunNoTarget());
            }
        }

        //Melee or ranged targeting one target
        private IEnumerator AttackRun(Destructible target)
        {
            character.SetBusy(true);
            is_attacking = true;

            bool is_ranged = target != null && CanWeaponAttackRanged(target);

            //Start animation
            if (onAttack != null)
                onAttack.Invoke(target, is_ranged);

            //Face target
            character.FaceTorward(target.transform.position);

            //Wait for windup
            float windup = GetAttackWindup();
            yield return new WaitForSeconds(windup);

            //Durability
            character.Inventory.UpdateAllEquippedItemsDurability(true, -1f);

            int nb_strikes = GetAttackStrikes(target);
            float strike_interval = GetAttackStikesInterval(target);

            while (nb_strikes > 0)
            {
                DoAttackStrike(target, is_ranged);
                yield return new WaitForSeconds(strike_interval);
                nb_strikes--;
            }

            //Reset timer
            attack_timer = 0f;

            //Wait for the end of the attack before character can move again
            float windout = GetAttackWindout();
            yield return new WaitForSeconds(windout);

            character.SetBusy(false);
            is_attacking = false;

            if(attack_type == PlayerAttackBehavior.ClickToHit)
                character.StopAutoMove();
        }

        //Ranged attack without a target
        private IEnumerator AttackRunNoTarget()
        {
            character.SetBusy(true);
            is_attacking = true;

            //Rotate toward 
            bool freerotate = TheCamera.Get().IsFreeRotation();
            if (freerotate)
                character.FaceFront();

            //Start animation
            if (onAttack != null)
                onAttack.Invoke(null, true);

            //Wait for windup
            float windup = GetAttackWindup();
            yield return new WaitForSeconds(windup);

            //Durability
            character.Inventory.UpdateAllEquippedItemsDurability(true, -1f);

            int nb_strikes = GetAttackStrikes();
            float strike_interval = GetAttackStikesInterval();

            while (nb_strikes > 0)
            {
                DoAttackStrikeNoTarget();
                yield return new WaitForSeconds(strike_interval);
                nb_strikes--;
            }

            //Reset timer
            attack_timer = 0f;

            //Wait for the end of the attack before character can move again
            float windout = GetAttackWindout();
            yield return new WaitForSeconds(windout);

            character.SetBusy(false);
            is_attacking = false;
        }

        private void DoAttackStrike(Destructible target, bool is_ranged)
        {
            //Ranged attack
            ItemData equipped = character.EquipData.GetEquippedWeaponData();
            if (target != null && is_ranged && equipped != null)
            {
                InventoryItemData projectile_inv = character.Inventory.GetFirstItemInGroup(equipped.projectile_group);
                ItemData projectile = ItemData.Get(projectile_inv?.item_id);
                if (projectile != null && CanWeaponAttackRanged(target))
                {
                    Vector3 pos = GetProjectileSpawnPos();
                    Vector3 dir = target.GetCenter() - pos;
                    GameObject proj = Instantiate(projectile.projectile_prefab, pos, Quaternion.LookRotation(dir.normalized, Vector3.up));
                    Projectile project = proj.GetComponent<Projectile>();
                    project.player_shooter = character;
                    project.dir = dir.normalized;
                    project.damage = equipped.damage + projectile.damage;
                    character.Inventory.UseItem(projectile, 1);
                }
            }

            //Melee attack
            else if (IsAttackTargetInRange(target))
            {
                target.TakeDamage(character, GetAttackDamage(target));

                if (onAttackHit != null)
                    onAttackHit.Invoke(target);
            }
        }

        //Strike without target
        private void DoAttackStrikeNoTarget()
        {
            //Ranged attack
            ItemData equipped = character.EquipData.GetEquippedWeaponData();
            if (equipped != null && equipped.IsRangedWeapon())
            {
                InventoryItemData projectile_inv = character.Inventory.GetFirstItemInGroup(equipped.projectile_group);
                ItemData projectile = ItemData.Get(projectile_inv?.item_id);
                if (projectile != null)
                {
                    character.Inventory.UseItem(projectile, 1);
                    Vector3 pos = GetProjectileSpawnPos();
                    Vector3 dir = transform.forward;
                    bool freerotate = TheCamera.Get().IsFreeRotation();
                    if (freerotate)
                        dir = TheCamera.Get().GetFacingDir();

                    GameObject proj = Instantiate(projectile.projectile_prefab, pos, Quaternion.LookRotation(dir.normalized, Vector3.up));
                    Projectile project = proj.GetComponent<Projectile>();
                    project.player_shooter = character;
                    project.dir = dir.normalized;
                    project.damage = equipped.damage + projectile.damage;

                    if (freerotate)
                        project.SetInitialCurve(GetAimDir());
                }
            }
            else
            {
                Destructible destruct = Destructible.GetNearestAutoAttack(character, character.GetInteractCenter(), 10f);
                if (destruct != null && IsAttackTargetInRange(destruct))
                {
                    destruct.TakeDamage(character, GetAttackDamage(destruct));

                    if (onAttackHit != null)
                        onAttackHit.Invoke(destruct);
                }
            }
        }

        //Cancel current attack
        public void CancelAttack()
        {
            if (is_attacking)
            {
                is_attacking = false;
                attack_timer = 0f;
                character.SetBusy(false);
                character.StopAutoMove();
                if (attack_routine != null)
                    StopCoroutine(attack_routine);
            }
        }

        //Is the player currently attacking?
        public bool IsAttacking()
        {
            return is_attacking;
        }

        public bool CanAttack()
        {
            return attack_type != PlayerAttackBehavior.NoAttack;
        }

        //Does Attack has priority on actions?
        public bool CanAutoAttack(Destructible target)
        {
            bool has_required_item = target != null && target.required_item != null && character.EquipData.HasItemInGroup(target.required_item);
            return CanAttack(target) && (has_required_item || target.target_team == AttackTeam.Enemy || !target.Selectable.CanAutoInteract());
        }

        //Can it be attacked at all?
        public bool CanAttack(Destructible target)
        {
            return attack_type != PlayerAttackBehavior.NoAttack && target != null && target.CanBeAttacked() 
                && (target.required_item != null || target.target_team != AttackTeam.Ally) //Cant attack allied unless has required item
                && HasRequiredTool(target); //Cannot attack unless has equipped item
        }

		public bool HasRequiredTool(Destructible target)
		{
			return target.required_item == null || character.EquipData.HasItemInGroup(target.required_item);
		}

        public int GetAttackDamage(Destructible target)
        {
            int damage = hand_damage;

            ItemData equipped = character.EquipData.GetEquippedWeaponData();
            if (equipped != null && CanWeaponHitTarget(target))
                damage = equipped.damage;

            float mult = 1f + character.Attributes.GetBonusEffectTotal(BonusType.AttackBoost, target.Selectable.groups);
            damage = Mathf.RoundToInt(damage * mult);

            return damage;
        }

        public float GetAttackRange()
        {
            ItemData equipped = character.EquipData.GetEquippedWeaponData();
            if (equipped != null && CanWeaponAttack())
                return Mathf.Max(equipped.range, attack_range);
            return attack_range;
        }

        public float GetAttackRange(Destructible target)
        {
            ItemData equipped = character.EquipData.GetEquippedWeaponData();
            if (equipped != null && CanWeaponHitTarget(target))
                return Mathf.Max(equipped.range, attack_range);
            return attack_range;
        }

        public int GetAttackStrikes(Destructible target)
        {
            ItemData equipped = character.EquipData.GetEquippedWeaponData();
            if (equipped != null && CanWeaponHitTarget(target))
                return Mathf.Max(equipped.strike_per_attack, 1);
            return 1;
        }

        public float GetAttackStikesInterval(Destructible target)
        {
            ItemData equipped = character.EquipData.GetEquippedWeaponData();
            if (equipped != null && CanWeaponHitTarget(target))
                return Mathf.Max(equipped.strike_interval, 0.01f);
            return 0.01f;
        }

        public float GetAttackCooldown()
        {
            ItemData equipped = character.EquipData.GetEquippedWeaponData();
            if (equipped != null)
                return equipped.attack_cooldown / character.Attributes.GetAttackMult();
            return attack_cooldown / character.Attributes.GetAttackMult();
        }

        public int GetAttackStrikes()
        {
            ItemData equipped = character.EquipData.GetEquippedWeaponData();
            if (equipped != null)
                return Mathf.Max(equipped.strike_per_attack, 1);
            return 1;
        }

        public float GetAttackStikesInterval()
        {
            ItemData equipped = character.EquipData.GetEquippedWeaponData();
            if (equipped != null)
                return Mathf.Max(equipped.strike_interval, 0.01f);
            return 0.01f;
        }

        public float GetAttackWindup()
        {
            EquipItem item_equip = character.Inventory.GetEquippedWeaponMesh();
            if (item_equip != null && item_equip.override_timing)
                return item_equip.attack_windup / GetAttackAnimSpeed();
            return attack_windup / GetAttackAnimSpeed();
        }

        public float GetAttackWindout()
        {
            EquipItem item_equip = character.Inventory.GetEquippedWeaponMesh();
            if (item_equip != null && item_equip.override_timing)
                return item_equip.attack_windout / GetAttackAnimSpeed();
            return attack_windout / GetAttackAnimSpeed();
        }

        public float GetAttackAnimSpeed()
        {
            ItemData equipped = character.EquipData.GetEquippedWeaponData();
            if (equipped != null && equipped.attack_speed > 0.01f)
                return equipped.attack_speed * character.Attributes.GetAttackMult();
            return 1f * character.Attributes.GetAttackMult();
        }

        public Vector3 GetProjectileSpawnPos()
        {
            ItemData weapon = character.EquipData.GetEquippedWeaponData();
            EquipAttach attach = character.Inventory.GetEquipAttachment(weapon.equip_slot, weapon.equip_side);
            if (attach != null)
                return attach.transform.position;
            return transform.position + Vector3.up;
        }

        //Get camera direction for shooting projectiles, will only work if IsFreeRotation
        public Vector3 GetAimDir(float distance = 10f)
        {
            Vector3 cam_pos = TheCamera.Get().transform.position; 
            Vector3 cam_dir = TheCamera.Get().GetFacingDir();
            Vector3 far = cam_pos + cam_dir * distance;
            Vector3 aim = far - character.GetColliderCenter();
            return aim.normalized;
        }

        //Make sure the current equipped weapon can hit target, and has enough bullets
        public bool CanWeaponHitTarget(Destructible target)
        {
            ItemData equipped = character.EquipData.GetEquippedWeaponData();
            bool valid_ranged = equipped != null && equipped.IsRangedWeapon() && CanWeaponAttackRanged(target);
            bool valid_melee = equipped != null && equipped.IsMeleeWeapon();
            return valid_melee || valid_ranged;
        }

        //Check if target is valid for ranged attack, and if enough bullets
        public bool CanWeaponAttackRanged(Destructible destruct)
        {
            if (destruct == null)
                return false;

            return destruct.CanAttackRanged() && HasRangedProjectile();
        }

        public bool CanWeaponAttack()
        {
            return !HasRangedWeapon() || HasRangedProjectile();
        }

        public bool HasRangedWeapon()
        {
            ItemData equipped = character.EquipData.GetEquippedWeaponData();
            return (equipped != null && equipped.IsRangedWeapon());
        }

        public bool HasRangedProjectile()
        {
            ItemData equipped = character.EquipData.GetEquippedWeaponData();
            if (equipped != null && equipped.IsRangedWeapon())
            {
                InventoryItemData invdata = character.Inventory.GetFirstItemInGroup(equipped.projectile_group);
                ItemData projectile = ItemData.Get(invdata?.item_id);
                return projectile != null && character.Inventory.HasItem(projectile);
            }
            return false;
        }

        public float GetTargetAttackRange(Destructible target)
        {
            return GetAttackRange(target) + target.hit_range;
        }

        public bool IsAttackTargetInRange(Destructible target)
        {
            if (target != null)
            {
                float dist = (target.transform.position - character.GetInteractCenter()).magnitude;
                return dist < GetTargetAttackRange(target);
            }
            return false;
        }

        public int GetArmor()
        {
            int armor = base_armor;
            foreach (KeyValuePair<int, InventoryItemData> pair in character.EquipData.items)
            {
                ItemData idata = ItemData.Get(pair.Value?.item_id);
                if (idata != null)
                    armor += idata.armor;
            }

            armor += Mathf.RoundToInt(armor * character.Attributes.GetBonusEffectTotal(BonusType.ArmorBoost));

            return armor;
        }

        //Count total number of things killed of that type
        public int CountTotalKilled(CraftData craftable)
        {
            if (craftable != null)
                return character.SaveData.GetKillCount(craftable.id);
            return 0;
        }

        public void ResetKillCount(CraftData craftable)
        {
            if (craftable != null)
                character.SaveData.ResetKillCount(craftable.id);
        }

        public void ResetKillCount()
        {
            character.SaveData.ResetKillCount();
        }

        public bool IsDead()
        {
            return is_dead;
        }

        public PlayerCharacter GetCharacter()
        {
            return character;
        }
    }
}
