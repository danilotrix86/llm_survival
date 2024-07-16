from app.services.aiwrapper import OpenAIWrapper

def is_sentence(wrapper, words):
    # Prepare the text from the list of words
    text = " ".join(words)
    
    # Clear any previous messages
    wrapper.clear_messages()
    
    # Add the current text to the messages
    # Add the current text to the messages
    prompt = (
        "Please determine if the following text forms a complete, grammatically correct sentence. "
        "Respond with 'True' if it is a complete sentence and 'False' if it is not:\n\n"
        f"'{text}'"
    )
    wrapper.add_message("user", prompt)

    # Get the completion response
    response = wrapper.completion(response_format="text", max_tokens=10)
    
    # Extract the answer from the response
    answer = response.choices[0].message.content.strip().lower()
    
    # Determine if it's a complete sentence based on the model's response
    if "yes" in answer:
        return True
    else:
        return False

# Example usage
if __name__ == "__main__":
    wrapper = OpenAIWrapper("gpt-4o")
    
    print(is_sentence(wrapper, ["Yesterday"]))  # False
    print(is_sentence(wrapper, ["Yesterday", "I"]))  # False
    print(is_sentence(wrapper, ["Yesterday", "I", "went"]))  # False
    print(is_sentence(wrapper, ["Yesterday", "I", "went", "to"]))  # False
    print(is_sentence(wrapper, ["Yesterday", "I", "went", "to", "the"]))  # False
    print(is_sentence(wrapper, ["Yesterday", "I", "went", "to", "the", "beach"]))  # True
    print(is_sentence(wrapper, ["Yesterday", "I", "went", "to", "the", "beach", "and", "it", "was", "beautiful"]))  # True
