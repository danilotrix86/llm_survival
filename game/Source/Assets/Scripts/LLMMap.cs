using System.Collections;
using System.Collections.Generic;
using SurvivalEngine.WorldGen;
using Unity.VisualScripting;
using UnityEngine;

struct LLMZone{
    public BiomeZone zone;
    public bool known;

    public LLMZone(BiomeZone _zone){
        zone = _zone;
        known = false;
    }
}

public class LLMMap : MonoBehaviour
{
    [SerializeField]
    GameObject worldParent;

    LLMZone[] zones;

    public void DiscoverZone(GameObject position){

    }

    // Start is called before the first frame update
    void Start()
    {
        // Generate map
        BiomeZone[] mapObjects = worldParent.GetComponentsInChildren<BiomeZone>();

        zones = new LLMZone[mapObjects.Length];

        for(int i=0; i < zones.Length; i++){
            zones[i] = new LLMZone(mapObjects[i]);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
