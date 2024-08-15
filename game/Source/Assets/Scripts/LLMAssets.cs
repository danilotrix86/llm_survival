using System.Collections;
using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using UnityEngine;

public class LLMAssets : MonoBehaviour
{
	public SerializedDictionary<string, Sprite> commandIcons;

	void Awake(){
		instance = this;
	}

	private static LLMAssets instance;
	public static LLMAssets getInstance() { return instance; }
}
