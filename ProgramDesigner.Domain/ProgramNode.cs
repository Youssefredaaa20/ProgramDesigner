using System;

namespace ProgramDesigner.Domain;

public abstract class ProgramNode
{
    public Guid Id { get; set; }
    public Guid ProgramId { get; set; }
    public Guid? ParentId { get; set; }
    public int OrderIndex { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? PrerequisiteId { get; set; }
}
