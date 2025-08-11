using System;
using MediatR;
using Shared.CrossCutting.Models;

namespace Shared.CrossCutting.Interfaces;

public interface ICommand : IRequest<ApiResponse>;

public interface ICommand<TResponse> : IRequest<ApiResponse<TResponse>>;

public interface ICommandHandler<in TCommand> : 
    IRequestHandler<TCommand, ApiResponse>
    where TCommand : ICommand;

public interface ICommandHandler<in TCommand, TResponse> :
    IRequestHandler<TCommand, ApiResponse<TResponse>>
    where TCommand : ICommand<TResponse>;

public interface IQuery : IRequest<ApiResponse>;
public interface IQuery<TResponse> : IRequest<ApiResponse<TResponse>>;

public interface IQueryHandler<in TQuery> : IRequestHandler<TQuery, ApiResponse>
    where TQuery : IQuery;



public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, ApiResponse<TResponse>>
    where TQuery : IQuery<TResponse>;


