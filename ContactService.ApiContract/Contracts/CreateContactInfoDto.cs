namespace ContactService.ApiContract.Contracts;

public class CreateContactInfoDto
{
    public int InfoType { get; set; }
    public string InfoValue { get; set; } = string.Empty;
}