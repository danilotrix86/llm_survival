using System.Collections;
using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using UnityEngine;

using SurvivalEngine;
using UnityEditor.Experimental.GraphView;
using System.Runtime.Serialization;
using System.Linq;

public class LLMManager : MonoBehaviour
{
	[SerializeField] LLMCharacter character;

	[SerializedDictionary("id", "data")]
	[SerializeField] SerializedDictionary<string, ConstructionData> buildables;

	[SerializedDictionary("id", "data")]
	[SerializeField] SerializedDictionary<string, GroupData> interactableGroups;

	[SerializedDictionary("id", "data")]
	[SerializeField] SerializedDictionary<string, GroupData> attackableGroups;

	[SerializedDictionary("id", "data")]
	[SerializeField] SerializedDictionary<string, ItemData> collectibleItems;


	[SerializedDictionary("command", "substitution")]
	[SerializeField] SerializedDictionary<string, string> substitutions;

	public LLMCharacter GetCharacter() { return character; }

	LLMInterface llminterface;

	public string ProcessCommand(string command){
		command = getSubstitution(command);
		Debug.Log($"Running: {command}");

		string[] tokens = command.Split(LLMStrings.DELIM);


		return processCommand(tokens);
	}

	string processCommand(string[] tokens){
		llminterface.StartCommand(tokens);
		// Exact matches
		if(tokens[0] == "interact"){
			interact(tokens);
		}else if(tokens[0] == "pick"){
			collect(tokens);
		}else if(tokens[0] == "attack"){
			attack(tokens);
		}else if(tokens[0] == "craft"){
			craft(tokens);
		}else if(tokens[0] == "build"){
			build(tokens);
		}else if(tokens[0] == "explore"){
			explore(tokens);
		}else if(tokens[0] == "eat"){
			eat();
		}else{
			llminterface.ErrorCommand("CommandNotFound");
		}

		return "";
	}

	// interact_{object}
	// interacts with object
	// args
	//	0 - object title
	void interact(string[] tokens){
		GroupData associatedData;

		if(interactableGroups.TryGetValue(tokens[1], out associatedData)){

			llminterface.AddArgument(associatedData.title == "" ? associatedData.name : associatedData.title);
			character.InteractGroup(associatedData);
		}else{
			llminterface.ErrorCommand("Invalid");
		}
	}

	// collect_{item}
	// picks up object
	// args
	// 	0 - item title
	void collect(string[] tokens){
		ItemData associatedData;

		if(collectibleItems.TryGetValue(tokens[1], out associatedData)){

			llminterface.AddArgument(associatedData.title);
			character.PickItem(associatedData);
		}else{
			llminterface.ErrorCommand("Invalid");
		}
	}

	// attack_{target}
	// attacks a target
	// args
	//	0 - target title
	//  1 - required item title
	void attack(string[] tokens){
		GroupData associatedData;

		if(attackableGroups.TryGetValue(tokens[1], out associatedData)){
			Selectable nearest = Selectable.GetNearestKnownGroup(associatedData, character.transform.position);
			Destructible destruct = nearest ? nearest.GetComponent<Destructible>() : null;


			llminterface.AddArgument(associatedData.title == ""  ? (associatedData.group_id == "" ? associatedData.name : associatedData.group_id) : associatedData.title);
			llminterface.AddArgument(destruct? destruct.required_item.name : null);

			character.Attack(destruct);
		}else{
			llminterface.ErrorCommand("Invalid");
		}
	}

	// craft_{item}
	// crafts an item
	// args
	//	0 - item title
	//  1 - missing string
	void craft(string[] tokens){
		CraftData craftableItem = CraftData.GetByName(tokens[1]);
		if(!(craftableItem is ItemData)){
			craftableItem = null;
		}

		if(craftableItem){

			llminterface.AddArgument(craftableItem.title);
			character.Craft((ItemData)craftableItem);
		}else{
			llminterface.ErrorCommand("Invalid");
		}
	}

	// build_{object}
	// builds an object
	// args
	//  0 - building title
	//  1 - missing items
	void build(string[] tokens){
		ConstructionData associatedData;

		if(buildables.TryGetValue(tokens[1], out associatedData)){
			llminterface.AddArgument(associatedData.title);
			character.build(associatedData);
		}else{
			llminterface.ErrorCommand("Invalid");
		}
	}


	// build_{object}
	// builds an object
	// args
	//  0 - explored region name
	//  1 - Description
	void explore(string[] tokens){
		character.explore();
	}

	// eat
	// eatts
	// args
	//   1 - The food that was eaten
	void eat(){
		character.eat();
	}

	string getSubstitution(string input){
		string substitution;
		if(substitutions.TryGetValue(input, out substitution)){
			return substitution;
		}

		return input;
	}

    void Start()
    {
		llminterface = LLMInterface.Instance();
		// The intial command is explore to get the LLM oriented
		// This also initializes the first explored zone of the area
		llminterface.AcceptCommand("explore");
    }

    // Update is called once per frame
    void Update()
    {
    }
}
