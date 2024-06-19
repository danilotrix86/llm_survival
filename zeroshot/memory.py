import logging
import tiktoken
from config import config
from settings.utils import load_from_json, save_to_json

# Initialize the logger
logger = logging.getLogger(__name__)

class Memory:
    """
    Represents the memory of a game character, including various dynamically defined categories.
    """

    def __init__(self, categories, base_path):
        """
        Initializes a new Memory instance with dynamically defined categories.

        Parameters:
        -----------
        categories : list of dict
            A list of dictionaries where each dictionary contains:
            - 'name': The name of the category (e.g., 'instruction', 'actions', 'logs').
            - 'description': The description of the category.
            - 'content' (optional): Initial content for the category, default is an empty list.
        base_path : str
            The base path where category JSON files are located.
        """
        self.memory = {}
        self.base_path = base_path
        self.load_memory(categories)
        logger.info("Memory initialized.")

    def load_memory(self, categories):
        """
        Loads initial memory categories from a list of categories.

        Parameters:
        -----------
        categories : list of dict
            A list of dictionaries containing the initial categories and their descriptions.
        """
        for category in categories:
            name = category['name']
            description = category['description']
            content = category.get('content', [])
            self.memory[name] = {
                'content': content,
                'description': description
            }
            self._load_category_from_file(name)
        logger.info("Memory categories loaded.")

    def _load_category_from_file(self, category):
        """
        Helper method to load a category from its JSON file.

        Parameters:
        -----------
        category : str
            The name of the category to load.
        """
        category_file = f"{self.base_path}/{category}.json"
        self.memory[category]['content'] = load_from_json(category, category_file)
        logger.info(f"Loaded category '{category}' from file.")

    def _save_category_to_file(self, category):
        """
        Helper method to save a category to its JSON file.

        Parameters:
        -----------
        category : str
            The name of the category to save.
        """
        category_file = f"{self.base_path}/{category}.json"
        save_to_json(category, category_file, self.memory[category]['content'])
        logger.info(f"Saved category '{category}' to file.")

    def _modify_category(self, category, items=None, single_item=None):
        """
        Helper method to modify a category by adding items.

        Parameters:
        -----------
        category : str
            The category to modify.
        items : list of dict, optional
            A list of items to add to the category.
        single_item : dict, optional
            A single item to add to the category.
        """
        if items:
            self.memory[category]['content'].extend(items)
        elif single_item:
            self.memory[category]['content'].append(single_item)

        # Ensure logs do not exceed the configured size
        if category == 'logs' and len(self.memory[category]['content']) > config.LOGS_SIZE:
            self.memory[category]['content'] = self.memory[category]['content'][-config.LOGS_SIZE:]
        
        # Save changes to the JSON file
        self._save_category_to_file(category)
        logger.info(f"Modified category '{category}'.")

    def add_item(self, category, name=None, description=None, quantity=None, items=None):
        """
        Adds an item or multiple items to the specified category list.

        Parameters:
        -----------
        category : str
            The category to which the item(s) will be added (e.g., 'instruction', 'actions', 'logs').
        name : str, optional
            The name of the item (only used if adding a single item).
        description : str, optional
            The description of the item (only used if adding a single item).
        quantity : int, optional
            The quantity of the item (only used if adding a single item to 'inventory').
        items : list of dict, optional
            A list of items to add, where each item is a dictionary with 'name' and 'description' (and 'quantity' for 'inventory').
        """
        if items:
            self._modify_category(category, items=items)
        else:
            item = {"name": name, "description": description}
            if category == 'inventory':
                item["quantity"] = quantity
            self._modify_category(category, single_item=item)
        logger.info(f"Added item(s) to category '{category}'.")

    def remove_item(self, category, name):
        """
        Removes an item with the given name from the specified category list.

        Parameters:
        -----------
        category : str
            The category from which the item will be removed (e.g., 'instruction', 'actions', 'logs').
        name : str
            The name of the item to be removed.
        """
        self.memory[category]['content'] = [item for item in self.memory[category]['content'] if item['name'] != name]
        # Save changes to the JSON file
        self._save_category_to_file(category)
        logger.info(f"Removed item '{name}' from category '{category}'.")

    def update_item(self, category, name, new_description=None, new_quantity=None):
        """
        Updates the description of an item with the given name in the specified category list.

        Parameters:
        -----------
        category : str
            The category in which the item will be updated (e.g., 'instruction', 'actions', 'logs').
        name : str
            The name of the item to be updated.
        new_description : str, optional
            The new description of the item.
        new_quantity : int, optional
            The new quantity of the item (only used for 'inventory').
        """
        for item in self.memory[category]['content']:
            if item['name'] == name:
                if new_description:
                    item['description'] = new_description
                if category == 'inventory' and new_quantity is not None:
                    item['quantity'] = new_quantity
                break
        # Save changes to the JSON file
        self._save_category_to_file(category)
        logger.info(f"Updated item '{name}' in category '{category}'.")

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
        return '\n'.join([self.category_to_string(cat) for cat in self.memory])

    def category_to_string(self, category_name):
        """
        Returns a string representation of a specific category in the memory.

        Parameters:
        -----------
        category_name : str
            The name of the category to transform into a string.

        Returns:
        --------
        str
            A string representation of the specified category.
        """
        if category_name not in self.memory:
            return f"Category '{category_name}' not found in memory."

        data = self.memory[category_name]
        if category_name == 'inventory':
            return f"{category_name} ({data['description']}):\n" + '\n'.join(
                [f"  - {item['name']}: {item['description']} (Quantity: {item['quantity']})" for item in data['content']]
            )
        else:
            return f"{category_name} ({data['description']}):\n" + '\n'.join(
                [f"  - {item['name']}: {item['description']}" for item in data['content']]
            )

    def num_tokens(self, encoding_name="cl100k_base"):
        """
        Returns the number of tokens in a text string using the specified encoding, defaulting to UTF-8.

        Parameters:
        -----------
        encoding_name : str
            The name of the encoding to use for counting tokens.

        Returns:
        --------
        int
            The number of tokens in the text string.
        """
        encoding = tiktoken.get_encoding(encoding_name)
        return len(encoding.encode(self.to_string()))

    def update_memory(self, action_request):
        """
        Updates the memory with new content based on the ActionRequest object.

        Parameters:
        -----------
        action_request : ActionRequest
            An instance of the ActionRequest Pydantic model containing the new memory content.
        """
        try:
            # Extract individual fields from the action_request
            action = action_request.action
            status = action_request.status
            message = action_request.message
            inventory = action_request.inventory.dict()    # This should be a dictionary
            player_info = action_request.player_info.dict()  
            
            # Update the memory with the new content
            for player_info_key in player_info:
                self.update_item('player_info', player_info_key, new_description=player_info[player_info_key])

            # Update the memory with the new log
            full_log = f"The action '{action}' was executed with status '{status}' and message: '{message}'."
            self.add_item('logs', name=action, description=full_log)

            
            logger.info(f"Inventory: {inventory}")
            # Update the inventory
            for item_name, item_quantity in inventory.items():
                self.update_item('inventory', item_name, new_quantity=item_quantity)
            
            
           
        except KeyError as e:
            logger.error(f"Key error: {e}")
            raise
        except Exception as e:
            logger.error(f"Unexpected error: {e}")
            raise

