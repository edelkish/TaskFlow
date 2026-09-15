using TaskFlow.Application.Common;
using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Interfaces;

public interface IAuthService
{
    Task<Result<AuthResponseDto>> LoginAsync(LoginDto dto);
    Task<Result<AuthResponseDto>> RegisterAsync(RegisterDto dto);
}
