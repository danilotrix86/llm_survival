import json
from langchain_core.prompts import ChatPromptTemplate, MessagesPlaceholder
from langchain_openai import ChatOpenAI
from langchain.agents import tool, create_tool_calling_agent, AgentExecutor
import logging
from app import config
from dotenv import load_dotenv
import os

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
        file_path = f"app/settings/{book_name}.json"
        with open(file_path, "r") as file:
            content = json.load(file)
        return json.dumps(content)
    except Exception as e:
        logger.error(f"Error reading book '{book_name}': {e}")
        return json.dumps({"error": str(e)})

class SurvivalGameAgent:
    def __init__(self):
        """
        Initialize the SurvivalGameAgent with OpenAI API key and necessary configurations.

        Parameters:
            openai_api_key (str): API key for OpenAI.
        """
        self.tools = [read_book]
        self.prompt = ChatPromptTemplate.from_messages(
            [
                ("system", '''
                    
                    You’re the main character in a video game, stranded on a tropical island that’s more “survive or else” than “fun in the sun.” Your top priority? Stay alive by juggling your hunger, thirst, stress, and health levels, all while plotting your grand escape.

                    You’ve got access to some super important resources, cleverly disguised as "books":

                    actions: A menu of all the things you can do. Pick wisely—this is your life we're talking about!
                    player_info: Your vital stats, like hunger, thirst, stress, and health. Basically, how close you are to either thriving or face-planting.
                    inventory: A list of all the stuff you’ve managed to scrounge up so far.
                    logs: A running tally of everything you’ve done—whether genius or questionable.
                    objectives: The big-ticket goals you need to achieve to make it off this island.
                    Your mission? Dive into these "books" to gather all the intel you need, then decide what to do next. When you’re ready, send back a JSON object with two key ingredients:

                    "action": The next move you’re going to make, picked from the "actions" book.
                    "observation": A short, snappy reason for your choice, written from your perspective. Keep it under 30 words and make it fun—after all, humor might be your last defense against island madness!
                    Remember, your action must come from the "actions" book—no wild improvisations, no matter how desperate things get!    

                '''),
                ("human", "{input}"),
                MessagesPlaceholder(variable_name="agent_scratchpad"),
            ]
        )
        # Load environment variables from .env file
        load_dotenv()

        self.api_key = os.getenv('OPENAI_API_KEY')
        self.llm = ChatOpenAI(model=config.GPT_ENGINE, api_key=self.api_key)
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

            #logger.info(f"Output: {output}")

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
