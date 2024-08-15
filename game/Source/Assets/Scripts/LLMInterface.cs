using System;
using System.Collections;
using System.Collections.Generic;
using SurvivalEngine;
using UnityEngine;
using Newtonsoft.Json;
using System.Net.Http;
using JetBrains.Annotations;
using System.Text;
using AYellowpaper.SerializedCollections;
using UnityEditor.UI;
using System.Linq;
using UnityEngine.Networking.PlayerConnection;


// Json to send to the API
class ActionOutput{
	public string action;
	public string message;
	public string status;
	public string xp;
	Dictionary<string, int> inventory;
	Dictionary<string, string> stats;


	public ActionOutput(string _action, string _message, string _status, PlayerCharacter character){
		action = _action;
		message = _message;
		status = _status;

		inventory = new Dictionary<string, int>();
		stats = new Dictionary<string, string>();

		// Count inventory
		foreach (string s in LLMStrings.ITEMS_TO_TRACK){
			int count = character.InventoryData.CountItem(s);
			inventory[s] = count;
		}

		// Count buildings
		foreach(string s in LLMStrings.BUILDINGS_TO_TRACK){
			inventory[s] = 0;
			foreach(KeyValuePair<GroupData, HashSet<Selectable>> kv in Selectable.groupList){
				if(kv.Key.group_id == s){
					inventory[s] = kv.Value.Count;
					break;
				}
			}
		}

		// Get stats
		foreach (AttributeType attr in LLMStrings.ATTR_TO_TRACK){
			float statValue = character.Attributes.GetAttributeValue(attr);
			float max       = character.Attributes.GetAttributeMax(attr);

			stats[attr.ToString().ToLower()] = LLMStrings.GetStatusLabel(statValue, max);
		}

		xp = character.Attributes.GetXP("").ToString();
	}

	public string ToJson(){
		// This isn't the cleanest, but for some reaaason the serializer cant handle the nested dictionary
		string subInv = JsonConvert.SerializeObject(inventory, Formatting.Indented);
		string subStats = JsonConvert.SerializeObject(stats, Formatting.Indented);

		string json = "{\n";
		json += $"\"action\": \"{action}\",\n";
		json += $"\"message\": \"{message}\",\n";
		json += $"\"status\": \"{status}\",\n";
		json += $"\"inventory\": {subInv},\n";
		json += $"\"player_info\": {subStats},\n";
		json += $"\"xp\": \"{xp}\"\n";
		json += "}";
		return json;
	}
}

public class LLMInterface : MonoBehaviour
{
	private static LLMInterface instance;
	public static LLMInterface Instance(){ return instance; }

	// Delay before taking action
	[SerializeField] float actionDelay = 3;

	// Delay before reporting an error
	[SerializeField] float errorDelay = 3;

	[SerializeField] float recieveTimeout = 15;
	[SerializeField] float actionTimeout = 75;

	// Command send to the manager
	string currentCommand = "";
	// Command processed from the server
	string acceptedCommand = "";
	// Command waiting to be processed from the server
	string recievedCommand = "";
	string recievedReason = "";

	List<object> args = new List<object>();


	LLMManager manager;

	public void StopCommand(string filter = "", string negativefilter = ""){
		if(currentCommand == "" || !currentCommand.StartsWith(filter)) return;
		if(negativefilter != "" && acceptedCommand.StartsWith(negativefilter)) return;

		string message = LLMStrings.GetMessage(acceptedCommand, "Success", args);
		int    xp      = LLMStrings.GetXp(acceptedCommand, "Success");
		((PlayerCharacter)manager.GetCharacter()).Attributes.GainXP("", xp);

		EmitJson(new ActionOutput(acceptedCommand, message, "success", manager.GetCharacter()));
		currentCommand = "";
		args.Clear();
	}

	IEnumerator ErrorDelay(ActionOutput action){
		yield return new WaitForSecondsRealtime(errorDelay);
		EmitJson(action);
	}

	public void ErrorCommand(string code){
		if(currentCommand == "") return;

		string message = LLMStrings.GetMessage(acceptedCommand, code, args);
		if(message == "") message = code;

		ThoughtManager.Instace().AddText("<color=\"red\">" + message + "</color>");

	 	StartCoroutine("ErrorDelay", new ActionOutput(acceptedCommand, message, "error", manager.GetCharacter()));

		currentCommand = "";
		args.Clear();
	}

	public void AddArgument(object arg){
		args.Add(arg);
	}

	public void StartCommand(string _command){
		currentCommand = _command;
		args.Clear();
	}

	public void StartCommand(string[] tokens){
		StartCommand(string.Join(LLMStrings.DELIM, tokens));
	}

	public void EmitStatus(string message){
		if(message == "") {
			//StopCommand(message);
			return;
		}
	}

	public void AcceptCommand(string command){
		acceptedCommand = command;
		StopCoroutine("ActionTimeout");
		StartCoroutine("ActionTimeout");
		manager.ProcessCommand(command);
	}

	IEnumerator ActionTimeout(){
		yield return new WaitForSecondsRealtime(actionTimeout);
		ErrorCommand("ErrorTimeout");
	}

	void EmitJson(ActionOutput action){
		string jsonString = action.ToJson();
		Debug.Log(jsonString);

		var client = new HttpClient();

		var content = new StringContent(jsonString, Encoding.UTF8, "application/json");
		client.PostAsync(LLMStrings.API_NEXT_ACTION, content).ContinueWith(async t => ProcessResponse(await (await t).Content.ReadAsStringAsync()));
	}

	void ProcessResponse(string msg){

		try{
			List<string> values = JsonConvert.DeserializeObject<List<string>>(msg);
			string action = values[0];
			string reason = values[1];
			Debug.Log("Recieved: " + action + ", " + reason);
			lock(recievedCommand){
				recievedCommand = action;
				recievedReason = reason;
			}
		}catch(Newtonsoft.Json.JsonSerializationException e){
			Debug.LogError($"Deserialization Error!\n{msg}");
		}

	}


	void Awake(){
        instance = this;
		manager = GetComponent<LLMManager>();

		LLMStrings.Setup();
	}

    // Start is called before the first frame update
    void Start()
    {
    }

	IEnumerator startAction(string action){
		yield return new WaitForSecondsRealtime(actionDelay);
		AcceptCommand(action);
	}

    // Update is called once per frame
    void Update()
    {
		foreach(var key in dCommandStr.Keys){
			if(Input.GetKeyDown(key)){
				AcceptCommand(dCommandStr.GetValueOrDefault(key));
			}
		}

		lock(recievedCommand){
			if(recievedCommand != ""){
				Sprite icon = LLMAssets.getInstance().commandIcons.GetValueOrDefault(recievedCommand, null);
				ThoughtManager.Instace().SetText(recievedReason, icon);

				StartCoroutine("startAction", recievedCommand.Clone());
				recievedReason = "";
				recievedCommand = "";
			}
		}
    }


	[SerializedDictionary("key", "command")]
	[SerializeField] SerializedDictionary<KeyCode, string> dCommandStr;
}
