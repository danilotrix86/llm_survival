using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{

    /// <summary>
    /// Add this script to any temporary FX so it is destroyed after 'lifetime' seconds
    /// </summary>

    public class SpawnFX : MonoBehaviour
    {

        public float lifetime = 5f; //In seconds

        void Start()
        {
            Destroy(gameObject, lifetime);
        }
    }

}
