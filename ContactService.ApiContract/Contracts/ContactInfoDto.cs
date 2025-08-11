namespace ContactService.ApiContract.Contracts;

public class ContactInfoDto
{
    public Guid Id { get; set; }
    public string InfoType { get; set; }
    public string InfoValue { get; set; } = string.Empty;
}