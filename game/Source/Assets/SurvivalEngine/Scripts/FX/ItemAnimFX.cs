using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    /// <summary>
    /// Animation on an equipped item that will appear when character doing a specific animation
    /// </summary>

    public class ItemAnimFX : MonoBehaviour
    {
        public string anim;
        public GameObject fx;

        private EquipItem item;

        void Start()
        {
            fx.SetActive(false);

            item = GetComponent<EquipItem>();

            PlayerCharacter character = item.GetCharacter();
            if (character != null)
                character.onTriggerAnim += OnAnim;
        }

        private void OnDestroy()
        {
            PlayerCharacter character = item.GetCharacter();
            if (character != null)
                character.onTriggerAnim -= OnAnim;
        }

        private void OnAnim(string anim, float duration)
        {
            if (this.anim == anim)
                StartCoroutine(RunFX(duration));
        }

        private IEnumerator RunFX(float duration)
        {
            fx.SetActive(true);
            yield return new WaitForSeconds(duration);
            fx.SetActive(false);
        }
    }

}
