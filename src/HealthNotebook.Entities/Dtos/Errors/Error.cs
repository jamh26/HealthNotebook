namespace HealthNotebook.Entities.Dtos.Errors;

public class Error
{
    public int Code { get; set; }
    public string Type { get; set; }
    public string Message { get; set; }
}