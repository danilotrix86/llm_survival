import json
import tiktoken
from config import config

class Memory:
    """
    Represents the memory of a game character, including instructions, actions, logs, objectives, game info, and player info.
    """

    def __init__(self, instruction="", actions=None, logs=None, objectives=None, game_info=None, player_info=None):
        """
        Initializes a new Memory instance with optional parameters for instructions, actions, logs, objectives, game info, and player info.

        Parameters:
        -----------
        instruction : str, optional
            Instruction string for the character (default is an empty string).
        actions : list of dict, optional
            List of possible actions the character can perform (default is an empty list).
        logs : list of dict, optional
            List of logs of previous actions executed by the character (default is an empty list).
        objectives : list of dict, optional
            List of main game objectives (default is an empty list).
        game_info : list of dict, optional
            List of game info elements (default is an empty list).
        player_info : list of dict, optional
            List of player variables (default is an empty list).
        """
        self.memory = {
            'instruction': instruction,
            'actions': actions if actions is not None else [],
            'logs': logs if logs is not None else [],
            'objectives': objectives if objectives is not None else [],
            'game_info': game_info if game_info is not None else [],
            'player_info': player_info if player_info is not None else []
        }

    def add_item(self, category, name, description):
        """
        Adds a JSON object with the given name and description to the specified category list.

        Parameters:
        -----------
        category : str
            The category to which the item will be added (e.g., 'actions', 'logs').
        name : str
            The name of the item.
        description : str
            The description of the item.
        """
        item_json = json.dumps({"name": name, "description": description})
        self.memory[category].append(item_json)

        # Ensure logs do not exceed 10 entries
        if category == 'logs' and len(self.memory[category]) > config.LOGS_SIZE:
            self.memory[category].pop(0)


    def remove_item(self, category, name):
        """
        Removes a JSON object with the given name from the specified category list.

        Parameters:
        -----------
        category : str
            The category from which the item will be removed (e.g., 'actions', 'logs').
        name : str
            The name of the item to be removed.
        """
        self.memory[category] = [item for item in self.memory[category] if json.loads(item)['name'] != name]

    def update_item(self, category, name, new_description):
        """
        Updates the description of a JSON object with the given name in the specified category list.

        Parameters:
        -----------
        category : str
            The category in which the item will be updated (e.g., 'actions', 'logs').
        name : str
            The name of the item to be updated.
        new_description : str
            The new description of the item.
        """
        for i, item in enumerate(self.memory[category]):
            item_data = json.loads(item)
            if item_data['name'] == name:
                item_data['description'] = new_description
                self.memory[category][i] = json.dumps(item_data)
                break

    def get_memory_state(self):
        """
        Returns the current state of the memory.

        Returns:
        --------
        dict
            A dictionary representing the current state of the memory.
        """
        return self.memory

    def to_string(self):
        """
        Returns a string representation of the memory.

        Returns:
        --------
        str
            A string representation of the memory.
        """
        return json.dumps(self.memory, indent=2)

    def num_tokens(self, encoding_name: str = "cl100k_base") -> int:
        """Returns the number of tokens in a text string using the specified encoding, defaulting to UTF-8."""
        encoding = tiktoken.get_encoding(encoding_name)
        num_tokens = len(encoding.encode(self.to_string()))
        return num_tokens