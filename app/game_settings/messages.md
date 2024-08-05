
The messages.json work with a longest prefix match. What this means, is that the command will try to match in the json, the key with the longest prefix. For example, say we have a json



{
"grass.pick.success": "You have cut grass",
"pick.success": "You have picked {0}"
}



If you were to pick grass with the command "pick_grass", and it was successful, the program will look for "grass.pick.success", in this case, there is a message for grass.pick.success, so that message will be used.



If you were to pick up a rock with the command "pick_rock", the program will look for "rock.pick.success", since no such name exists, it will remove the first section, and look again, so now it will look for "pick.success", which has a key. It does this until something is found or there is nothing left. You can use {} to specify arguments, which depend on the command and are passed by the game.



This allows you to not have to worry about every individual message, you can write broad messages for all of a certain error types, while specifying the messages you want in more detail. For example, you could just have a message "ErrorTool": "You require a {1} for that", and it would work for every command.



Below I have listed every command type and their possible errors, with their arguments.



==============================================
Generic errors
==============================================
CommandNotFound - Thrown when the command is in no way a valid command



==============================================
fish, sleep, drink, pick_berries
==============================================
Arg 0, the name of the interacted group



Success
ErrorNoObject - The object does not exist (there is nowhere to drink, fish, sleep, or pick berries from)
ErrorHarvestUnready - The object is not ready for harvest and is still growing (for plants, currently only berries)
ErrorNoTool - The player lacks the tool for that



==============================================
Attackables: cut_wood, mine_{}
==============================================
Arg 0 - The name of the attacked group
Arg 1 - The tool required (useful to identify what is needed in case of errors)
Success
ErrorNoObject - The object does not exist
ErrorTool - The player does not have a valid tool
ErrorCantAttack - This is a pretty rare case, it means for some unknown reason, the object cannot be attacked



==============================================
Item pickups: pick_fibres, pick_sticks, pick_stones, pick_sail
==============================================
Arg 0 - The items name



Success
Invalid - The chosen target is invalid (ex. pick_dirt, since dirt is not an item)
ErrorNoObject - The object does not exist



=============================================
Crafting: craft_{}
=============================================
Arg 0 - The items name
Arg 1 - The missing items



Success
Invalid - The chosen target is invalid (ex craft_rock, since rocks can't be crafted)
ErrorMissingItems - The player does not have all the items needed



============================================
Building: build_{}
===========================================
Arg 0 - The buildings name
Arg 1 - The missing items



Success
Invalid - The chosen target is invalid
ErrorMissingItems - The player does not have all the items needed
ErrorNoLocation - The player has nowhere nearby they can build



===========================================
Explore: explore_{}
===========================================
Arg 0 - The name of the explored regions
Arg 1 - The description of the explored region



Success
ErrorNoObject - There is nowhere else to explore