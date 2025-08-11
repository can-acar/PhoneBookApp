using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ContactService.ApplicationService.Behaviors;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Moq;
using Xunit;

namespace ContactService.Tests.Behaviors;

public class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_WhenValidationPasses_ShouldProceedWithRequest()
    {
        // Arrange
        var validators = new List<IValidator<TestRequest>> { CreatePassingValidator() };
        var behavior = new ValidationBehavior<TestRequest, TestResponse>(validators);
        
        var response = new TestResponse();
        RequestHandlerDelegate<TestResponse> next = (cancellationToken) => Task.FromResult(response);
        
        // Act
        var result = await behavior.Handle(new TestRequest(), next, CancellationToken.None);
        
        // Assert
        Assert.Equal(response, result);
    }
    
    [Fact]
    public async Task Handle_WithNoValidators_ShouldProceedWithRequest()
    {
        // Arrange
        var validators = Enumerable.Empty<IValidator<TestRequest>>();
        var behavior = new ValidationBehavior<TestRequest, TestResponse>(validators);
        
        var response = new TestResponse();
        RequestHandlerDelegate<TestResponse> next = (cancellationToken) => Task.FromResult(response);
        
        // Act
        var result = await behavior.Handle(new TestRequest(), next, CancellationToken.None);
        
        // Assert
        Assert.Equal(response, result);
    }
    
    [Fact]
    public async Task Handle_WhenValidationFails_ShouldThrowValidationException()
    {
        // Arrange
        var validators = new List<IValidator<TestRequest>> { CreateFailingValidator() };
        var behavior = new ValidationBehavior<TestRequest, TestResponse>(validators);
        
        var nextCalled = false;
        RequestHandlerDelegate<TestResponse> next = (cancellationToken) => {
            nextCalled = true;
            return Task.FromResult(new TestResponse());
        };
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() => 
            behavior.Handle(new TestRequest(), next, CancellationToken.None));
        
        Assert.NotNull(exception);
        Assert.Single(exception.Errors);
        Assert.Equal("Error message", exception.Errors.First().ErrorMessage);
        Assert.False(nextCalled, "Next delegate should not have been called when validation fails");
    }
    
    [Fact]
    public async Task Handle_WithMultipleValidators_ShouldCombineErrors()
    {
        // Arrange
        var validators = new List<IValidator<TestRequest>>
        {
            CreateFailingValidator("Error 1"),
            CreateFailingValidator("Error 2")
        };
        
        var behavior = new ValidationBehavior<TestRequest, TestResponse>(validators);
        
        RequestHandlerDelegate<TestResponse> next = (cancellationToken) => Task.FromResult(new TestResponse());
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() => 
            behavior.Handle(new TestRequest(), next, CancellationToken.None));
        
        Assert.NotNull(exception);
        Assert.Equal(2, exception.Errors.Count());
        Assert.Contains(exception.Errors, e => e.ErrorMessage == "Error 1");
        Assert.Contains(exception.Errors, e => e.ErrorMessage == "Error 2");
    }
    
    [Fact]
    public async Task Handle_WithMixedValidationResults_ShouldThrowValidationException()
    {
        // Arrange
        var validators = new List<IValidator<TestRequest>>
        {
            CreatePassingValidator(),
            CreateFailingValidator("Some error")
        };
        
        var behavior = new ValidationBehavior<TestRequest, TestResponse>(validators);
        
        RequestHandlerDelegate<TestResponse> next = (cancellationToken) => Task.FromResult(new TestResponse());
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() => 
            behavior.Handle(new TestRequest(), next, CancellationToken.None));
        
        Assert.NotNull(exception);
        Assert.Single(exception.Errors);
        Assert.Equal("Some error", exception.Errors.First().ErrorMessage);
    }
    
    private IValidator<TestRequest> CreatePassingValidator()
    {
        var mockValidator = new Mock<IValidator<TestRequest>>();
        mockValidator
            .Setup(v => v.ValidateAsync(It.IsAny<IValidationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        
        return mockValidator.Object;
    }
    
    private IValidator<TestRequest> CreateFailingValidator(string errorMessage = "Error message")
    {
        var mockValidator = new Mock<IValidator<TestRequest>>();
        var failures = new List<ValidationFailure>
        {
            new ValidationFailure("Property", errorMessage)
        };
        
        mockValidator
            .Setup(v => v.ValidateAsync(It.IsAny<IValidationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(failures));
        
        return mockValidator.Object;
    }
    
    // Helper classes for testing
    public class TestRequest : IRequest<TestResponse>
    {
        public string? Data { get; set; }
    }
    
    public class TestResponse
    {
    }
}
