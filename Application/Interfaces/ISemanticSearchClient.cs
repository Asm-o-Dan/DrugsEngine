namespace Application.Interfaces;

public sealed record SemanticHit(Guid Id, double Score);

public sealed record SemanticDocument(Guid Id, string? Description, string? DescriptionSource);

/// <summary>Клиент Python-сервиса векторного поиска (Qdrant + эмбеддинги).</summary>
public interface ISemanticSearchClient
{
    /// <summary>Задан ли адрес сервиса.</summary>
    bool IsConfigured { get; }

    /// <summary>Поиск препаратов по смыслу текста. <exception cref="SemanticSearchUnavailableException"/></summary>
    Task<IReadOnlyList<SemanticHit>> SearchAsync(string text, int limit, CancellationToken ct);

    /// <summary>Препараты с похожими описаниями. <exception cref="SemanticSearchUnavailableException"/></summary>
    Task<IReadOnlyList<SemanticHit>> SimilarAsync(Guid drugId, int limit, CancellationToken ct);

    /// <summary>Описание препарата из векторного индекса; null, если препарат не проиндексирован.</summary>
    Task<SemanticDocument?> GetDocumentAsync(Guid drugId, CancellationToken ct);
}
