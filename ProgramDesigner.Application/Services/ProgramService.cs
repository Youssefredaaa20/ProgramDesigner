using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProgramDesigner.Domain;
using ProgramDesigner.Infrastructure;
using ProgramDesigner.Application.Dtos;

namespace ProgramDesigner.Application.Services;

public class ProgramService : IProgramService
{
    private readonly ProgramDesignerDbContext _dbContext;
    private readonly TreeBuilder _treeBuilder;

    public ProgramService(ProgramDesignerDbContext dbContext, TreeBuilder treeBuilder)
    {
        _dbContext = dbContext;
        _treeBuilder = treeBuilder;
    }

    public async Task<(Guid ProgramId, ProgramNodeResponseDto RootNode)> CreateProgramAsync(ProgramNodeRequestDto rootDto)
    {
        var programId = Guid.NewGuid();
        var flatNodes = _treeBuilder.Flatten(rootDto, programId);

        _dbContext.Nodes.AddRange(flatNodes);
        await _dbContext.SaveChangesAsync();

        return (programId, _treeBuilder.Rebuild(flatNodes)!);
    }

    public async Task<ProgramNodeResponseDto?> GetProgramAsync(Guid id)
    {
        var flatNodes = await _dbContext.Nodes
            .Where(n => n.ProgramId == id)
            .ToListAsync();

        if (!flatNodes.Any())
            return null;

        return _treeBuilder.Rebuild(flatNodes);
    }

    public async Task<List<ProgramNode>> GetProgramNodesAsync(Guid id)
    {
        return await _dbContext.Nodes
            .Where(n => n.ProgramId == id)
            .ToListAsync();
    }
}
