using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ProgramDesigner.Application.Dtos;

namespace ProgramDesigner.Application.Services;

public interface IProgramService
{
    Task<(Guid ProgramId, ProgramNodeResponseDto RootNode)> CreateProgramAsync(ProgramNodeRequestDto rootDto);
    Task<ProgramNodeResponseDto?> GetProgramAsync(Guid id);
    Task<List<ProgramDesigner.Domain.ProgramNode>> GetProgramNodesAsync(Guid id);
}
