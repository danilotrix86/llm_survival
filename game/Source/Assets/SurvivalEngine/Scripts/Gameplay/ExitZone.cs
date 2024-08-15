using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    /// <summary>
    /// Zone to change scene when you enter this zone, make sure there is also a trigger collider
    /// </summary>

    public class ExitZone : MonoBehaviour
    {
        [Header("Exit")]
        public string scene;
        public int go_to_index = 0; //If you set this to 0, it will go to default character position, otherwise to the Exit zone with same index

        [Header("Entrance")]
        public int entry_index = 1; //Make sure this is > 0
        public Vector3 entry_offset;

        private float timer = 0f;

        private static List<ExitZone> exit_list = new List<ExitZone>();

        void Awake()
        {
            exit_list.Add(this);
        }

        private void OnDestroy()
        {
            exit_list.Remove(this);
        }

        void Update()
        {
            timer += Time.deltaTime;
        }

        public void EnterZone()
        {
            TheGame.Get().TransitionToScene(scene, go_to_index);
        }

        private void OnTriggerEnter(Collider collision)
        {
            if (timer > 0.1f && collision.GetComponent<PlayerCharacter>())
            {
                EnterZone();
            }
        }

        public static ExitZone GetIndex(int index)
        {
            foreach (ExitZone exit in exit_list)
            {
                if (index == exit.entry_index)
                    return exit;
            }
            return null;
        }
    }

}
