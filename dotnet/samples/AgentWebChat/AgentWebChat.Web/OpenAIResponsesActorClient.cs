﻿// Copyright (c) Microsoft. All rights reserved.

using System.ClientModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using A2A;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Responses;

namespace AgentWebChat.Web;

[SuppressMessage("Style", "IDE0051:Remove unused private members", Justification = "for debug / sample purposes")]
[SuppressMessage("Style", "IDE0059:Unnecessary assignment of a value", Justification = "debug")]
internal sealed class OpenAIResponsesActorClient : IAgentClient
{
    private readonly string _baseUri;

    public OpenAIResponsesActorClient(string baseUri)
    {
        this._baseUri = baseUri.TrimEnd('/');
    }

    public async IAsyncEnumerable<AgentRunResponseUpdate> RunStreamingAsync(
        string agentName,
        IList<ChatMessage> messages,
        string? threadId = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // this is a "root" of the OpenAI Responses surface for the agent
        var relativeUri = "/" + agentName + "/v1/";

        OpenAIClientOptions options = new()
        {
            Endpoint = new Uri(this._baseUri + relativeUri)
        };

#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        OpenAIResponseClient openAiClient = new(model: "myModel!", credential: new ApiKeyCredential("dummy-key"), options: options);
#pragma warning restore OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        var chatClient = openAiClient.AsIChatClient();
        var chatOptions = new ChatOptions()
        {
            ConversationId = threadId
        };

        await foreach (var update in chatClient.GetStreamingResponseAsync(messages, chatOptions, cancellationToken: cancellationToken))
        {
            // Depending on what type is returned by server MEAI will convert it differently, and since OpenAI SDK
            // has really "closed" most of types definitions
            // (i.e. https://github.com/openai/openai-dotnet/blob/84286cd916adc7b03e50cc8031b587714af016ed/src/Generated/Models/Responses/InternalResponsesAssistantMessage.cs#L12)
            // it may appear that sample frontend will not be able to interpret some of the output.
            // Should be fixed in later versions of OpenAI SDK.

            yield return new AgentRunResponseUpdate(update);
        }
    }

    public Task<AgentCard?> GetAgentCardAsync(string agentName, CancellationToken cancellationToken = default)
        => Task.FromResult<AgentCard?>(null!);
}
