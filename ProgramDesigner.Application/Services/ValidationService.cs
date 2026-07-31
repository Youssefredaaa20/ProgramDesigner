using System;
using System.Collections.Generic;
using System.Linq;
using ProgramDesigner.Domain;
using ProgramDesigner.Application.Dtos;

namespace ProgramDesigner.Application.Services;

public class ValidationService : IValidationService
{
    public ValidationResult Validate(ProgramNode root)
    {
        var result = new ValidationResult();

        // Pass 1: Sequence index, lookup maps
        var sequenceMap = new Dictionary<Guid, int>();
        var nodeLookup = new Dictionary<Guid, ProgramNode>();
        var parentLookup = new Dictionary<Guid, ProgramNode>();
        int sequenceIndex = 0;

        BuildLookups(root, null, sequenceMap, nodeLookup, parentLookup, ref sequenceIndex);

        // Pass 2: Impossible prerequisites
        foreach (var node in nodeLookup.Values)
        {
            if (!node.PrerequisiteId.HasValue) continue;

            var targetId = node.PrerequisiteId.Value;
            if (!nodeLookup.TryGetValue(targetId, out var target))
            {
                continue; // Should not happen in a valid db state, but handle gracefully
            }

            string? issueReason = null;

            if (target.Id == node.Id)
            {
                issueReason = "A node cannot depend on itself";
            }
            else if (IsInsideSubtree(nodeLookup, parentLookup, node.Id, target.Id))
            {
                issueReason = "Prerequisite points to a node inside its own subtree";
            }
            else if (sequenceMap[target.Id] > sequenceMap[node.Id])
            {
                issueReason = "Prerequisite refers to a node that appears later in the program";
            }

            if (issueReason != null)
            {
                result.ImpossiblePrerequisites.Add(new PrerequisiteIssue
                {
                    NodeId = node.Id,
                    NodeName = node.Name,
                    PrerequisiteId = target.Id,
                    PrerequisiteName = target.Name,
                    Reason = issueReason
                });
            }
            else
            {
                // Pass 3: Reachability warnings
                CheckReachability(node, target, parentLookup, result.ReachabilityWarnings);
            }
        }

        result.IsValid = result.ImpossiblePrerequisites.Count == 0;
        return result;
    }

    private void BuildLookups(
        ProgramNode node, 
        ProgramNode? parent, 
        Dictionary<Guid, int> sequenceMap, 
        Dictionary<Guid, ProgramNode> nodeLookup, 
        Dictionary<Guid, ProgramNode> parentLookup, 
        ref int sequenceIndex)
    {
        sequenceMap[node.Id] = sequenceIndex++;
        nodeLookup[node.Id] = node;
        
        if (parent != null)
        {
            parentLookup[node.Id] = parent;
        }

        if (node is GroupNode group)
        {
            foreach (var child in group.Children.OrderBy(c => c.OrderIndex))
            {
                BuildLookups(child, group, sequenceMap, nodeLookup, parentLookup, ref sequenceIndex);
            }
        }
    }

    private bool IsInsideSubtree(
        Dictionary<Guid, ProgramNode> nodeLookup, 
        Dictionary<Guid, ProgramNode> parentLookup, 
        Guid subtreeRootId, 
        Guid nodeIdToFind)
    {
        var currentId = nodeIdToFind;
        while (parentLookup.TryGetValue(currentId, out var parent))
        {
            if (parent.Id == subtreeRootId)
            {
                return true;
            }
            currentId = parent.Id;
        }
        return false;
    }

    private void CheckReachability(
        ProgramNode node, 
        ProgramNode target, 
        Dictionary<Guid, ProgramNode> parentLookup, 
        List<PrerequisiteIssue> reachabilityWarnings)
    {
        var currentId = target.Id;
        while (parentLookup.TryGetValue(currentId, out var parent))
        {
            if (parent is GroupNode group && group.Rule == GroupRule.Choice)
            {
                int countToChoose = group.ChoiceCount ?? 1;
                
                if (countToChoose < group.Children.Count)
                {
                    reachabilityWarnings.Add(new PrerequisiteIssue
                    {
                        NodeId = node.Id,
                        NodeName = node.Name,
                        PrerequisiteId = target.Id,
                        PrerequisiteName = target.Name,
                        Reason = $"Target is inside '{group.Name}', a choice of {countToChoose} of {group.Children.Count} — participants who choose a different option will never satisfy this prerequisite"
                    });
                    
                    break;
                }
            }
            currentId = parent.Id;
        }
    }
}
