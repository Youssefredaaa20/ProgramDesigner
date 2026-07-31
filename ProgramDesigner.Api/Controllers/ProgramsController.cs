using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ProgramDesigner.Application.Dtos;
using ProgramDesigner.Application.Services;

namespace ProgramDesigner.Api.Controllers;

[ApiController]
[Route("programs")]
public class ProgramsController : ControllerBase
{
    private readonly IProgramService _programService;
    private readonly TreeBuilder _treeBuilder;
    private readonly IValidationService _validationService;

    public ProgramsController(
        IProgramService programService, 
        TreeBuilder treeBuilder,
        IValidationService validationService)
    {
        _programService = programService;
        _treeBuilder = treeBuilder;
        _validationService = validationService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateProgram([FromBody] ProgramNodeRequestDto requestDto)
    {
        try
        {
            var result = await _programService.CreateProgramAsync(requestDto);
            return CreatedAtAction(nameof(GetProgram), new { id = result.ProgramId }, result.RootNode);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProgram(Guid id)
    {
        var responseDto = await _programService.GetProgramAsync(id);
        
        if (responseDto == null)
        {
            return NotFound();
        }
        
        return Ok(responseDto);
    }

    [HttpPost("{id}/validate")]
    public async Task<IActionResult> ValidateProgram(Guid id)
    {
        var nodes = await _programService.GetProgramNodesAsync(id);
        
        if (nodes == null || !nodes.Any())
        {
            return NotFound();
        }

        var rootNode = _treeBuilder.BuildDomainTree(nodes);
        if (rootNode == null)
        {
            return NotFound();
        }

        var validationResult = _validationService.Validate(rootNode);

        return Ok(validationResult);
    }
}
