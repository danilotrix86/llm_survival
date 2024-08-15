using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    /// <summary>
    /// Hide / Show object that has a UID
    /// </summary>

    [RequireComponent(typeof(UniqueID))]
    public class HideShowObject : MonoBehaviour
    {
        public bool visible_at_start = true;

        private UniqueID unique_id;

        private void Awake()
        {
            unique_id = GetComponent<UniqueID>();
        }

        void Start()
        {
            if (HasUID() && PlayerData.Get().HasHiddenState(unique_id.unique_id))
                gameObject.SetActive(!PlayerData.Get().IsObjectHidden(unique_id.unique_id));
            else
                gameObject.SetActive(visible_at_start);

            if(!HasUID() && Time.time < 0.1f)
                Debug.LogError("UID is empty on " + gameObject.name + ". It is required for HideShowObject");
        }

        public void Show()
        {
            unique_id.Show();
        }

        public void Hide()
        {
            unique_id.Hide();
        }

        public bool HasUID()
        {
            return unique_id.HasUID();
        }
    }

}
