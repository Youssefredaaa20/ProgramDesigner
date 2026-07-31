using ProgramDesigner.Domain;
using ProgramDesigner.Application.Dtos;

namespace ProgramDesigner.Application.Services;

public interface IValidationService
{
    ValidationResult Validate(ProgramNode root);
}
