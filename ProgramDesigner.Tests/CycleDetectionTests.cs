using System.Linq;
using Xunit;
using ProgramDesigner.Domain;
using ProgramDesigner.Application.Services;

namespace ProgramDesigner.Tests;

public class CycleDetectionTests
{
    private readonly ValidationService _validationService;

    public CycleDetectionTests()
    {
        _validationService = new ValidationService();
    }

    [Fact]
    public void Validate_DirectCycle_IsValidIsFalse()
    {
        // Arrange
        var nodeA = TestNodeBuilder.Step("Node A");
        var nodeB = TestNodeBuilder.Step("Node B");
        
        nodeA.WithPrerequisite(nodeB);
        nodeB.WithPrerequisite(nodeA);

        var root = TestNodeBuilder.Group("Root", GroupRule.InOrder, null, nodeA, nodeB);

        // Act
        var result = _validationService.Validate(root);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(1, result.ImpossiblePrerequisites.Count);
        Assert.Contains(result.ImpossiblePrerequisites, i => i.NodeName == "Node A" && i.PrerequisiteName == "Node B");
    }

    [Fact]
    public void Validate_SelfReference_IsValidIsFalseWithReason()
    {
        // Arrange
        var nodeA = TestNodeBuilder.Step("Node A");
        nodeA.WithPrerequisite(nodeA);

        var root = TestNodeBuilder.Group("Root", GroupRule.InOrder, null, nodeA);

        // Act
        var result = _validationService.Validate(root);

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.ImpossiblePrerequisites);
        var issue = result.ImpossiblePrerequisites.Single();
        Assert.Equal("Node A", issue.NodeName);
        Assert.Equal("Node A", issue.PrerequisiteName);
        Assert.Contains("itself", issue.Reason.ToLower());
    }

    [Fact]
    public void Validate_GroupDependsOnDescendant_IsValidIsFalseWithReason()
    {
        // Arrange
        var descendant = TestNodeBuilder.Step("Descendant");
        var group = TestNodeBuilder.Group("My Group", GroupRule.InOrder, null, descendant);
        group.WithPrerequisite(descendant);

        var root = TestNodeBuilder.Group("Root", GroupRule.InOrder, null, group);

        // Act
        var result = _validationService.Validate(root);

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.ImpossiblePrerequisites);
        var issue = result.ImpossiblePrerequisites.Single();
        Assert.Equal("My Group", issue.NodeName);
        Assert.Equal("Descendant", issue.PrerequisiteName);
        Assert.Contains("inside its own subtree", issue.Reason.ToLower());
    }
}
