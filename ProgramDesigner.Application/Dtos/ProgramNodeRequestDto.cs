using System.Collections.Generic;

namespace ProgramDesigner.Application.Dtos;

public class ProgramNodeRequestDto
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    
    // "Step" or "Group"
    public string NodeType { get; set; } = string.Empty;
    
    // Only for Step: "AttendSession", "PassTest", "SubmitWork"
    public string? StepType { get; set; }
    
    // Only for Group: "InOrder", "Choice"
    public string? Rule { get; set; }
    
    public int? ChoiceCount { get; set; }
    
    public string? PrerequisiteKey { get; set; }
    
    public List<ProgramNodeRequestDto>? Children { get; set; }
}
