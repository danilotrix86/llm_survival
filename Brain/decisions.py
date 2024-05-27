
from config import config
from pydantic import BaseModel, ValidationError


class ActionResponse(BaseModel):
    action: str
    reason: str

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

        if (config.LLM_ENGINE == "openai"):
            from openai_wrapper.openai_class import OpenAIWrapper
            self.decision_wrapper = OpenAIWrapper(config.GPT_ENGINE)

    def get_next_action(self):
        """
        Gets the next action from the OpenAI model based on the current memory.

        Returns:
        --------
        str
            The next action as a JSON string.
        """
        # Convert the entire memory to a string for the system message
        memory_string = self.memory.to_string()

        if (config.LLM_ENGINE == "openai"):
            # Add the memory string as a system message
            self.decision_wrapper.add_message("system", memory_string)

            try:
                # Get the next action from the OpenAI model
                response = self.decision_wrapper.completion(response_format="json")
                # Extract the content from the response
                response_content = response.choices[0].message.content

                # Validate the response content with Pydantic
                action_response = ActionResponse.model_validate_json(response_content)
                
                # Return the validated JSON response
                return action_response.model_dump_json()

            except ValidationError as e:
                # Handle validation errors
                print(f"Validation error: {e.json()}")
                return '{"action": "", "reason": "Validation error"}'

            except Exception as e:
                # Log the error
                print(f"Error fetching next action: {e}")
                return '{"action": "", "reason": "Error fetching next action"}'

        
