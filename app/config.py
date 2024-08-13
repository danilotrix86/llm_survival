LOGS_SIZE = 8

APPROACH = "ZEROSHOT" # "ZEROSHOT", "AGENTIC"

LLM_ENGINE = "openai" # "openai", these models require a groq api key: "llama3-8b-8192", "llama3-70b-8192", "llama-3.1-70b-versatile", "llama-3.1-405b-reasoning" ONLY FOR PAYING MEMBERS,  "mixtral-8x7b-32768", "gemma-7b-it", "gemma2-9b-it". They are much cheaper than OpenAI's models.

# if LLM_ENGINE = openai
GPT_ENGINE = "gpt-3.5-turbo"  # gpt-4o, gpt-4o-mini or gpt-3.5-turbo

LLM_TEMPERATURE = 0.2