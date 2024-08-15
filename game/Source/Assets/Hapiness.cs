using System.Collections;
using System.Collections.Generic;
using SurvivalEngine;
using Unity.VisualScripting;
using UnityEngine;

public class Hapiness : MonoBehaviour
{
	[SerializeField] float amount = 3;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

	void OnTriggerStay(Collider other){
		PlayerCharacterAttribute attr = other.GetComponent<PlayerCharacterAttribute>();

		if(attr){
			attr.AddAttribute(AttributeType.Stress, amount * Time.deltaTime);
		}
	}
}
