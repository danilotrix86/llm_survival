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
    try:
        file_path = f"settings/{book_name}.json"
        with open(file_path, "r") as file:
            content = json.load(file)
        return json.dumps(content)
    except Exception as e:
        logger.error(f"Error reading book '{book_name}': {e}")
        return json.dumps({"error": str(e)})

class SurvivalGameAgent:
    def __init__(self, openai_api_key: str):
        """
        Initialize the SurvivalGameAgent with OpenAI API key and necessary configurations.

        Parameters:
            openai_api_key (str): API key for OpenAI.
        """
        self.tools = [read_book]
        self.prompt = ChatPromptTemplate.from_messages(
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
        self.llm = ChatOpenAI(model="gpt-4o", api_key=openai_api_key)
        self.agent = None
        self.agent_executor = None

    def initialize_agent(self):
        """
        Initialize the agent and agent executor with tools and prompt.
        """
        try:
            self.agent = create_tool_calling_agent(self.llm, self.tools, self.prompt)
            self.agent_executor = AgentExecutor(
                agent=self.agent, tools=self.tools, verbose=True, handle_parsing_errors=True
            )
            logger.info("Agent initialized successfully.")
        except Exception as e:
            logger.error(f"Error initializing agent: {e}")

    def execute_agent(self, input_data):
        """
        Execute the agent with given input data.

        Parameters:
            input_data (dict): Data to be processed by the agent.

        Returns:
            tuple: The next action and observation.
        """
        try:
            input_data["tools"] = self.tools  # Add tools to input data
            input_data["tool_names"] = [tool.name for tool in self.tools]  # Add tool names to input data
            output = self.agent_executor.invoke(input_data)

            logger.info(f"Output: {output}")

            # Clean up the output to remove the backticks and parse the JSON
            if 'output' in output:
                cleaned_output = output['output'].strip('```json\n').strip('\n```')
                try:
                    parsed_output = json.loads(cleaned_output)
                    action = parsed_output.get("action")
                    observation = parsed_output.get("observation")
                    return action, observation
                except json.JSONDecodeError as e:
                    logger.error(f"Failed to parse JSON: {e}")
                    logger.error(f"Output content: {cleaned_output}")
                    return None, None
            else:
                logger.error("Key 'output' not found in the output dictionary.")
                return None, None
        except Exception as e:
            logger.error(f"Error executing agent: {e}")
            return None, None
