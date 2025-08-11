namespace ContactService.ApiContract.Contracts;

public class UpdateContactInfoDto
{
    public Guid? Id { get; set; }
    public int InfoType { get; set; }
    public string InfoValue { get; set; } = string.Empty;
}