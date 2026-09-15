namespace TaskFlow.Application.DTOs;

public record ThemeSettingsDto
{
    public string ColorMode { get; init; } = "light";
    public string SidebarBg { get; init; } = "bg-body-secondary";
    public string HeaderBg { get; init; } = "bg-body";
    public string FooterBg { get; init; } = "bg-body";
    public string Primary { get; init; } = "#467FD0";
}

public record UpdateThemeSettingsDto
{
    public string ColorMode { get; init; } = "light";
    public string SidebarBg { get; init; } = "bg-body-secondary";
    public string HeaderBg { get; init; } = "bg-body";
    public string FooterBg { get; init; } = "bg-body";
    public string Primary { get; init; } = "#467FD0";
}