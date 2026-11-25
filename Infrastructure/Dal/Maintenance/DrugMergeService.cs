using Infrastructure.Parsing;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Dal.Maintenance;

/// <summary>
/// Проставляет ключ сопоставления (Drug.MatchKey) препаратам без него и объединяет препараты с одинаковым ключом —
/// один товар, записанный разными сетями по-разному. Цены и избранное переносятся на сохраняемую запись;
/// если у обеих есть цена в одной аптеке, остаётся более свежая. Идемпотентно.
/// </summary>
public sealed class DrugMergeService(DrugsBotDbContext db, ILogger<DrugMergeService> logger)
{
    private const int BatchSize = 1000;

    public async Task<(int Keyed, int Merged)> RunAsync(CancellationToken ct)
    {
        var keyed = await BackfillKeysAsync(ct);
        var merged = await MergeDuplicatesAsync(ct);
        if (keyed > 0 || merged > 0)
            logger.LogInformation("Сопоставление товаров: ключей проставлено {Keyed}, дублей объединено {Merged}", keyed, merged);
        return (keyed, merged);
    }

    private async Task<int> BackfillKeysAsync(CancellationToken ct)
    {
        var total = 0;
        while (true)
        {
            // ключ «нет» тоже нужно запомнить, иначе такие препараты будут перебираться каждый раз
            var batch = await db.Drugs.Where(d => d.MatchKey == null).OrderBy(d => d.Id).Take(BatchSize).ToListAsync(ct);
            if (batch.Count == 0) break;
            foreach (var drug in batch)
                drug.SetMatchKey(ProductKey.Compute(drug.Name, drug.Manufacturer, drug.Form) ?? NoKey(drug.Id));
            await db.SaveChangesAsync(ct);
            db.ChangeTracker.Clear();
            total += batch.Count;
        }
        return total;
    }

    /// <summary>Уникальная заглушка для товаров, которые нельзя надёжно сопоставить: они ни с чем не объединяются.</summary>
    private static string NoKey(Guid id) => $"~{id:N}";

    private async Task<int> MergeDuplicatesAsync(CancellationToken ct)
    {
        var groups = await db.Drugs
            .Where(d => d.MatchKey != null && !d.MatchKey.StartsWith("~"))
            .GroupBy(d => d.MatchKey)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key!)
            .ToListAsync(ct);

        var merged = 0;
        foreach (var key in groups)
        {
            // сохраняем запись с наибольшим числом цен (обычно — сеть, где товар давно), при равенстве — более раннюю
            var members = await db.Drugs.Where(d => d.MatchKey == key)
                .Select(d => new { d.Id, Offers = d.DrugItems.Count })
                .OrderByDescending(d => d.Offers).ThenBy(d => d.Id)
                .ToListAsync(ct);
            var keep = members[0].Id;

            await using var tx = await db.Database.BeginTransactionAsync(ct);
            foreach (var dup in members.Skip(1).Select(m => m.Id))
            {
                await db.Database.ExecuteSqlInterpolatedAsync($"""
                    DELETE FROM "DrugItem" d USING "DrugItem" k
                    WHERE d."DrugId" = {dup} AND k."DrugId" = {keep} AND d."DrugStoreId" = k."DrugStoreId" AND d."SeenAt" <= k."SeenAt";
                    DELETE FROM "DrugItem" k USING "DrugItem" d
                    WHERE d."DrugId" = {dup} AND k."DrugId" = {keep} AND d."DrugStoreId" = k."DrugStoreId";
                    UPDATE "DrugItem" SET "DrugId" = {keep} WHERE "DrugId" = {dup};
                    UPDATE "FavoriteDrug" SET "DrugId" = {keep} WHERE "DrugId" = {dup};
                    UPDATE "Drug" k SET "Form" = coalesce(k."Form", d."Form"),
                                        "ActiveIngredient" = coalesce(k."ActiveIngredient", d."ActiveIngredient"),
                                        "CountryCodeId" = coalesce(k."CountryCodeId", d."CountryCodeId")
                    FROM "Drug" d WHERE k."Id" = {keep} AND d."Id" = {dup};
                    DELETE FROM "Drug" WHERE "Id" = {dup};
                    """, ct);
                merged++;
            }
            await tx.CommitAsync(ct);
        }
        return merged;
    }
}
