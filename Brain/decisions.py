from Brain.openai_wrapper.openai_class import OpenAIWrapper
from config import config
import json

class Decision:
    def __init__(self, memory):
        """
        Initializes a new Decision instance.

        Parameters:
        -----------
        memory : Memory
            An instance of the Memory class.
        """
        self.memory = memory
        self.openai_wrapper = OpenAIWrapper(config.GPT_ENGINE)

    def get_next_action(self):

        print ("calculating next action...")

        """
        Gets the next action from the OpenAI model based on the current memory.

        Returns:
        --------
        str
            The next action as a JSON string.
        """
        # Convert the entire memory to a string for the system message
        memory_string = self.memory.to_string()

        # Convert only the objectives to a string for the user message
        objectives_string = json.dumps(self.memory.memory['objectives'], indent=2)
        print ("objectives_string", objectives_string)

        # Add the memory string as a system message
        self.openai_wrapper.add_message("system", memory_string)

        # Add the objectives string as a user message
        self.openai_wrapper.add_message("user", objectives_string)

        # Get the next action from the OpenAI model
        response = self.openai_wrapper.completion(response_format="json")
        
        # Extract and return the content from the response
        return response.choices[0].message.content

    
