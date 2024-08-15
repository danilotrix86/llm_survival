using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine
{
    //Allow sprites to receive shadow

    [ExecuteInEditMode]
    public class SpriteShadow : MonoBehaviour
    {
        void Start()
        {
            if (GetComponent<Renderer>())
            {
                GetComponent<Renderer>().receiveShadows = true;
            }
        }
    }

}
