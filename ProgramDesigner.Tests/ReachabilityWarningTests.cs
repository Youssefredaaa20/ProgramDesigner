using System.Linq;
using Xunit;
using ProgramDesigner.Domain;
using ProgramDesigner.Application.Services;

namespace ProgramDesigner.Tests;

public class ReachabilityWarningTests
{
    private readonly ValidationService _validationService;

    public ReachabilityWarningTests()
    {
        _validationService = new ValidationService();
    }

    [Fact]
    public void Validate_PrerequisiteInsideChoiceGroup_ProducesWarningButIsValid()
    {
        // Arrange
        var targetNode = TestNodeBuilder.Step("Target Node");
        var otherNode = TestNodeBuilder.Step("Other Node");
        var choiceGroup = TestNodeBuilder.Group("Choice Group", GroupRule.Choice, 1, targetNode, otherNode);
        
        var dependentNode = TestNodeBuilder.Step("Dependent Node").WithPrerequisite(targetNode);
        
        var root = TestNodeBuilder.Group("Root", GroupRule.InOrder, null, choiceGroup, dependentNode);

        // Act
        var result = _validationService.Validate(root);

        // Assert
        Assert.True(result.IsValid);
        Assert.Single(result.ReachabilityWarnings);
        var warning = result.ReachabilityWarnings.Single();
        Assert.Equal("Dependent Node", warning.NodeName);
        Assert.Equal("Target Node", warning.PrerequisiteName);
        Assert.Contains("inside 'Choice Group'", warning.Reason);
    }

    [Fact]
    public void Validate_PrerequisiteIsChoiceGroupItself_NoWarning()
    {
        // Arrange
        var childNode1 = TestNodeBuilder.Step("Child Node 1");
        var childNode2 = TestNodeBuilder.Step("Child Node 2");
        var choiceGroup = TestNodeBuilder.Group("Choice Group", GroupRule.Choice, 1, childNode1, childNode2);
        
        var dependentNode = TestNodeBuilder.Step("Dependent Node").WithPrerequisite(choiceGroup);
        
        var root = TestNodeBuilder.Group("Root", GroupRule.InOrder, null, choiceGroup, dependentNode);

        // Act
        var result = _validationService.Validate(root);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.ReachabilityWarnings);
    }

    [Fact]
    public void Validate_PrerequisiteInsidePickAllChoiceGroup_NoWarning()
    {
        // Arrange
        var targetNode = TestNodeBuilder.Step("Target Node");
        var otherNode = TestNodeBuilder.Step("Other Node");
        // ChoiceCount == Children.Count (2 == 2)
        var choiceGroup = TestNodeBuilder.Group("Pick All Group", GroupRule.Choice, 2, targetNode, otherNode);
        
        var dependentNode = TestNodeBuilder.Step("Dependent Node").WithPrerequisite(targetNode);
        
        var root = TestNodeBuilder.Group("Root", GroupRule.InOrder, null, choiceGroup, dependentNode);

        // Act
        var result = _validationService.Validate(root);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.ReachabilityWarnings);
    }

    [Fact]
    public void Validate_TwoLevelDeepReachability_ProducesWarning()
    {
        // Arrange
        // A Choice group nested inside an InOrder group, which is itself inside another Choice group,
        // where the risky ancestor is two levels up, not the immediate parent.
        // Actually, the prompt means: prerequisite points to a node.
        // The node is inside an InOrder group.
        // That InOrder group is inside a Choice group where count < children.
        // The risky ancestor is two levels up.
        
        var targetNode = TestNodeBuilder.Step("Target Node");
        var inOrderGroup = TestNodeBuilder.Group("Inner InOrder Group", GroupRule.InOrder, null, targetNode);
        var otherChoiceOption = TestNodeBuilder.Step("Other Choice Option");
        var outerChoiceGroup = TestNodeBuilder.Group("Outer Choice Group", GroupRule.Choice, 1, inOrderGroup, otherChoiceOption);
        
        var dependentNode = TestNodeBuilder.Step("Dependent Node").WithPrerequisite(targetNode);

        var root = TestNodeBuilder.Group("Root", GroupRule.InOrder, null, outerChoiceGroup, dependentNode);

        // Act
        var result = _validationService.Validate(root);

        // Assert
        Assert.True(result.IsValid);
        Assert.Single(result.ReachabilityWarnings);
        var warning = result.ReachabilityWarnings.Single();
        Assert.Equal("Dependent Node", warning.NodeName);
        Assert.Equal("Target Node", warning.PrerequisiteName);
        Assert.Contains("inside 'Outer Choice Group'", warning.Reason);
    }
}
