from Brain.memory import Memory
from settings.utils import load_categories_from_json
from Brain.decisions import Decision
from time import sleep

# Main function
def main():
    # Load categories file path
    categories_file = 'Brain/memory.json'
    
    # Initialize memory with categories
    memory = Memory(load_categories_from_json(categories_file), 'settings')

    decision = Decision(memory)
    
    action = decision.get_next_action()
    print (decision.decision_wrapper.messages)
    print("Action: ", action)


# If main
if __name__ == "__main__":
    main()
