using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SurvivalEngine;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using UnityEditor.PackageManager;
using SurvivalEngine.WorldGen;
using Unity.VisualScripting;

public class LLMCharacter : MonoBehaviour
{
	PlayerCharacter character;
	PlayerCharacterCraft crafter;
	PlayerCharacterCombat attacker;

	LLMInterface llminterface;

    void Awake()
    {
        character = gameObject.GetComponent<PlayerCharacter>();
		crafter   = gameObject.GetComponent<PlayerCharacterCraft>();
		attacker  = gameObject.GetComponent<PlayerCharacterCombat>();
    }

	void Start(){
		llminterface = LLMInterface.Instance();
	}

    void Update()
    {
        
    }

	public void InteractGroup(GroupData group){
		Selectable nearest = Selectable.GetNearestKnownGroup(group, transform.position);
		if(nearest == null){
			llminterface.ErrorCommand("ErrorNoObject");
			return;
		}

		Plant nearestPlant = nearest.GetComponent<Plant>();
		if(nearestPlant){
			if(!nearestPlant.HasFruit()){
				llminterface.ErrorCommand("ErrorHarvestUnready");
				return;
			}
		}

		character.Interact(nearest);

		//llminterface.EmitStatus("Walking to " + group.title); // TODO general case
	}

	public void Attack(Destructible destruct){
		if(destruct == null){
			llminterface.ErrorCommand("ErrorNoObject");
			return;
		}

		if(!attacker.HasRequiredTool(destruct)){
			llminterface.ErrorCommand("ErrorTool");
			return;
		}
		if(!attacker.CanAttack(destruct)){
			llminterface.ErrorCommand("ErrorCantAttack"); 
			return;
		}

		character.AttackMove(destruct);

		//llminterface.EmitStatus("Attacking " + group.title);
	}

	public void PickItem(ItemData item){
		Item nearest = Item.GetNearestKnownData(item, transform.position);

		if(nearest == null){
			llminterface.ErrorCommand("ErrorNoObject");
			return;
		}

		character.Interact(nearest.GetComponent<Selectable>());

		//llminterface.EmitStatus("Walking to " + item.title);
	}

	public void Craft(ItemData item){
		if(!crafter.CanCraft(item)){
			string missingString = getCraftMissingString(item);
			llminterface.AddArgument(missingString);
			llminterface.ErrorCommand("ErrorMissingItems");
			return;
		}

		crafter.StartCrafting(item);

		llminterface.StopCommand();
	}

	private bool findBuildPos(Vector3 closestPos){
		if(float.IsInfinity(closestPos.x)) return false;
		// Try to build
		float end = 10;
		float incr = 0.5f;
		for(float x = 2; x < end; x += incr){
			for(float y = 2; y < end; y += incr){
				Vector3 buildPos = closestPos + new Vector3(x, 0, y);
				if(crafter.TryBuildAt(buildPos)) return true;

				buildPos = closestPos + new Vector3(-x, 0, -y);
				if(crafter.TryBuildAt(buildPos)) return true;
			}
		}

		return false;
	}

	private Vector3 findClosestShelter(){
		GameObject[] objects = GameObject.FindGameObjectsWithTag("Shelter");
		if(objects.Length == 0) return Vector3.positiveInfinity;

		Vector3 closest = objects[0].transform.position;
		foreach(GameObject go in objects){
			if(Vector3.Distance(closest, transform.position) > Vector3.Distance(go.transform.position, transform.position)){
				closest = go.transform.position;
			}
		}

		return closest;
	}

	public void build(ConstructionData construction){
		if(!crafter.CanCraft(construction)){
			string missingString = getCraftMissingString(construction);
			llminterface.AddArgument(missingString);
			llminterface.ErrorCommand("ErrorMissingItems");
			return;
		}

		Buildable building = crafter.CraftConstructionBuildMode(construction);
		building.onBuild += ()=> LLMInterface.Instance().StopCommand();

		if(construction.id == "raft"){
			SortedDictionary<float, Vector3> positions = new SortedDictionary<float, Vector3>();
			foreach(GameObject obj in GameObject.FindGameObjectsWithTag("ExitZone")){
				float dist = Vector3.Distance(transform.position, obj.transform.position);

				positions.Add(dist, obj.transform.position);
			}

			foreach(Vector3 pos in positions.Values){
				if(crafter.TryBuildAt(pos)) return;
			}
			llminterface.ErrorCommand("ErrorNoLocation");
			crafter.CancelBuilding();
			return;
		}

		// Find nearest shelter to build at
		if(construction.title != "shelter" && findBuildPos(findClosestShelter())) return;
		else if(findBuildPos(transform.position)) return;

		llminterface.AddArgument("");
		llminterface.ErrorCommand("ErrorNoLocation");
		crafter.CancelBuilding();
	}

	public void explore(){
		llminterface = LLMInterface.Instance();

		LLMBiome nearestUndiscovered = LLMBiome.getNearestCell(transform.position, true);

		if(nearestUndiscovered == null) {
			llminterface.ErrorCommand("ErrorNoLocation");
			return;
		}

		Vector3 explorePos = nearestUndiscovered.transform.position;
		BiomeData info = nearestUndiscovered.GetComponent<BiomeZone>().data;
		
		llminterface.AddArgument(info.name);
		llminterface.AddArgument(info.desc);

		character.MoveTo(explorePos);
		nearestUndiscovered.discovered = true;
	}

	public void eat(){
		ItemData highestItem = null;
		int highestSlot = -1;
		foreach(KeyValuePair<int, InventoryItemData> pair in character.Inventory.InventoryData.items){
			ItemData item = pair.Value.GetItem();
			if(item.type == ItemType.Consumable){
				if(!highestItem || highestItem.eat_hunger < item.eat_hunger){
					highestItem = item;
					highestSlot = pair.Key;
				}
			}
		}

		if(highestItem){
			character.Inventory.EatItem(highestSlot);
			llminterface.AddArgument(highestItem.title);
			llminterface.StopCommand();
		}else{
			llminterface.AddArgument("food");
			llminterface.ErrorCommand("ErrorNoObject");
		}
	}

	private string getCraftMissingString(CraftData craft){
		Dictionary<ItemData, int> missing = crafter.GetCraftMissing(craft);
		string missingString = "";
		foreach(KeyValuePair<ItemData, int> pair in missing){
			string itemName = pair.Key.title + (pair.Value > 1 ? "s" : "");
			missingString += $"{pair.Value} {itemName}, ";
		}
		return missingString;
	}

	// Allow implicit conversions to Survival Engine character
	public static implicit operator PlayerCharacter(LLMCharacter c) => c.character;
}
