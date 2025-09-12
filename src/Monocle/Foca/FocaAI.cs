using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Dynamo.Graph.Nodes;
using Dynamo.Graph.Notes;
using Dynamo.Models;
using Dynamo.ViewModels;
using MonocleViewExtension.Utilities;
using OpenAI;
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

        List<ChatMessage> chatMessages = new List<ChatMessage>();

        /// <summary>
        /// Response from backend service
        /// </summary>
        internal string response;

        public FocaAI(string apiKey, string endpoint = "https://api.x.ai/v1")
        {
            // Create a ChatGPTClient instance with the API key and endpoint
            chatGPTClient = new(
                model: "grok-code-fast-1", // Or other Grok model names like "grok-beta"
                credential: new ApiKeyCredential(apiKey),
                options: new OpenAIClientOptions() { Endpoint = new Uri(endpoint) });

            chatMessages.Add(new SystemChatMessage("Revit & Dynamo expert, adept at graph creation/modification, element management, & workflow automation. Proficient in Dynamo, Revit, BIM, AutoCAD, Custom Nodes, Packages, Python, & architectural modeling. Offer clear explanations, best practices, & code examples.\r\n\r\n"));

            // Create a ChatGPTAssistantClient instance with the API key and endpoint
            //assistantClient = new(apiKey, endpoint);

            //assistant = assistantClient.GetAssistant("asst_J8PSA1asQDqEGluCGMykzJhJ").Value;
        }

        internal async Task SendMessageToGPT(string msg)
        {
            // Send the user's input to the ChatGPT client and start to stream the response
            // Single chat completion
            chatMessages.Add(ChatMessage.CreateUserMessage(msg));

            ChatCompletion completion = await chatGPTClient.CompleteChatAsync(chatMessages);
            response = completion.Content[0].Text;
        }


        #region commands
        internal static async Task SetGroupTitleAndDescription(AnnotationViewModel newGroup)
        {
            List<string> nodeNames = new List<string>();
            List<string> noteText = new List<string>();

            foreach (var modelBase in newGroup.Nodes)
            {
                if (modelBase is not NoteModel)
                {
                    var node = newGroup.WorkspaceViewModel.Nodes.FirstOrDefault(n => n.NodeModel.GUID.Equals(modelBase.GUID));

                    if (node is null) continue;

                    nodeNames.Add(node.Name);
                }

                if (modelBase is NoteModel noteModel)
                {
                    noteText.Add(noteModel.Text);
                }
            }
            string noteTexts = noteText.Any() ? $"Dynamo Notes containing text: {string.Join(',', noteText)}" : "";


            string prompt = $"Dynamo nodes: {string.Join(',', nodeNames)}{noteTexts}. Suggest concise title and description (<20 words)." +
                            "<response_format>\r\n<title></title>\r\n<description></description>";

            var apiKey = Globals.OpenAIApiKey;


            FocaAI focaAi =
                new FocaAI(apiKey);

            await focaAi.SendMessageToGPT(prompt);

            var result = focaAi.response;

            newGroup.AnnotationDescriptionText = Utilities.StringUtils.FindTextBetween(result, "<description>", "</description>");
            newGroup.AnnotationText = Utilities.StringUtils.FindTextBetween(result, "<title>", "</title>");
        }



        #endregion
    }
}
