using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LLMBiome : MonoBehaviour{
    public static List<LLMBiome> cells = new List<LLMBiome>();
    public bool discovered;

    public void Awake(){
        cells.Add(this);
    }

    public static LLMBiome getNearestCell(Vector3 pos, bool undiscovered=false){
        LLMBiome lowestCell = null;
        float lowestDistance = float.MaxValue;
        foreach(LLMBiome cell in cells){
            if(undiscovered && cell.discovered) continue;

            float currentDist = Vector3.Distance(cell.transform.position, pos);
            if(currentDist < lowestDistance){
                lowestCell = cell;
                lowestDistance = currentDist;
            }
        }

        return lowestCell;
    }

}
