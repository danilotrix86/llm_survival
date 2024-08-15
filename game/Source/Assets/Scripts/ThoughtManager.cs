using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class ThoughtManager : MonoBehaviour
{
	private static ThoughtManager instance;
	public static ThoughtManager Instace(){ return instance; }

	[SerializeField] GameObject thoughtBubble;
	[SerializeField] Image iconHolder;

	GameObject observationPanel;

	[SerializeField] private float thoughtDuration = 5.6f;


	Quaternion originalRotation;

	void Awake(){
		instance = this;
	}

	void Start(){
		originalRotation = transform.rotation;
	}

	void Update(){
		transform.rotation = originalRotation;
	}

	public void SetText(string thought, Sprite icon){
		if(icon != null) { 
			thoughtBubble.SetActive(true);
			iconHolder.sprite = icon;
		}

		observationPanel = GameObject.FindGameObjectWithTag("ThoughtPanel");
		TextMeshProUGUI text = observationPanel.GetComponentInChildren<TextMeshProUGUI>();

		observationPanel.GetComponent<Image>().enabled = true;
		text.enabled = true;

		text.SetText(thought);

		StopCoroutine("Deactivate");
		StartCoroutine("Deactivate");
	}


	public void AddText(string addition){
		observationPanel = GameObject.FindGameObjectWithTag("ThoughtPanel");
		TextMeshProUGUI text = observationPanel.GetComponentInChildren<TextMeshProUGUI>();
		SetText(text.text + "\n" + addition, null);

	}

	IEnumerator Deactivate(){
		yield return new WaitForSecondsRealtime(thoughtDuration);

		thoughtBubble.SetActive(false);
		observationPanel.GetComponent<Image>().enabled = false;
		observationPanel.GetComponentInChildren<TextMeshProUGUI>().enabled = false;
	}
}
