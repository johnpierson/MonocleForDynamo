using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenAI.Assistants;
using OpenAI.Chat;

namespace MonocleViewExtension.Foca
{
    
    public class FocaAI
    {
        // Chat GPT related fields
        private readonly ChatClient chatGPTClient;
        // Chat GPT Assistant related fields
#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        private readonly AssistantClient assistantClient;

        private readonly Assistant assistant;


        private readonly List<MessageContent> userMessages = new List<MessageContent>();

        private readonly List<MessageContent> assistantMessages = new List<MessageContent>();
#pragma warning restore OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        /// <summary>
        /// Response from backend service
        /// </summary>
        internal string response;

        public FocaAI(string apiKey)
        {
            // Create a ChatGPTClient instance with the API key
            chatGPTClient = new(model: "gpt-4o-mini", apiKey);

            // Create a ChatGPTAssistantClient instance with the API key
            //assistantClient = new(apiKey);

            //assistant = assistantClient.GetAssistant("asst_J8PSA1asQDqEGluCGMykzJhJ").Value;
        }

        internal async Task SendMessageToGPT(string msg)
        {
            // Send the user's input to the ChatGPT client and start to stream the response
            // Single chat completion
            ChatCompletion completion = await chatGPTClient.CompleteChatAsync(msg);
            response = completion.Content[0].Text;
        }

    }
}
