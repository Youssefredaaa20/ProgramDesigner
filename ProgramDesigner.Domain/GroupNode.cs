using System.Collections.Generic;

namespace ProgramDesigner.Domain;

public class GroupNode : ProgramNode
{
    public GroupRule Rule { get; set; }
    public int? ChoiceCount { get; set; }
    public List<ProgramNode> Children { get; set; } = new();
}
