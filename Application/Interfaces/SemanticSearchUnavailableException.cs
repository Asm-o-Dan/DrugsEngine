namespace Application.Interfaces;

/// <summary>Сервис векторного поиска не настроен или не отвечает.</summary>
public sealed class SemanticSearchUnavailableException(string message, Exception? inner = null) : Exception(message, inner);
