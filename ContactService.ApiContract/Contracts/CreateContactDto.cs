namespace ContactService.ApiContract.Contracts;

public class CreateContactDto
{
    public string Name { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public List<CreateContactInfoDto> ContactInfos { get; set; } = new();
}