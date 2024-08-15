using System.Collections;
using System.Collections.Generic;
using SurvivalEngine.WorldGen;
using UnityEngine;
using UnityEngine.Events;

namespace SurvivalEngine
{
    public enum SelectableType
    {
        Interact=0, //Interacts at the pivot position of the object (transform position)
        InteractBound=5, //When its set to Bound, will interest with the nearest position in the collider's bounding box
        InteractSurface=10, //When it's a surface, can be interacted with at different position among the surface, instead of just the center
        CantInteract =20, //Can be clicked on/hovered, but cant interact
        CantSelect=30, //Cannot be clicked on or hovered
    }

    /// <summary>
    /// Anything the player can interact with is a Selectable
    /// Most objects are selectable (anything the player can click on). 
    /// Selectables can contain actions.
    /// Selectables are deactivated when too far from the camera. For game performance.
    /// Author: Indie Marc (Marc-Antoine Desbiens)
    /// </summary>

    public class Selectable : MonoBehaviour
    {
        public SelectableType type; 
        public float use_range = 2f;

        [Header("Action")]
        public SAction[] actions;

        [Header("Groups")]
        public GroupData[] groups;

        [Header("Optimization")]
        public float active_range = 40f; //If farther than this, will be disabled for optim
        public bool always_run_scripts = false; //Set to true to have other scripts still run this one is not active

        [Header("Outline")]
        public GameObject outline; //Toggle a child object as the outline
        public bool generate_outline = false; //This will generate the outline automatically (will use the first mesh found)
        public Material outline_material; //Material used when generating the outline

        [HideInInspector]
        public bool dont_optimize = false; //If true, will never be turned  off by optimizer

        [HideInInspector]
        public bool dont_destroy = false; //If true, will not be destroyed automatically

        public UnityAction onSelect; //When clicked with mouse, before reaching destination
        public UnityAction<PlayerCharacter> onUse; //After clicked, when character reaches use distance, or when using action button while nearby
		public UnityAction                afterUse;
        public UnityAction onDestroy;

        private Collider[] colliders;
        private Destructible destruct; //May be null, not all selectables have one, so check if null first (quick access for optimization)
        private Character character; //May be null, not all selectables have one, so check if null first (quick access for optimization)
        private UniqueID unique_id; //May be null,  not all selectables have one, so check if null first
        private Transform transf; //Quick access to last position
        private bool is_hovered = false;
        private bool is_active = true;

        private List<MonoBehaviour> scripts = new List<MonoBehaviour>();
        private List<Animator> animators = new List<Animator>();

        private List<GroupData> active_groups = new List<GroupData>();

        private static HashSet<Selectable> active_list = new HashSet<Selectable>();
		public static Dictionary<GroupData, HashSet<Selectable>> groupList = new Dictionary<GroupData, HashSet<Selectable>>();
        private static List<Selectable> selectable_list = new List<Selectable>();
        private static GameObject fx_parent;

        void Awake()
        {
            destruct = GetComponent<Destructible>();
            character = GetComponent<Character>();
            unique_id = GetComponent<UniqueID>();
            colliders = GetComponentsInChildren<Collider>();
            selectable_list.Add(this);
            active_list.Add(this);
            transf = transform;
            is_active = true;
            scripts.AddRange(GetComponents<MonoBehaviour>());
            animators.AddRange(GetComponentsInChildren<Animator>());

            if (groups != null){
                active_groups.AddRange(groups);

				foreach(GroupData group in active_groups){
					if(!groupList.ContainsKey(group)) groupList.Add(group, new HashSet<Selectable>());
					groupList[group].Add(this);
				}
			}
        }

        void OnDestroy()
        {
            selectable_list.Remove(this);
            active_list.Remove(this);


			foreach(GroupData group in active_groups){
				if(groupList.ContainsKey(group)) groupList[group].Remove(this);
			}
        }

        void Start()
        {
            GenerateAutomaticOutline();

            if ((TheGame.IsMobile() || PlayerControls.IsAnyGamePad()) && groups.Length > 0 && AssetData.Get().item_merge_fx != null)
            {
                if (fx_parent == null)
                    fx_parent = new GameObject("FX");

                GameObject fx = Instantiate(AssetData.Get().item_merge_fx, transform.position, AssetData.Get().item_merge_fx.transform.rotation);
                fx.GetComponent<ItemMergeFX>().target = this;
                fx.transform.SetParent(fx_parent.transform);
            }
			afterUse += () =>LLMInterface.Instance().StopCommand(negativefilter: "sleep");
        }

        private void GenerateAutomaticOutline()
        {
            //Generate automatic outline object
            if (generate_outline && outline_material != null)
            {
                MeshRenderer[] renders = GetComponentsInChildren<MeshRenderer>();
                foreach (MeshRenderer render in renders)
                {
                    GameObject new_outline = Instantiate(render.gameObject, render.transform.position, render.transform.rotation);
                    new_outline.name = "OutlineMesh";
                    new_outline.transform.localScale = render.transform.lossyScale; //Preserve scale from parents
                    new_outline.SetActive(false);

                    foreach (MonoBehaviour script in new_outline.GetComponents<MonoBehaviour>())
                        script.enabled = false; //Disable scripts

                    MeshRenderer out_render = new_outline.GetComponent<MeshRenderer>();
                    Material[] mats = new Material[out_render.sharedMaterials.Length];
                    for (int i = 0; i < mats.Length; i++)
                        mats[i] = outline_material;
                    out_render.sharedMaterials = mats;
                    out_render.allowOcclusionWhenDynamic = false;
                    out_render.receiveShadows = false;
                    out_render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    if (outline != null)
                    {
                        new_outline.transform.SetParent(outline.transform);
                    }
                    else
                    {
                        new_outline.transform.SetParent(transform);
                        outline = new_outline;
                    }
                }
            }
        }

        public void Select()
        {
            if (onSelect != null)
                onSelect.Invoke();
        }

        //When the character interact with this selectable, check all the actions and see if any should be triggered.
        public void Use(PlayerCharacter character, Vector3 pos)
        {
            if (enabled)
            {
                PlayerUI ui = PlayerUI.Get(character.player_id);
                ItemSlot slot = ui?.GetSelectedSlot();

                MAction maction = slot?.GetItem()?.FindMergeAction(this);
                AAction aaction = FindAutoAction(character);

                if (maction != null && maction.CanDoAction(character, slot, this))
                {
                    maction.DoAction(character, slot, this);
                    PlayerUI.Get(character.player_id)?.CancelSelection();
                }
                else if (aaction != null && aaction.CanDoAction(character, this))
                {
                    aaction.InvokeAction(character, this);
                }
                else if (actions.Length > 0)
                {
                    ActionSelector.Get(character.player_id)?.Show(character, this, pos);
                }

                if (onUse != null){
                    onUse.Invoke(character);
				}

				if(afterUse != null && !character.IsBusy()){
					afterUse.Invoke();
				}
            }
        }

        //This is used by TheRender to hide far away selectable
        public void SetActive(bool visible, bool turn_off_gameobject=false)
        {
            if (is_active != visible)
            {
                if (!dont_optimize || visible)
                {
                    if (turn_off_gameobject && !always_run_scripts)
                        gameObject.SetActive(visible);

                    this.enabled = visible;
                    is_active = visible;
                    is_hovered = false;

                    if (!turn_off_gameobject && !always_run_scripts)
                    {
                        foreach (MonoBehaviour script in scripts)
						{
							if(script != null)
								script.enabled = visible;
						}

                        foreach (Animator anim in animators)
                        {
                            if (anim != null)
                                anim.enabled = visible;
                        }
                    }

                    if (visible){
                        active_list.Add(this);
					} else{
                        active_list.Remove(this);
					}
                }
            }
        }

        public void SetHover(bool value)
        {
            is_hovered = value;

            if (outline != null && is_hovered != outline.activeSelf)
                outline.SetActive(is_hovered);
        }

        public AAction FindAutoAction(PlayerCharacter character)
        {
            foreach (SAction action in actions)
            {
                if (action != null && action is AAction)
                {
                    AAction aaction = (AAction)action;
                    if (aaction.CanDoAction(character, this))
                        return aaction;
                }
            }
            return null;
        }

        public virtual void Destroy(float delay = 0f)
        {
            if (!dont_destroy)
                Destroy(gameObject, delay);

            if (onDestroy != null)
                onDestroy.Invoke();
        }

        public Transform GetTransform()
        {
            return transf;
        }

        public Vector3 GetPosition()
        {
            return transf.position;
        }

        public bool IsHovered()
        {
            return is_hovered;
        }

        public bool IsActive()
        {
            return is_active && enabled;
        }

        public bool AreScriptsActive()
        {
            return is_active || always_run_scripts;
        }

        //Player can interact with it by clicking on it, such as using actions
        public bool CanBeClicked()
        {
            return is_active && enabled && type != SelectableType.CantSelect;
        }

        //Player can interact with it by clicking on it, such as using actions
        public bool CanBeInteracted()
        {
            return is_active && enabled && type != SelectableType.CantInteract && type != SelectableType.CantSelect;
        }

        //Player can interact automatically with this
        public bool CanAutoInteract()
        {
            return CanBeInteracted() && (onUse != null || actions.Length > 0);
        }

        public void AddGroup(GroupData group)
        {
            if (!active_groups.Contains(group)){
                active_groups.Add(group);
				groupList[group].Add(this);
			}
        }

        public void RemoveGroup(GroupData group)
        {
            if (active_groups.Contains(group)){
                active_groups.Remove(group);
				groupList[group].Remove(this);
			}
        }

        public bool HasGroup(GroupData group)
        {
            foreach (GroupData agroup in active_groups)
            {
                if (agroup == group)
                    return true;
            }
            return false;
        }

        public bool HasGroup(GroupData[] mgroups)
        {
            foreach (GroupData mgroup in mgroups)
            {
                foreach (GroupData agroup in active_groups)
                {
                    if (agroup == mgroup)
                        return true;
                }
            }
            return false;
        }

        public bool IsInUseRange(PlayerCharacter character)
        {
            Vector3 select_pos = GetClosestInteractPoint(character.transform.position);
            float dist = (select_pos - character.transform.position).magnitude;
            return dist <= use_range + character.interact_range;
        }

        public float GetUseRange(PlayerCharacter character)
        {
            return use_range + character.interact_range;
        }

        public bool IsNearCamera(float distance)
        {
            float dist = (transf.position - TheCamera.Get().GetTargetPos()).magnitude;
            return dist < distance;
        }

        public Vector3 GetClosestInteractPoint(Vector3 pos)
        {
            if (type == SelectableType.InteractBound || type == SelectableType.InteractSurface)
                return GetClosestPoint(pos);
            return transf.position; //If not surface, always interact from center
        }

        public Vector3 GetClosestInteractPoint(Vector3 pos, Vector3 click_pos)
        {
            if (type == SelectableType.InteractBound)
                return GetClosestPoint(pos);
            if (type == SelectableType.InteractSurface)
                return click_pos; //If surface, interact where you clicked
            return transf.position; //If not surface, always interact from center
        }

        public Vector3 GetClosestPoint(Vector3 pos) 
        {
            //A bit slow dont run every frame
            Vector3 nearest = transf.position;
            float min_dist = (transf.position - pos).magnitude;
            foreach (Collider collide in colliders)
            {
                Vector3 npos = collide.bounds.ClosestPoint(pos);
                float dist = (npos - pos).magnitude;
                if (dist < min_dist)
                {
                    min_dist = dist;
                    nearest = npos;
                }
            }
            return nearest;
        }

        public Destructible Destructible { get { return destruct; } } //May be null, beware!
        public Character Character{ get { return character; } } //May be null, beware!

        public string GetUID()
        {
            if (unique_id != null)
                return unique_id.unique_id;
            return "";
        }

        public static Selectable GetNearestRaycast(float range=10f)
        {
            float min_dist = range;
            Selectable nearest = null;
            Vector3 pos = TheCamera.Get().transform.position;
            foreach (Selectable select in PlayerControlsMouse.Get().GetRaycastList())
            {
                Vector3 dist = (select.transf.position - pos);
                if (dist.magnitude < min_dist)
                {
                    nearest = select;
                    min_dist = dist.magnitude;
                }
            }
            return nearest;
        }

        //Get nearest active selectable
        public static Selectable GetNearest(Vector3 pos, float range = 999f)
        {
            Selectable nearest = null;
            float min_dist = range;
            foreach (Selectable select in active_list)
            {
                if (select.enabled && select.gameObject.activeSelf)
                {
                    float dist = (select.transf.position - pos).magnitude;
                    if (dist < min_dist)
                    {
                        min_dist = dist;
                        nearest = select;
                    }
                }
            }
            return nearest;
        }

        //Get nearest active hovered seletable
        public static Selectable GetNearestHover(Vector3 pos, float range = 999f)
        {
            Selectable nearest = null;
            float min_dist = range;
            foreach (Selectable select in active_list)
            {
                if (select.enabled && select.gameObject.activeSelf && select.IsHovered())
                {
                    float dist = (select.transf.position - pos).magnitude;
                    if (dist < min_dist)
                    {
                        min_dist = dist;
                        nearest = select;
                    }
                }
            }
            return nearest;
        }

        //Get nearest active selectable that can be interacted with
        public static Selectable GetNearestAutoInteract(Vector3 pos, float range = 999f)
        {
            Selectable nearest = null;
            float min_dist = range;
            foreach (Selectable select in active_list)
            {
                if (select.enabled && select.gameObject.activeSelf && select.CanAutoInteract())
                {
                    float offset = select.type == SelectableType.InteractSurface ? 2f : 0f; //Prioritize not surface by 2f
                    float dist = (select.GetClosestInteractPoint(pos) - pos).magnitude + offset;
                    if (dist < min_dist)
                    {
                        min_dist = dist;
                        nearest = select;
                    }
                }
            }
            return nearest;
        }

        public static Selectable GetNearestKnownGroup(GroupData group, Vector3 pos){
            Selectable nearest = null;
            float min_dist = 99999999999;

			if(!groupList.ContainsKey(group)) return null;

			foreach(Selectable select in groupList.GetValueOrDefault(group))
            {
                if (select.gameObject.activeSelf && select.HasGroup(group)){
					if(select.unique_id.parentCell.discovered)
					{
						float offset = select.type == SelectableType.InteractSurface ? 1f : 0f; //Prioritize not surface by 1f
						float dist = (select.GetClosestInteractPoint(pos) - pos).magnitude + offset;
						if (dist < min_dist)
						{
							min_dist = dist;
							nearest = select;
						}
					}
				}
            }
            return nearest;
        }

        //Get nearest active selectable belonging to Group
        public static Selectable GetNearestGroup(GroupData group, Vector3 pos, float range = 999f)
        {
            Selectable nearest = null;
            float min_dist = range;
            foreach (Selectable select in active_list)
            {
                if (select.enabled && select.gameObject.activeSelf && select.HasGroup(group))
                {
                    float offset = select.type == SelectableType.InteractSurface ? 1f : 0f; //Prioritize not surface by 1f
                    float dist = (select.GetClosestInteractPoint(pos) - pos).magnitude + offset;
                    if (dist < min_dist)
                    {
                        min_dist = dist;
                        nearest = select;
                    }
                }
            }
            return nearest;
        }

        //Get nearest active selectable belonging to any group in array
        public static Selectable GetNearestGroup(GroupData[] groups, Vector3 pos, float range = 999f)
        {
            Selectable nearest = null;
            float min_dist = range;
            foreach (Selectable select in active_list)
            {
                if (select.enabled && select.gameObject.activeSelf && select.HasGroup(groups))
                {
                    float offset = select.type == SelectableType.InteractSurface ? 1f : 0f; //Prioritize not surface by 1f
                    float dist = (select.GetClosestInteractPoint(pos) - pos).magnitude + offset;
                    if (dist < min_dist)
                    {
                        min_dist = dist;
                        nearest = select;
                    }
                }
            }
            return nearest;
        }

        public static Selectable GetByUID(string uid)
        {
            if (!string.IsNullOrEmpty(uid))
            {
                foreach (Selectable select in active_list)
                {
                    if (uid == select.GetUID())
                        return select;
                }
            }
            return null;
        }

        //Get all active selectables (Active selectables are all the selectable in sight of the player, the ones too far or outside of the camera are inactive)
        public static HashSet<Selectable> GetAllActive()
        {
            return active_list;
        }

        //Get ALL selectables (Careful, this list can be very big for large maps, so dont loop on it each frame)
        public static List<Selectable> GetAll()
        {
            return selectable_list;
        }
    }

}