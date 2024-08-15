using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SurvivalEngine
{
    public enum BuildableType {

        Default=0,
        Grid=10,

    }

    /// <summary>
    /// Buildable is the base script for objects that can be placed on the map and have a build mode (transparent version of model that follow mouse)
    /// </summary>

    public class Buildable : MonoBehaviour
    {
        [Header("Buildable")]
        public BuildableType type;
        public float build_distance = 2f;
        public float grid_size = 1f;

        [Header("Build Obstacles")]
        public LayerMask floor_layer = 1 << 9; //Will build on top of this layer
        public LayerMask obstacle_layer = 1; //Cant build if of those obstacles
        public float build_obstacle_radius = 0.4f; //Can't build if obstacles within radius
        public float build_ground_dist = 0.4f; //Ground must be at least this distance on all 4 sides to build (prevent building on a wall or in the air)
        public bool build_flat_floor = false; //If true, must be built on mostly flat floor
        public bool auto_destroy = false; //If true, will be destroyed if the floor underneath get destroyed

        [Header("FX")]
        public AudioClip build_audio;
        public GameObject build_fx;

		[SerializeField]
        public UnityAction onBuild;

        protected Selectable selectable; //Can be nulls
        protected Destructible destruct; //Can be nulls
        protected UniqueID unique_id; //Can be nulls

        private PlayerCharacter building_character = null;
        private bool building_mode = false; //Building mode means player is selecting where to build it, but it doesnt really exists yet
        private bool position_set = false;
        private bool visible_set = true;
        private Color prev_color = Color.white;
        private float manual_rotate = 0f;
        private float update_timer = 0f;

        private List<Collider> colliders = new List<Collider>();
        private List<MeshRenderer> renders = new List<MeshRenderer>();
        private List<Material> materials = new List<Material>();
        private List<Material> materials_transparent = new List<Material>();
        private List<Color> materials_color = new List<Color>();

        void Awake()
        {
            selectable = GetComponent<Selectable>();
            destruct = GetComponent<Destructible>();
            unique_id = GetComponent<UniqueID>();
            renders.AddRange(GetComponentsInChildren<MeshRenderer>());

            foreach (MeshRenderer render in renders)
            {
                foreach (Material material in render.sharedMaterials)
                {
                    bool valid_mat = material && MaterialTool.HasColor(material);
                    Material material_normal = valid_mat ? new Material(material) : null;
                    Material material_trans = valid_mat ? new Material(material) : null;
                    if (material_trans != null)
                        MaterialTool.ChangeRenderMode(material_trans, BlendMode.Fade);
                    materials.Add(material_normal);
                    materials_transparent.Add(material_trans);
                    materials_color.Add(valid_mat ? material.color : Color.white);
                }
            }

            foreach (Collider collide in GetComponentsInChildren<Collider>())
            {
                if (collide.enabled && !collide.isTrigger)
                {
                    colliders.Add(collide);
                }
            }
        }

        void Start()
        {

        }

        void Update()
        {
            if (building_mode && building_character != null)
            {
                PlayerControls constrols = PlayerControls.Get(building_character.player_id);
                PlayerControlsMouse mouse = PlayerControlsMouse.Get();

                if (!position_set)
                {
                    if (constrols.IsGamePad())
                    {
                        //Controller Game Pad building
                        Vector3 forward = TheCamera.Get().IsFreeRotation() ? TheCamera.Get().GetFacingFront() : building_character.transform.forward;
                        transform.position = building_character.transform.position + forward * GetBuildRange(building_character);
                        transform.rotation = Quaternion.Euler(0f, manual_rotate, 0f) * Quaternion.LookRotation(building_character.GetFacing(), Vector3.up);
                    }
                    else
                    {
                        //Mouse/Touch controls
                        transform.position = mouse.GetPointingPos();
                        transform.rotation = Quaternion.Euler(0f, manual_rotate, 0f) * TheCamera.Get().GetFacingRotation();
                    }

                    //Snap to grid
                    FindAutoPosition();

                    //Show/Hide on mobile
                    if (TheGame.IsMobile())
                    {
                        SetBuildVisible(mouse.IsMouseHold());
                    }
                }

                bool can_build = CheckIfCanBuild();
                Color color = can_build ? Color.white : Color.red * 0.9f;
                SetModelColor(new Color(color.r, color.g, color.b, 0.5f), !can_build);

            }

            update_timer += Time.deltaTime;
            if (update_timer > 0.5f) {
                update_timer = Random.Range(-0.02f, 0.02f);
                SlowUpdate(); //Optimization
            }
            
        }

        private void SlowUpdate() {
            if (destruct != null && auto_destroy && !destruct.IsDead() && !building_mode)
            {
                if (!CheckValidFloorBuilt() || !CheckIfFlatGround())
                {
                    destruct.Kill();
                }
            }
        }

        public void StartBuild(PlayerCharacter character)
        {
            building_mode = true;
            position_set = false;
            building_character = character;

            if(selectable != null)
                selectable.enabled = false;
            if (destruct)
                destruct.enabled = false;

            foreach (Collider collide in colliders)
                collide.isTrigger = true;

            if (TheGame.IsMobile()) //Hide on mobile
            {
                SetBuildVisible(false);
            }
        }

        public void SetBuildVisible(bool visible)
        {
            if (visible_set != visible)
            {
                visible_set = visible;
                foreach (MeshRenderer mesh in renders)
                    mesh.enabled = visible;
            }
        }

        public void SetBuildPositionTemporary(Vector3 pos)
        {
            if (building_mode)
            {
                transform.position = pos;
                FindAutoPosition();
            }
        }

        public void SetBuildPosition(Vector3 pos)
        {
            if (building_mode)
            {
                position_set = true;
                transform.position = pos;

                FindAutoPosition();

                SetBuildVisible(true);
            }
        }

        public void FinishBuild()
        {
            gameObject.SetActive(true);
            building_mode = false;
            position_set = true;
            building_character = null;

            foreach (Collider collide in colliders)
                collide.isTrigger = false;

			foreach(Animator animator in GetComponents<Animator>()){
				animator.enabled = true;
			}

            SetBuildVisible(true);

            if (selectable != null)
                selectable.enabled = true;

            if (destruct)
                destruct.enabled = true;

            SetModelColor(Color.white, false);

            if (build_fx != null)
                Instantiate(build_fx, transform.position, Quaternion.identity);
            
            if (onBuild != null)
                onBuild.Invoke();

			foreach(Transform child in transform){
				child.gameObject.SetActive(true);
			}
        }

        //Call this function to rotate building manually (like by using a key)
        public void RotateManually(float angle_y)
        {
            if (building_mode)
            {
                manual_rotate += angle_y;
            }
        }

        private void SetModelColor(Color color, bool replace)
        {
            if (color != prev_color)
            {
                int index = 0;
                foreach (MeshRenderer render in renders)
                {
                    Material[] mesh_materials = render.sharedMaterials;
                    for (int i = 0; i < mesh_materials.Length; i++)
                    {
                        if (index < materials.Count && index < materials_transparent.Count)
                        {
                            Material mesh_mat = mesh_materials[i];
                            Material ref_mat = color.a < 0.99f ? materials_transparent[index] : materials[index];
                            if (ref_mat != null)
                            {
                                ref_mat.color = materials_color[index] * color;
                                if (replace)
                                    ref_mat.color = color;
                                if (ref_mat != mesh_mat)
                                    mesh_materials[i] = ref_mat;
                            }
                        }
                        index++;
                    }
                    render.sharedMaterials = mesh_materials;
                }
            }

            prev_color = color;
        }

        //Check if possible to build at current position
        public bool CheckIfCanBuild()
        {
            bool dont_overlap = !CheckIfOverlap();
            bool flat_ground = CheckIfFlatGround();
            bool accessible = CheckIfAccessible();
            bool valid_ground = CheckValidFloor();
            //Debug.Log(dont_overlap + " " + flat_ground + " " + accessible + " " + valid_ground);
            return dont_overlap && flat_ground && valid_ground && accessible;
        }

        //Check if overlaping another object (cant build)
        public bool CheckIfOverlap()
        {
            List<Collider> overlap_colliders = new List<Collider>();
            LayerMask olayer = obstacle_layer & ~floor_layer;  //Remove floor layer from obstacles

            //Check collision with bounding box
            foreach (Collider collide in colliders) {
                Collider[] over = Physics.OverlapBox(transform.position, collide.bounds.extents, Quaternion.identity, olayer);
                foreach (Collider overlap in over)
                {
                    if(!overlap.isTrigger)
                        overlap_colliders.Add(overlap);
                }
            }

            //Check collision with radius (includes triggers)
            if (build_obstacle_radius > 0.01f)
            {
                Collider[] over = Physics.OverlapSphere(transform.position, build_obstacle_radius, olayer);
                overlap_colliders.AddRange(over);
            }

            //Check collision list
            foreach (Collider overlap in overlap_colliders)
            {
                if (overlap != null)
                {
                    //Dont overlap with player and dont overlap with itself
                    PlayerCharacter player = overlap.GetComponent<PlayerCharacter>();
                    Buildable buildable = overlap.GetComponentInParent<Buildable>();
                    if (player == null && buildable != this){
						Debug.DrawLine(overlap.transform.position, transform.position);
                        return true;
					}
                }
            }

            return false;
        }

        //Make sure there is no obstacles in between the player and the building, this applies only if placing with keyboard/gamepad
        public bool CheckIfAccessible()
        {
            PlayerControls controls = PlayerControls.Get(building_character.player_id);
            bool game_pad = controls != null && controls.IsGamePad();

            if (position_set || !game_pad)
                return true; //Dont check this is placing with mouse or if position already set

            if (building_character != null)
            {
                Vector3 center = building_character.GetColliderCenter();
                Vector3 build_center = transform.position + Vector3.up * build_ground_dist;
                Vector3 dir = build_center - center;

                RaycastHit h1;
                bool f1 = PhysicsTool.RaycastCollision(center, dir, out h1);

                return !f1 || h1.collider.GetComponentInParent<Buildable>() == this;
            }
            return false;
        }

        //Check if there is a flat floor underneath (can't build a steep cliff)
        public bool CheckIfFlatGround()
        {
            if (!build_flat_floor)
                return true; //Dont check for flat ground

            Vector3 center = transform.position + Vector3.up * build_ground_dist;
            Vector3 p0 = center;
            Vector3 p1 = center + Vector3.right * build_obstacle_radius;
            Vector3 p2 = center + Vector3.left * build_obstacle_radius;
            Vector3 p3 = center + Vector3.forward * build_obstacle_radius;
            Vector3 p4 = center + Vector3.back * build_obstacle_radius;
            Vector3 dir = Vector3.down * (build_ground_dist + build_ground_dist);

            RaycastHit h0, h1, h2, h3, h4;
            bool f0 = PhysicsTool.RaycastCollision(p0, dir, out h0);
            bool f1 = PhysicsTool.RaycastCollision(p1, dir, out h1);
            bool f2 = PhysicsTool.RaycastCollision(p2, dir, out h2);
            bool f3 = PhysicsTool.RaycastCollision(p3, dir, out h3);
            bool f4 = PhysicsTool.RaycastCollision(p4, dir, out h4);

            return f0 && f1 && f2 && f3 && f4;
        }

        //Check if ground is valid layer, this one check only the first hit collision (more strict)
        public bool CheckValidFloor()
        {
            Vector3 center = transform.position + Vector3.up * build_ground_dist;
            Vector3 p0 = center;
            Vector3 p1 = center + Vector3.right * build_obstacle_radius;
            Vector3 p2 = center + Vector3.left * build_obstacle_radius;
            Vector3 p3 = center + Vector3.forward * build_obstacle_radius;
            Vector3 p4 = center + Vector3.back * build_obstacle_radius;
            Vector3 dir = Vector3.down * (build_ground_dist + build_ground_dist);

            RaycastHit h0, h1, h2, h3, h4;
            bool f0 = PhysicsTool.RaycastCollision(p0, dir, out h0);
            bool f1 = PhysicsTool.RaycastCollision(p1, dir, out h1);
            bool f2 = PhysicsTool.RaycastCollision(p2, dir, out h2);
            bool f3 = PhysicsTool.RaycastCollision(p3, dir, out h3);
            bool f4 = PhysicsTool.RaycastCollision(p4, dir, out h4);
            f0 = f0 && PhysicsTool.IsLayerInLayerMask(h0.collider.gameObject.layer, floor_layer);
            f1 = f1 && PhysicsTool.IsLayerInLayerMask(h1.collider.gameObject.layer, floor_layer);
            f2 = f2 && PhysicsTool.IsLayerInLayerMask(h2.collider.gameObject.layer, floor_layer);
            f3 = f3 && PhysicsTool.IsLayerInLayerMask(h3.collider.gameObject.layer, floor_layer);
            f4 = f4 && PhysicsTool.IsLayerInLayerMask(h4.collider.gameObject.layer, floor_layer);

            if(build_flat_floor)
                return f1 && f2 && f3 && f4 && f0; //Floor must be valid on all sides
            else
                return f1 || f2 || f3 || f4 || f0; //Floor must be valid only on one side
        }

        //Check if its still valid floor after built, this one ignore itself and check only the layer (less strict)
        public bool CheckValidFloorBuilt()
        {
            Vector3 center = transform.position + Vector3.up * build_ground_dist;
            Vector3 p0 = center;
            Vector3 p1 = center + Vector3.right * build_obstacle_radius;
            Vector3 p2 = center + Vector3.left * build_obstacle_radius;
            Vector3 p3 = center + Vector3.forward * build_obstacle_radius;
            Vector3 p4 = center + Vector3.back * build_obstacle_radius;
            Vector3 dir = Vector3.down * (build_ground_dist + build_ground_dist);

            RaycastHit h0, h1, h2, h3, h4;
            bool f0 = PhysicsTool.RaycastCollisionLayer(p0, dir, floor_layer, out h0);
            bool f1 = PhysicsTool.RaycastCollisionLayer(p1, dir, floor_layer, out h1);
            bool f2 = PhysicsTool.RaycastCollisionLayer(p2, dir, floor_layer, out h2);
            bool f3 = PhysicsTool.RaycastCollisionLayer(p3, dir, floor_layer, out h3);
            bool f4 = PhysicsTool.RaycastCollisionLayer(p4, dir, floor_layer, out h4);

            return f1 || f2 || f3 || f4 || f0;
        }

        private void FindAutoPosition()
        {
            if (IsGrid())
            {
                transform.position = FindGridPosition(transform.position);
                transform.rotation = FindGridRotation(transform.rotation);
            }

            transform.position = FindBuildPosition(transform.position, floor_layer);
        }

        //Find the height to build
        public Vector3 FindBuildPosition(Vector3 pos, LayerMask mask)
        {
            float offset = build_distance;
            Vector3 center = pos + Vector3.up * offset;
            Vector3 p0 = center;
            Vector3 p1 = center + Vector3.right * build_obstacle_radius;
            Vector3 p2 = center + Vector3.left * build_obstacle_radius;
            Vector3 p3 = center + Vector3.forward * build_obstacle_radius;
            Vector3 p4 = center + Vector3.back * build_obstacle_radius;
            Vector3 dir = Vector3.down * (offset + build_ground_dist);

            RaycastHit h0, h1, h2, h3, h4;
            bool f0 = PhysicsTool.RaycastCollisionLayer(p0, dir, mask, out h0);
            bool f1 = PhysicsTool.RaycastCollisionLayer(p1, dir, mask, out h1);
            bool f2 = PhysicsTool.RaycastCollisionLayer(p2, dir, mask, out h2);
            bool f3 = PhysicsTool.RaycastCollisionLayer(p3, dir, mask, out h3);
            bool f4 = PhysicsTool.RaycastCollisionLayer(p4, dir, mask, out h4);

            Vector3 dist_dir = Vector3.down * build_distance;
            if (f0 && h0.distance < dist_dir.magnitude) { dist_dir = Vector3.down * h0.distance; }
            if (f1 && h1.distance < dist_dir.magnitude) { dist_dir = Vector3.down * h1.distance; }
            if (f2 && h2.distance < dist_dir.magnitude) { dist_dir = Vector3.down * h2.distance; }
            if (f3 && h3.distance < dist_dir.magnitude) { dist_dir = Vector3.down * h3.distance; }
            if (f4 && h4.distance < dist_dir.magnitude) { dist_dir = Vector3.down * h4.distance; }

            return center + dist_dir;
        }

        public Vector3 FindGridPosition(Vector3 pos) {
            if (grid_size >= 0.1f)
            {
                float x = Mathf.RoundToInt(pos.x / grid_size) * grid_size;
                float y = Mathf.RoundToInt(pos.y / grid_size) * grid_size;
                float z = Mathf.RoundToInt(pos.z / grid_size) * grid_size;
                return new Vector3(x, y, z);
            }
            return pos;
        }

        public Quaternion FindGridRotation(Quaternion rot)
        {
            Vector3 euler = rot.eulerAngles;
            float angle = Mathf.RoundToInt(euler.y / 90f) * 90f;
            return Quaternion.Euler(euler.x, angle, euler.z);
        }

        public float GetBuildRange(PlayerCharacter character)
        {
            return build_distance + character.interact_range;
        }

        public bool IsBuilding()
        {
            return building_mode;
        }

        public bool IsPositionSet()
        {
            return position_set;
        }

        public bool IsGrid()
        {
            return type == BuildableType.Grid;
        }

        public Destructible GetDestructible()
        {
            return destruct; //May be null
        }

        public Selectable GetSelectable()
        {
            return selectable;
        }

        public string GetUID()
        {
            if (unique_id != null)
                return unique_id.unique_id;
            return "";
        }
    }

}