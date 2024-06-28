import json
import os

def load_from_json(category_name, file_path):
    """
    Loads data from a JSON file for a specified category.

    Parameters:
    -----------
    category_name : str
        The name of the category to load (e.g., 'instructions', 'actions').
    file_path : str
        Path to the JSON file containing the data.

    Returns:
    --------
    list of dict
        A list of items for the specified category, where each item is represented as a dictionary.
    """
    if not os.path.exists(file_path) or os.path.getsize(file_path) == 0:
        return []

    with open(file_path, 'r') as file:
        data = json.load(file)
    
    return data.get(category_name, [])

def save_to_json(category_name, file_path, data):
    """
    Saves data to a JSON file for a specified category.

    Parameters:
    -----------
    category_name : str
        The name of the category to save (e.g., 'instructions', 'actions').
    file_path : str
        Path to the JSON file where the data will be saved.
    data : list of dict
        A list of items for the specified category, where each item is represented as a dictionary.
    """
    existing_data = {}
    if os.path.exists(file_path) and os.path.getsize(file_path) != 0:
        with open(file_path, 'r') as file:
            existing_data = json.load(file)

    existing_data[category_name] = data
    with open(file_path, 'w') as file:
        json.dump(existing_data, file, indent=2)

def load_categories_from_json(file_path):
    """
    Loads categories from a JSON file.

    Parameters:
    -----------
    file_path : str
        Path to the JSON file containing the categories.

    Returns:
    --------
    list of dict
        A list of categories, where each category is represented as a dictionary.
    """
    with open(file_path, 'r') as file:
        categories = json.load(file)
    return categories

def save_categories_to_json(file_path, categories):
    """
    Saves categories to a JSON file.

    Parameters:
    -----------
    file_path : str
        Path to the JSON file where the categories will be saved.
    categories : list of dict
        A list of categories, where each category is represented as a dictionary.
    """
    with open(file_path, 'w') as file:
        json.dump(categories, file, indent=2)
