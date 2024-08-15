using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameEnder : MonoBehaviour
{

	[SerializeField]
	private GameObject mountPoint;

    // Start is called before the first frame update
    void Start()
    {
        
    }

	void OnEnable(){
		GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
		playerObject.SetActive(false);
		Camera.main.enabled = false;
		
	}

    // Update is called once per frame
    void Update()
    {
        
    }
	
	public void End(){
		SceneManager.LoadScene(0);
	}
}
