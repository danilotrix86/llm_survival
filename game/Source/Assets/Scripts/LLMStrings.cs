using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SurvivalEngine;
using System.Linq;
using Unity.VisualScripting.Antlr3.Runtime;
using System.Net.Http;
using Newtonsoft.Json;

public static class LLMStrings
{
	public static readonly string API_MESSAGES    = "http://127.0.0.1:8000/messages/";
	public static readonly string API_XP    = "http://127.0.0.1:8000/xp/";
	public static readonly string API_NEXT_ACTION = "http://127.0.0.1:8000/next_action/";
	public static readonly string API_START = "http://127.0.0.1:8000/start_new_game/";

	public static readonly string DELIM = "_";

	// JSON Formatting
	public static readonly string[] ITEMS_TO_TRACK = {"axe", "fibers", "stone", "wood", "stick", "berry", "fish", "rope", "fishrod", "iron", "gold", "compass", "pickaxe", "torch", "sail"};

	public static readonly string[] BUILDINGS_TO_TRACK = {"firepit", "shelter"};

	public static readonly float[] thresholds = {0.3f, 0.5f, 0.65f, 0.8f, 0.9f, 1.0f};
	public static readonly string[] STATUS = {"Critical", "Very Low", "Low", "Normal", "Good", "Very Good"};
	public static readonly AttributeType[] ATTR_TO_TRACK = {AttributeType.Health, AttributeType.Hunger, AttributeType.Thirst, AttributeType.Stress};
	private static Dictionary<string, string> MESSAGES;
	private static Dictionary<string, int> XP_VALUES;

	// Commands
	public static string GetMessage(string command, string  msgID, List<object> args){
		string[] tokens = command.Split(DELIM);

		string message = CodePrefixMatch(MESSAGES, FormatMessageCode(tokens, msgID));

		if(message == null) return "";
		
		if(args.Count == 1){
			return string.Format(message, args[0]);
		}
		return string.Format(message, args.ToArray());
	}

	public static int GetXp(string command, string msgID){
		string[] tokens = command.Split(DELIM);

		int xp = CodePrefixMatch(XP_VALUES, FormatMessageCode(tokens, msgID));
		return xp;
	}

	public static void Setup(){
		LoadMessages();
		LoadXP();
	}

	private async static void LoadXP(){
		XP_VALUES = new Dictionary<string, int>();

		XP_VALUES["Success"] = 3;

		var client = new HttpClient();

		HttpResponseMessage response = await client.GetAsync(API_XP);
		string json = await response.Content.ReadAsStringAsync();

		try{
			Dictionary<string, int> messages = JsonConvert.DeserializeObject<Dictionary<string, int>>(json);
			
			foreach(var kv in messages){
				XP_VALUES[kv.Key] = kv.Value;
			}

			Debug.Log("Loaded XP values");
		}catch(Newtonsoft.Json.JsonSerializationException e){
			Debug.LogWarning($"WARNING! Could not parse XP values from server\n{json}");
		}catch(JsonReaderException e){
			Debug.LogWarning($"WARNING! Could not parse XP values from server\n{json}");
		}
	}

	private async static void LoadMessages(){
		MESSAGES = new Dictionary<string, string>();
		MESSAGES["CommandNotFound"] = "That was not a valid command.";
		MESSAGES["Invalid"]         = "The target of the command was invalid.";

		MESSAGES["ErrorNoObject"] =   "No {0} found";
		MESSAGES["ErrorTool"]     =   "You require a {1} for that";
		MESSAGES["wood.cut.ErrorTool"] = "You require an axe for that";
		MESSAGES["mine.ErrorTool"] = "You require a pickaxe for that";
		MESSAGES["mine.Success"] = "You have mined some metal";
		MESSAGES["ErrorCantAttack"] = "That object cannot be attacked";
		MESSAGES["craft.ErrorMissingItems"] = "Cannot craft {0}, missing: {1}";

		MESSAGES["wood.cut.Success"] = "Cut down tree";
		MESSAGES["interact.Success"] = "Interacted with {0}";
		MESSAGES["pick.Success"]   = "Picked up {0}";
		MESSAGES["craft.Success"] = "Crafted {0}";
		MESSAGES["explore.Success"] = "Explored {0}. There is {1} around you";
		MESSAGES["eat.Success"] = "Ate {0}";

		var startclient = new HttpClient();
		startclient.PostAsync(API_START, null);

		var client = new HttpClient();

		HttpResponseMessage response = await client.GetAsync(API_MESSAGES);
		string json = await response.Content.ReadAsStringAsync();

		try{
			Dictionary<string, string> messages = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
			
			foreach(var kv in messages){
				MESSAGES[kv.Key] = kv.Value;
			}

			Debug.Log("Loaded messages");
		}catch(Newtonsoft.Json.JsonSerializationException e){
			Debug.LogWarning($"WARNING! Could not parse messages from server\n{json}");
		}

	}

	// Finds a code inside the messages dictionary using the prefix match algorithm
	// For example, if there are two entries Success.Grass.Cut and Grass.Cut, 
	// the success message will go to the first, and all other messages coming 
	// from cut grass will use Grass.Cut
	private static T CodePrefixMatch<T>(Dictionary<string, T> dict, string code){
		for(int i = 0; i != -1; i = code.IndexOf(".")){
			if(i != 0) code = code.Substring(i+1);
			T val;
			if(dict.TryGetValue(code, out val)) return val;
		}

		return default(T);
	}

	// Format a command into a code that can be used to identify messages
	// The code is to allow for a longest prefix match to find the appropriate code
	// For example, if the "cut_grass" command returns ErrorNoAction
	// An error of ErrorNoAction.Grass.Cut will be thrown
	private static string FormatMessageCode(string[] tokens, string msgID){
		string code = string.Join(".", tokens.Reverse()) + "." +    msgID;
		return code;
	}

	public static string GetStatusLabel(float value, float maxValue){
		float percent = value/(maxValue+1);

		float last = 0.0f;
		for(int i = 0; i < thresholds.Length; i++){
			if(last < percent &&  percent <= thresholds[i]) return STATUS[i];
			last = thresholds[i];
		}

		return STATUS[STATUS.Length - 1];
	}
}
