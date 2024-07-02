from typing import Optional, List, Tuple
from langchain_core.tools import tool
from langchain_core.prompts import ChatPromptTemplate
from langchain_groq import ChatGroq
import os
from dotenv import load_dotenv
import json

# Load environment variables
load_dotenv()
groq_api_key = os.getenv('GROQ_API_KEY')

# Check if the API key is loaded properly
if not groq_api_key:
    raise ValueError("GROQ_API_KEY environment variable is not set.")

# Initialize ChatGroq
chat = ChatGroq(
    temperature=0,
    model="llama3-70b-8192",
    api_key=groq_api_key,
    verbose=True
)

@tool
def read_book(book_name: str) -> str:
    """
    Open and read a book in JSON format.

    Parameters:
        book_name (str): Name of the book.

    Returns:
        str: Book content in JSON format.
    """
    try:
        file_path = f"app/settings/{book_name}.json"
        print(f"Trying to open file: {file_path}")  # Debugging
        with open(file_path, "r") as file:
            content = json.load(file)
        print(f"Successfully read file: {file_path}")  # Debugging
        return json.dumps(content)
    except Exception as e:
        print(f"Error reading file: {file_path}, Error: {str(e)}")  # Debugging
        return json.dumps({"error": str(e)})

# Bind tools to the model
tool_model = chat.bind_tools([read_book], tool_choice="auto")

# Prepare the input prompt for reading books
read_books_prompt = '''You are a character in a video game on a tropical island. Your goal is to survive by maintaining hunger, thirst, energy, and health levels and escape the island.

You have access to books that contain information about:

actions: the list of actions you can perform
player_info: your current stats (hunger, thirst, energy, health)
game_info: the current state of the game
inventory: the items you have
logs: the actions you have performed
objectives: the main objectives to complete
current_plan: the plan you just came up with
warnings: Critical alerts or cautions that the character should be aware of, potentially affecting decision-making or gameplay

Read all available books to gather information. Do not take any action yet, just read the books.'''

def execute_agent(input_data: str) -> Tuple[str, str]:
    try:
        print("Step 1: Reading books")
        res = tool_model.invoke(read_books_prompt)
        print(f"Tool Calls: {res.tool_calls}")
        book_contents = {}
        for tool_call in res.tool_calls:
            print(f"Tool Call ID: {tool_call['id']}, Tool Name: {tool_call['name']}, Args: {tool_call['args']}")
            if tool_call['name'] == 'read_book':
                book_name = tool_call['args']['book_name']
                book_content = read_book(book_name)
                book_contents[book_name] = json.loads(book_content)
                print(f"Book Content: {book_content}")
        
        print("\nStep 2: Making a decision")
        decision_prompt = f'''Based on the information from all the books you've read:

{json.dumps(book_contents, indent=2)}

What is your next action? Return a JSON object with "action" (the next action to take) and "observation" (a short, informative explanation for that action). The action can only be one of the actions in the actions book.

Example response:
{{
    "action": "pick_sticks",
    "observation": "You need sticks to build a shelter."
}}'''

        decision = chat.invoke(decision_prompt)
        
        print("\nFinal Decision:")
        print(decision.content)

        # Parse the JSON response
        decision_data = json.loads(decision.content)
        action = decision_data['action']
        observation = decision_data['observation']

        return action, observation

    except Exception as e:
        print(f"An error occurred: {str(e)}")
        return "error", str(e)

# Example usage
if __name__ == "__main__":
    input_data = "Start the game"  # You can modify this as needed
    action, observation = execute_agent(input_data)
    print(f"\nReturned result: ['{action}', '{observation}']")