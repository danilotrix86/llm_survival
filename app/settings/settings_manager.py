import json
from pathlib import Path
from typing import Dict, Any
import config
import tiktoken
import logging
from app.validation.pydantic_val import ActionRequest

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

class SettingsManager:
    def __init__(self, settings_dir: str):
        """
        Initialize the SettingsManager with a directory containing settings files.
        
        Args:
            settings_dir (str): Directory where settings JSON files are stored.
        """
        self.settings_dir = Path(settings_dir)
        self.memory = self._load_json("memory.json")
        self.records = {
            record["name"]: {
                "description": record["description"],
                "data": self._load_json(f"{record['name']}.json")
            }
            for record in self.memory
        }

    def _load_json(self, file_name: str) -> Dict[str, Any]:
        """
        Load JSON data from a file.
        
        Args:
            file_name (str): Name of the JSON file to load.
        
        Returns:
            Dict[str, Any]: Data loaded from the JSON file.
        """
        file_path = self.settings_dir / file_name
        try:
            if file_path.exists():
                with open(file_path, 'r') as file:
                    return json.load(file)
            return {}
        except json.JSONDecodeError:
            raise ValueError(f"Error decoding JSON from file: {file_name}")
        except Exception as e:
            raise RuntimeError(f"An error occurred while loading file {file_name}: {e}")

    def _save_json(self, file_name: str, data: Dict[str, Any]):
        """
        Save JSON data to a file.
        
        Args:
            file_name (str): Name of the JSON file to save.
            data (Dict[str, Any]): Data to save to the JSON file.
        """
        file_path = self.settings_dir / file_name
        try:
            with open(file_path, 'w') as file:
                json.dump(data, file, indent=4)
        except Exception as e:
            raise RuntimeError(f"An error occurred while saving file {file_name}: {e}")

    def load_record(self, record: str) -> Dict[str, Any]:
        """
        Load data for a specific record.
        
        Args:
            record (str): Name of the record to load.
        
        Returns:
            Dict[str, Any]: Data of the specified record.
        """
        return self.records.get(record, {}).get("data", {})

    def save_record(self, record: str):
        """
        Save the data of a specific record to its corresponding JSON file.
        
        Args:
            record (str): Name of the record to save.
        """
        if record in self.records:
            self._save_json(f"{record}.json", self.records[record]["data"])

    def add_item(self, record: str, item: Dict[str, Any]):
        """
        Add an item to a specific record.
        
        Args:
            record (str): Name of the record to add the item to.
            item (Dict[str, Any]): Item to add to the record.
        """
        if record in self.records:
            if record == "logs":
                if len(self.records[record]["data"][record]) >= config.LOGS_SIZE:
                    self.records[record]["data"][record].pop(0)
            self.records[record]["data"][record].append(item)
            self.save_record(record)
        else:
            raise ValueError(f"Record {record} not found.")
        
    def edit_item(self, record: str, item_name: str, new_item: Dict[str, Any]):
        """
        Edit an existing item in a specific record.
        
        Args:
            record (str): Name of the record containing the item.
            item_name (str): Name of the item to edit.
            new_item (Dict[str, Any]): New data for the item.
        """
        if record in self.records:
            items = self.records[record]["data"].get(record, [])
            for i, item in enumerate(items):
                if item["name"] == item_name:
                    items[i] = new_item
                    self.save_record(record)
                    return
            raise ValueError(f"Item with name {item_name} not found in record {record}.")
        else:
            raise ValueError(f"Record {record} not found.")

    def remove_item(self, record: str, item_name: str):
        """
        Remove an item from a specific record.
        
        Args:
            record (str): Name of the record to remove the item from.
            item_name (str): Name of the item to remove.
        """
        if record in self.records:
            self.records[record]["data"][record] = [
                item for item in self.records[record]["data"].get(record, [])
                if item["name"] != item_name
            ]
            self.save_record(record)
        else:
            raise ValueError(f"Record {record} not found.")

    def reset_record(self, record: str):
        """
        Reset a specific record by clearing its data.
        
        Args:
            record (str): Name of the record to reset.
        """
        if record in self.records:
            self.records[record]["data"][record] = []
            self.save_record(record)
        else:
            raise ValueError(f"Record {record} not found.")

    def record_to_string(self, record: str) -> str:
        """
        Get a string representation of a specific record.
        
        Args:
            record (str): Name of the record to represent as a string.
        
        Returns:
            str: String representation of the record.
        """
        if record in self.records:
            record_data = self.records[record]["data"]
            record_description = self.records[record]["description"]
            result = f"{record.capitalize()} ({record_description}):\n"
            items = record_data.get(record, [])
            for item in items:
                result += f"{item['name']}: {item['description']}\n"
            return result.strip()
        return "{}"

    def all_records_to_string(self) -> str:
        """
        Get a string representation of all records.
        
        Returns:
            str: String representation of all records.
        """
        result = ""
        for record_name, record_content in self.records.items():
            record_description = record_content["description"]
            record_data = record_content["data"]
            result += f"{record_name.capitalize()} ({record_description}):\n"
            items = record_data.get(record_name, [])
            for item in items:
                result += f"{item['name']}: {item['description']}\n"
            result += "\n"
        return result.strip()


    def num_tokens(self, string, encoding_name="cl100k_base"):
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
        return len(encoding.encode(string))
    

    def update_memory(self, action_request: ActionRequest):
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
            inventory = action_request.inventory.model_dump()
            player_info = action_request.player_info.model_dump()
            
            # Update player_info
            for player_info_key, value in player_info.items():
                self.edit_item('player_info', player_info_key, {"name": player_info_key, "description": str(value)})

            # Add new log
            full_log = f"The action '{action}' was executed with status '{status}' and message: '{message}'."
            self.add_item('logs', {"name": action, "description": full_log})

            logger.info(f"Inventory: {inventory}")
            # Update inventory
            for item_name, item_quantity in inventory.items():
                self.edit_item('inventory', item_name, {"name": item_name, "description": str(item_quantity)})

        except AttributeError as e:
            logger.error(f"Attribute error: {e}")
            raise
        except Exception as e:
            logger.error(f"Unexpected error: {e}")
            raise