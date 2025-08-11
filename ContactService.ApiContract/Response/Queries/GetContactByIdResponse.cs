using ContactService.ApiContract.Contracts;

namespace ContactService.ApiContract.Response.Queries;

public class GetContactByIdResponse
{
    public ContactDto? Contact { get; set; }
}
