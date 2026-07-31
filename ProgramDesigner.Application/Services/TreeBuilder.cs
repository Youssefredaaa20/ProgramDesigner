using System;
using System.Collections.Generic;
using System.Linq;
using ProgramDesigner.Domain;
using ProgramDesigner.Application.Dtos;

namespace ProgramDesigner.Application.Services;

public class TreeBuilder
{
    public List<ProgramNode> Flatten(ProgramNodeRequestDto root, Guid programId)
    {
        var flatList = new List<ProgramNode>();
        var keyToGuid = new Dictionary<string, Guid>();
        
        // Pass 1: Pre-order traversal to build the list and assign IDs
        FlattenRecursive(root, programId, null, ref flatList, keyToGuid);
        
        // Pass 2: Resolve prerequisites
        ResolvePrerequisites(root, keyToGuid, flatList);
        
        return flatList;
    }
    
    private void FlattenRecursive(
        ProgramNodeRequestDto dto, 
        Guid programId, 
        Guid? parentId, 
        ref List<ProgramNode> flatList, 
        Dictionary<string, Guid> keyToGuid)
    {
        if (keyToGuid.ContainsKey(dto.Key))
        {
            throw new ArgumentException($"Duplicate key found in request: {dto.Key}");
        }
        
        Guid newId = parentId == null ? programId : Guid.NewGuid();
        keyToGuid[dto.Key] = newId;
        
        int orderIndex = flatList.Count(n => n.ParentId == parentId);
        
        ProgramNode node;
        
        if (dto.NodeType.Equals("Step", StringComparison.OrdinalIgnoreCase))
        {
            node = new StepNode
            {
                Id = newId,
                ProgramId = programId,
                ParentId = parentId,
                OrderIndex = orderIndex,
                Name = dto.Name,
                Type = Enum.Parse<StepType>(dto.StepType ?? "AttendSession", true)
            };
        }
        else if (dto.NodeType.Equals("Group", StringComparison.OrdinalIgnoreCase))
        {
            node = new GroupNode
            {
                Id = newId,
                ProgramId = programId,
                ParentId = parentId,
                OrderIndex = orderIndex,
                Name = dto.Name,
                Rule = Enum.Parse<GroupRule>(dto.Rule ?? "InOrder", true),
                ChoiceCount = dto.ChoiceCount
            };
        }
        else
        {
            throw new ArgumentException($"Invalid NodeType: {dto.NodeType}");
        }
        
        flatList.Add(node);
        
        if (dto.Children != null)
        {
            foreach (var child in dto.Children)
            {
                FlattenRecursive(child, programId, newId, ref flatList, keyToGuid);
            }
        }
    }
    
    private void ResolvePrerequisites(ProgramNodeRequestDto dto, Dictionary<string, Guid> keyToGuid, List<ProgramNode> flatList)
    {
        if (!string.IsNullOrEmpty(dto.PrerequisiteKey))
        {
            if (!keyToGuid.TryGetValue(dto.PrerequisiteKey, out Guid prereqId))
            {
                throw new ArgumentException($"Prerequisite key not found: {dto.PrerequisiteKey}");
            }
            
            var node = flatList.First(n => n.Id == keyToGuid[dto.Key]);
            node.PrerequisiteId = prereqId;
        }
        
        if (dto.Children != null)
        {
            foreach (var child in dto.Children)
            {
                ResolvePrerequisites(child, keyToGuid, flatList);
            }
        }
    }

    public ProgramNodeResponseDto? Rebuild(List<ProgramNode> flatNodes)
    {
        if (flatNodes == null || !flatNodes.Any())
            return null;
            
        var groupedByParent = flatNodes.GroupBy(n => n.ParentId ?? Guid.Empty).ToDictionary(g => g.Key, g => g.OrderBy(n => n.OrderIndex).ToList());
        
        // Find root (ParentId == null)
        var roots = groupedByParent.GetValueOrDefault(Guid.Empty);
        if (roots == null || !roots.Any())
            throw new ArgumentException("No root node found.");
            
        var rootNode = roots.First(); // Assume single root
        
        return BuildNodeResponse(rootNode, groupedByParent);
    }
    
    private ProgramNodeResponseDto BuildNodeResponse(ProgramNode node, Dictionary<Guid, List<ProgramNode>> groupedByParent)
    {
        var dto = new ProgramNodeResponseDto
        {
            Id = node.Id,
            Name = node.Name,
            PrerequisiteId = node.PrerequisiteId
        };
        
        if (node is StepNode stepNode)
        {
            dto.NodeType = "Step";
            dto.StepType = stepNode.Type.ToString();
        }
        else if (node is GroupNode groupNode)
        {
            dto.NodeType = "Group";
            dto.Rule = groupNode.Rule.ToString();
            dto.ChoiceCount = groupNode.ChoiceCount;
        }
        
        if (groupedByParent.TryGetValue(node.Id, out var children))
        {
            dto.Children = children.Select(c => BuildNodeResponse(c, groupedByParent)).ToList();
        }
        
        return dto;
    }

    public ProgramNode? BuildDomainTree(List<ProgramNode> flatNodes)
    {
        if (flatNodes == null || !flatNodes.Any())
            return null;
            
        var groupedByParent = flatNodes.GroupBy(n => n.ParentId ?? Guid.Empty).ToDictionary(g => g.Key, g => g.OrderBy(n => n.OrderIndex).ToList());
        
        var roots = groupedByParent.GetValueOrDefault(Guid.Empty);
        if (roots == null || !roots.Any())
            throw new ArgumentException("No root node found.");
            
        var rootNode = roots.First();
        
        PopulateChildren(rootNode, groupedByParent);
        
        return rootNode;
    }

    private void PopulateChildren(ProgramNode node, Dictionary<Guid, List<ProgramNode>> groupedByParent)
    {
        if (node is GroupNode groupNode)
        {
            if (groupedByParent.TryGetValue(node.Id, out var children))
            {
                groupNode.Children = children;
                foreach (var child in children)
                {
                    PopulateChildren(child, groupedByParent);
                }
            }
        }
    }
}
