from abc import ABC, abstractmethod
import os
from dotenv import load_dotenv
import logging

class AIWrapper(ABC):
    def __init__(self, model, env_var_name):
        load_dotenv()
        self.api_key = os.getenv(env_var_name)
        self.model = model
        self.messages = []
        self._initialize_client()

    @abstractmethod
    def _initialize_client(self):
        pass

    def add_message(self, role, content):
        self.messages.append({"role": role, "content": content})

    def remove_message(self, index):
        if 0 <= index < len(self.messages):
            del self.messages[index]
        else:
            raise IndexError("Message index out of range")

    def clear_messages(self):
        self.messages = []

    def get_messages(self):
        return self.messages

    def completion(self, response_format="text", **kwargs):
        api_params = {
            "model": self.model,
            "messages": self.messages,
            **kwargs
        }

        logging.info(f"Calling completion with params: {api_params}")
        
        if response_format == "json":
            api_params["response_format"] = {"type": "json_object"}

        return self._create_completion(api_params)

    @abstractmethod
    def _create_completion(self, api_params):
        pass

class OpenAIWrapper(AIWrapper):
    def __init__(self, model):
        super().__init__(model, 'OPENAI_API_KEY')

    def _initialize_client(self):
        import openai
        openai.api_key = self.api_key
        try:
            self.client = openai.OpenAI(api_key=self.api_key)
            logging.info("OpenAI client initialized successfully")
        except Exception as e:
            logging.error(f"Error initializing OpenAI client: {e}")

    def _create_completion(self, api_params):
        return self.client.chat.completions.create(**api_params)

class GroqWrapper(AIWrapper):
    def __init__(self, model):
        super().__init__(model, 'GROQ_API_KEY')

    def _initialize_client(self):
        from groq import Groq
        try:
            self.client = Groq(api_key=self.api_key)
            logging.info("Groq client initialized successfully")
        except Exception as e:
            logging.error(f"Error initializing Groq client: {e}")

    def _create_completion(self, api_params):
        return self.client.chat.completions.create(**api_params)