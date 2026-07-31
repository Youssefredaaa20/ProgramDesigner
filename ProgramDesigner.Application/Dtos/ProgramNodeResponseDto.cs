using System;
using System.Collections.Generic;

namespace ProgramDesigner.Application.Dtos;

public class ProgramNodeResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    
    public string NodeType { get; set; } = string.Empty;
    
    public string? StepType { get; set; }
    
    public string? Rule { get; set; }
    
    public int? ChoiceCount { get; set; }
    
    public Guid? PrerequisiteId { get; set; }
    
    public List<ProgramNodeResponseDto>? Children { get; set; }
}
