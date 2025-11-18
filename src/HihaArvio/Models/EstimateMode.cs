namespace HihaArvio.Models;

/// <summary>
/// Represents the mode of time estimate generation.
/// </summary>
public enum EstimateMode
{
    /// <summary>
    /// Work/project-related time estimates (e.g., "2 weeks", "3 sprints").
    /// </summary>
    Work = 0,

    /// <summary>
    /// Generic duration estimates (e.g., "5 minutes", "2 hours").
    /// </summary>
    Generic = 1,

    /// <summary>
    /// Humorous/exaggerated estimates (easter egg mode, triggered by >15s shake).
    /// </summary>
    Humorous = 2
}
