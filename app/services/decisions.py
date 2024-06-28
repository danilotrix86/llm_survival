from app import config  # Configuration settings
from pydantic import ValidationError
import logging
from validation.pydantic_val import NextAction

# Initialize the logger
logger = logging.getLogger(__name__)



class Decision:
    """
    Represents the decision-making process for the game character using OpenAI's model.
    """

    def __init__(self, memory):
        """
        Initializes a new Decision instance.

        Parameters:
        -----------
        memory : Memory
            An instance of the Memory class.
        """
        self.memory = memory

        if config.LLM_ENGINE == "openai":
            from services.openai_class import OpenAIWrapper
            self.decision_wrapper = OpenAIWrapper(config.GPT_ENGINE)

    def get_next_action(self):
        """
        Gets the next action from the OpenAI model based on the current memory.

        Returns:
        --------
        str
            The next action as a JSON string.
        """

        if config.LLM_ENGINE == "openai":
            # Add the memory string as a system message
            self.decision_wrapper.add_message("system", self.memory)

            try:
                # Get the next action from the OpenAI model
                response = self.decision_wrapper.completion(response_format="json")
                # Extract the content from the response
                response_content = response.choices[0].message.content

                # Validate the response content with Pydantic
                action_response = NextAction.model_validate_json(response_content)

                # Return the validated JSON response
                return action_response.model_dump_json()

            except ValidationError as e:
                # Handle validation errors
                logger.error(f"Validation error: {e.json()}")
                return '{"action": "", "observation": "Validation error"}'

            except Exception as e:
                # Log the error
                logger.error(f"Error fetching next action: {e}")
                return '{"action": "", "observation": "Error fetching next action"}'

