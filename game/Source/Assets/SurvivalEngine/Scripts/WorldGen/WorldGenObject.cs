using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEngine.WorldGen
{
    /// <summary>
    /// Properties of an object prefab for world gen
    /// </summary>

    public enum WorldGenObjectType {

        Default = 0,
        AvoidEdge = 10,
        NearEdge=12,

    }

    public class WorldGenObject : MonoBehaviour
    {
        public float size = 1f; //Minimum distance between this and another object
        public float size_group = 1f; //Minimum distance between this and another object of same group

        public WorldGenObjectType type;
        public float edge_dist = 0f; //Minimum (or max) distance between this and the edge of the biome

    }

}
