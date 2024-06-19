import json
from langchain_core.prompts import ChatPromptTemplate, MessagesPlaceholder
from langchain_openai import ChatOpenAI
from langchain.agents import tool, create_tool_calling_agent, AgentExecutor
import logging

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

@tool
def read_book(book_name: str) -> str:
    """
    Open and read a book in JSON format.

    Parameters:
        book_name (str): Name of the book.
        
    Returns:
        str: Book content in JSON format.
    """
    file_path = f"settings/{book_name}.json"
    with open(file_path, "r") as file:
        content = json.load(file)
    return json.dumps(content)

tools = [read_book]

prompt = ChatPromptTemplate.from_messages(
    [
        ("system", '''
            You are a character in a video game on a tropical island. Your goal is to survive by maintaining hunger, thirst, energy, and health levels and escape the island.

            You have access to books that contain information about:

            - actions: the list of actions you can perform
            - player_info: your current stats (hunger, thirst, energy, health)
            - game_info: the current state of the game
            - inventory: the items you have
            - logs: the actions you have performed
            - objectives: the main objectives to complete
            - current_plan: the plan you just came up with

            Explore all available books to gather information before deciding your next move. 
            Return a JSON object with "action" (the next action to take) and "observation" (a short, informative explanation for that action). The action can only be one of the actions in the actions book.
        '''),
        ("human", "{input}"),
        MessagesPlaceholder(variable_name="agent_scratchpad"),
    ]
)

# Choose the LLM that will drive the agent
llm = ChatOpenAI(model="gpt-4o")

# Construct the Tools agent
agent = create_tool_calling_agent(llm, tools, prompt)

# Create an agent executor by passing in the agent and tools
agent_executor = AgentExecutor(
    agent=agent, tools=tools, verbose=True, handle_parsing_errors=True
)

def run_agent(input_data):
    input_data["tools"] = tools  # Add tools to input data
    input_data["tool_names"] = [tool.name for tool in tools]  # Add tool names to input data
    return agent_executor.invoke(input_data)

input_data = {
    "input": "Return only a JSON object with the next action and observation (no other information added). The action should be only 1 of the actions in the actions book. You can't use other actions",
    "chat_history": []
}

output = run_agent(input_data)

# Print the output to understand its structure
print(output)

# Clean up the output to remove the backticks and parse the JSON
if 'output' in output:
    cleaned_output = output['output'].strip('```json\n').strip('\n```')
    try:
        parsed_output = json.loads(cleaned_output)
        # Extract action and observation
        action = parsed_output.get("action")
        observation = parsed_output.get("observation")
        print(f"Action: {action}")
        print(f"Observation: {observation}")
    except json.JSONDecodeError as e:
        print(f"Failed to parse JSON: {e}")
        print(f"Output content: {cleaned_output}")
else:
    print("Key 'output' not found in the output dictionary.")
