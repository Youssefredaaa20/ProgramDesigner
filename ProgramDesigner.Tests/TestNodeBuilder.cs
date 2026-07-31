using System;
using System.Linq;
using ProgramDesigner.Domain;

namespace ProgramDesigner.Tests;

public static class TestNodeBuilder
{
    public static GroupNode Group(string name, GroupRule rule, int? choiceCount = null, params ProgramNode[] children)
    {
        var group = new GroupNode
        {
            Id = Guid.NewGuid(),
            Name = name,
            Rule = rule,
            ChoiceCount = choiceCount,
            Children = children.ToList()
        };

        // Set up ParentId and OrderIndex for the children
        for (int i = 0; i < group.Children.Count; i++)
        {
            group.Children[i].ParentId = group.Id;
            group.Children[i].OrderIndex = i;
        }

        return group;
    }

    public static StepNode Step(string name, StepType type = StepType.AttendSession)
    {
        return new StepNode
        {
            Id = Guid.NewGuid(),
            Name = name,
            Type = type
        };
    }

    public static T WithPrerequisite<T>(this T node, ProgramNode prereq) where T : ProgramNode
    {
        node.PrerequisiteId = prereq.Id;
        return node;
    }
}
