using System.Collections.Generic;

namespace ProgramDesigner.Application.Dtos;

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<PrerequisiteIssue> ImpossiblePrerequisites { get; set; } = new();
    public List<PrerequisiteIssue> ReachabilityWarnings { get; set; } = new();
}
