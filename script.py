from Brain.memory import Memory
from Brain.decisions import Decision
import prompts


instructions = prompts.instructions



memory = Memory(instruction=instructions)

# Add player info
memory.add_item('player_info', 'health', 'Good')
memory.add_item('player_info', 'hunger', 'Normal')
memory.add_item('player_info', 'thirst', 'Normal')


memory.add_item('actions', 'pick sticks', 'Pick up sticks to build items.')
memory.add_item('actions', 'pick stone', 'Pick up stones to build items.')
memory.add_item('actions', 'cut wood', 'Cut wood to build items.')
memory.add_item('actions', 'eat', 'Eat to restore health.')
memory.add_item('actions', 'drink', 'Drink to restore thirst.')
memory.add_item('actions', 'sleep', 'Sleep to restore energy.')
memory.add_item('actions', 'collect', 'Collect items to use later.')
memory.add_item('actions', 'craft', 'Craft tools to survive.')


memory.add_item('logs', 'cut wood', 'at 13:34 You cut wood to build items.')
memory.add_item('logs', 'eat', 'at 14:14 You ate to restore health.')
memory.add_item('logs', 'drink', 'at 14:54 You drank to restore thirst.')
memory.add_item('logs', 'sleep', 'at 15:34 You slept to restore energy.')

memory.add_item('game_info', 'stones', '0')
memory.add_item('game_info', 'sticks', '0')


memory.add_item('objectives', 'Craft a ax', 'Craft an ax to cut wood faster. You need 2 sticks and 1 stone.')


# Print the updated memory state
print(memory.to_string())

print (memory.num_tokens())



# Create an instance of Decision
decision = Decision(memory)
print ("##############################################")
print (decision.get_next_action())
print ("##############################################")

memory.add_item('logs', 'pick sticks', 'at 16:14 You picked 1 stick')
memory.update_item('game_info', 'sticks', '1')

print(memory.to_string())
print ("##############################################")
print (decision.get_next_action())
print ("##############################################")


memory.add_item('logs', 'pick sticks', 'at 16:44 You picked 1 stick')
memory.update_item('game_info', 'sticks', '2')
memory.update_item('player_info', 'hunger', 'Low')

print(memory.to_string())
print ("##############################################")
print (decision.get_next_action())
print ("##############################################")


memory.add_item('logs', 'pick sticks', 'at 16:54 You picked 1 stone')
memory.update_item('game_info', 'sticks', '2')
memory.update_item('game_info', 'stones', '1')
memory.update_item('player_info', 'hunger', 'Very Low')

print(memory.to_string())
print ("##############################################")
print (decision.get_next_action())
print ("##############################################")
