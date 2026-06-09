using ERP.Chatbot.Domain;

namespace ERP.Chatbot.Application.Services;

public interface ILlmService
{
    Task<string> GenerateResponseAsync(
        string systemPrompt,
        IReadOnlyList<(string role, string content)> conversationHistory,
        string userMessage,
        CancellationToken cancellationToken = default);

    Task<ChatIntent> DetectIntentAsync(
        string userMessage,
        CancellationToken cancellationToken = default);
}
