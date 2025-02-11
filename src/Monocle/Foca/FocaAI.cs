using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dynamo.Graph.Notes;
using Dynamo.ViewModels;
using MonocleViewExtension.Utilities;
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
            string prompt =
                $"Given the following nodes from the Dynamo visual programming tool: {string.Join(',', nodeNames)} and notes with descriptive text of {string.Join(',', noteText)}. " +
                $"What would you title a grouping of these nodes as, in a few words? and what would you add as a more detailed description, in about 40 words or less? Please provide the response as xml, with open and close for title and description.";

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
