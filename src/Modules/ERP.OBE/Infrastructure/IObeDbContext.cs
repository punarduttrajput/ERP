using ERP.OBE.Domain;
using Microsoft.EntityFrameworkCore;

namespace ERP.OBE.Infrastructure;

public interface IObeDbContext
{
    DbSet<CoPoMapping> CoPoMappings { get; }
    DbSet<CoPsoMapping> CoPsoMappings { get; }
    DbSet<DirectAttainment> DirectAttainments { get; }
    DbSet<IndirectAttainmentSurvey> IndirectSurveys { get; }
    DbSet<SurveyQuestion> SurveyQuestions { get; }
    DbSet<SurveyResponse> SurveyResponses { get; }
    DbSet<AttainmentGap> AttainmentGaps { get; }
    DbSet<ActionPlan> ActionPlans { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
