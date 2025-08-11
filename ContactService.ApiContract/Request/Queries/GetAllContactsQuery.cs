using ContactService.ApiContract.Contracts;
using ContactService.Domain.Entities;
using MediatR;
using Shared.CrossCutting;
using Shared.CrossCutting.Interfaces;
using Shared.CrossCutting.Models;

namespace ContactService.ApiContract.Request.Queries;

// GetAllContactsQuery'in return type'ı özel bir durum - PageResponse<T> kullanıyor
// Bu yüzden IRequest<PageResponse<ContactSummaryDto>> şeklinde kalmalı
public class GetAllContactsQuery : IQuery<PageResponse<ContactSummaryDto>>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SearchTerm { get; set; }
}