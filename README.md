# AI Project - Survival Game

This project showcases an advanced AI-driven survival game where the AI agent is powered by large language models (LLMs). The system allows the AI to navigate complex survival scenarios dynamically, adapting its behavior based on its environment and experiences.

## Abstract

This thesis introduces a novel approach to creating an intelligent AI agent capable of surviving on a remote island by using large language models (LLMs). The AI agent autonomously manages resources, plans actions, and adapts strategies in a dynamic environment. This approach combines elements of game design, artificial intelligence, and human-computer interaction to deliver a more immersive and adaptable gameplay experience.

## Configuration

### Approaches

You can choose between two approaches for the AI's decision-making process:

- **`ZEROSHOT`**: This approach leverages predefined responses and actions based on the AI's memory of past experiences. It relies on the AI's ability to understand context and provide responses without needing extensive retraining.

- **`AGENTIC`**: This approach uses an agent-based system to generate responses dynamically based on the current state and environment. It emphasizes real-time adaptation and strategy formulation, allowing the AI to make decisions that are more context-aware.

### LLM Engines

The AI's behavior can be powered by different LLM engines. The choice of LLM engine impacts the cost and performance of the AI:

- **OpenAI**: Utilizes OpenAI’s models. This requires setting `LLM_ENGINE` to `"openai"` and specifying a model from the available options such as `gpt-4o`, `gpt-4o-mini`, or `gpt-3.5-turbo`.

- **Groq API**: Uses cost-effective models available through Groq. Set `LLM_ENGINE` to one of the following options: `"llama3-8b-8192"`, `"llama3-70b-8192"`, `"mixtral-8x7b-32768"`, or `"gemma-7b-it"`.

### Configuration File

Configure the project by modifying the `config.py` file. Here are the key settings:

```python
LOGS_SIZE = 8

APPROACH = "ZEROSHOT"  # Choose between "ZEROSHOT" and "AGENTIC"

LLM_ENGINE = "openai"  # Choose between "openai" and "groq"
# If LLM_ENGINE = "openai"
GPT_ENGINE = "gpt-4o"  # Choose between "gpt-4o", "gpt-4o-mini", or "gpt-3.5-turbo"

LLM_TEMPERATURE = 0.5
```

- **`LOGS_SIZE`**: Defines the number of logs to maintain.
- **`APPROACH`**: Set this to either `"ZEROSHOT"` or `"AGENTIC"` based on the desired decision-making approach.
- **`LLM_ENGINE`**: Choose the LLM engine. If set to `"openai"`, specify the `GPT_ENGINE`. For Groq models, use one of the specified Groq models.
- **`LLM_TEMPERATURE`**: Controls the randomness of responses. A value closer to 0 makes the output more deterministic, while higher values introduce more randomness.

### Environment Variables

Create a `.env` file in the root directory to configure API keys for the LLM engines. The file should include:

- **For OpenAI Engine:**
  ```plaintext
  OPENAI_API_KEY=your_openai_api_key_here
  ```

- **For Groq API Engine:**
  ```plaintext
  GROQ_API_KEY=your_groq_api_key_here
  ```

Replace `your_openai_api_key_here` and `your_groq_api_key_here` with your actual API keys.

## Getting Started

To start using the project:

### Prerequisites

- Python 3.x
- [pip](https://pip.pypa.io/en/stable/installation/) for package management

### Installation

1. Clone the repository:
   ```bash
   git clone https://github.com/danilotrix86/llm_survival.git
   cd llm_survival
   ```

2. Set up a virtual environment (optional):
   ```bash
   python -m venv venv
   # Activate the virtual environment
   # On Windows
   venv\Scripts\activate
   # On macOS/Linux
   source venv/bin/activate
   ```

3. Install dependencies:
   ```bash
   pip install -r requirements.txt
   ```

### Running the Application

Run the application with `uvicorn`:
```bash
python.exe .\run.py
```

The API will be accessible at `http://127.0.0.1:8000`.

## API Endpoints

- **`GET /messages/`**: Fetches messages from the game settings.
- **`GET /xp/`**: Fetches experience points (xp) data.
- **`POST /next_action/`**: Determines the next action based on the received request. Supports different approaches (`ZEROSHOT` or `AGENTIC`).
- **`POST /start_new_game/`**: Starts a new game session, resetting logs and player information.

## Error Handling

The API provides detailed error messages and status codes to help with debugging and usage.

## Logging

The application uses Python's `logging` module for tracking important events and errors. Logs are displayed in the console and can be further configured in `logging.basicConfig`.