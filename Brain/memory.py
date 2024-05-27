import tiktoken
from config import config
from settings.utils import load_from_json, save_to_json

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

    def add_item(self, category, name=None, description=None, items=None):
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
        items : list of dict, optional
            A list of items to add, where each item is a dictionary with 'name' and 'description' keys.
        """
        if items:
            self._modify_category(category, items=items)
        else:
            item = {"name": name, "description": description}
            self._modify_category(category, single_item=item)

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

    def update_item(self, category, name, new_description):
        """
        Updates the description of an item with the given name in the specified category list.

        Parameters:
        -----------
        category : str
            The category in which the item will be updated (e.g., 'instruction', 'actions', 'logs').
        name : str
            The name of the item to be updated.
        new_description : str
            The new description of the item.
        """
        for item in self.memory[category]['content']:
            if item['name'] == name:
                item['description'] = new_description
                break
        # Save changes to the JSON file
        self._save_category_to_file(category)

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
