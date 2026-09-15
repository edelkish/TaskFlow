namespace TaskFlow.Application.DTOs;

public record LoginDto(
    string Email,
    string Password
);

public record RegisterDto(
    string Email,
    string Password,
    string FullName
);

public record AuthResponseDto(
    string Token,
    DateTime Expiration,
    string Email,
    string FullName
);
