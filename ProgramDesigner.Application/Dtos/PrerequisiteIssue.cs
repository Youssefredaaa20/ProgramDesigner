using System;

namespace ProgramDesigner.Application.Dtos;

public class PrerequisiteIssue
{
    public Guid NodeId { get; set; }
    public string NodeName { get; set; } = string.Empty;
    public Guid PrerequisiteId { get; set; }
    public string PrerequisiteName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}
