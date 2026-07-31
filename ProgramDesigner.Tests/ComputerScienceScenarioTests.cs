using System.Linq;
using Xunit;
using ProgramDesigner.Domain;
using ProgramDesigner.Application.Services;

namespace ProgramDesigner.Tests;

public class ComputerScienceScenarioTests
{
    private readonly ValidationService _validationService;

    public ComputerScienceScenarioTests()
    {
        _validationService = new ValidationService();
    }

    private GroupNode BuildComputerScienceTree()
    {
        var foundations = TestNodeBuilder.Step("Foundations");
        
        var mlBasics = TestNodeBuilder.Step("ML Basics");
        var electives = TestNodeBuilder.Group("Electives", GroupRule.Choice, 2, 
            TestNodeBuilder.Step("Elective 1"), 
            TestNodeBuilder.Step("Elective 2"), 
            TestNodeBuilder.Step("Elective 3"));
        var aiCapstone = TestNodeBuilder.Step("AI Capstone").WithPrerequisite(electives);
        
        var aiMajor = TestNodeBuilder.Group("AI", GroupRule.InOrder, null, mlBasics, electives, aiCapstone);
        var itMajor = TestNodeBuilder.Step("IT");
        var programmingMajor = TestNodeBuilder.Step("Programming");

        var major = TestNodeBuilder.Group("Major", GroupRule.Choice, 1, aiMajor, itMajor, programmingMajor).WithPrerequisite(foundations);

        var finalCapstone = TestNodeBuilder.Step("Final Capstone").WithPrerequisite(major);

        return TestNodeBuilder.Group("Computer Science", GroupRule.InOrder, null, foundations, major, finalCapstone);
    }

    [Fact]
    public void Validate_ComputerScienceTree_IsValidIsTrueAndImpossiblePrerequisitesIsEmpty()
    {
        // Arrange
        var root = BuildComputerScienceTree();

        // Act
        var result = _validationService.Validate(root);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.ImpossiblePrerequisites);
    }

    [Fact]
    public void Validate_ComputerScienceTree_ReachabilityWarningsContainsOneWarningForAICapstone()
    {
        // Arrange
        var root = BuildComputerScienceTree();

        // Act
        var result = _validationService.Validate(root);

        // Assert
        Assert.Single(result.ReachabilityWarnings);
        var warning = result.ReachabilityWarnings.Single();
        Assert.Equal("AI Capstone", warning.NodeName);
        Assert.Equal("Electives", warning.PrerequisiteName);
        Assert.Contains("Target is inside 'Major'", warning.Reason); 
        // Note: The warning reason actually finds 'Major' as the risky ancestor first? 
        // Let's check the code for ValidationService.CheckReachability: it traverses up from the target. 
        // The target is 'Electives'. Its parent is 'AI'. Its parent is 'Major' (Choice 1 of 3).
        // Wait, 'Electives' is NOT a choice that countToChoose < group.Children.Count?
        // Ah, the target is Electives, which is inside AI, which is inside Major.
        // Major is a Choice (1 of 3). So the warning is because Target is inside 'Major'.
    }
}
