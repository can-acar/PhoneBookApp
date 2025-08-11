# Organization of Projects in Solution

## Contact Service
- ContactService.Api
- ContactService.ApiContract
- ContactService.ApplicationService
- ContactService.Container
- ContactService.Domain
- ContactService.Grpc
- ContactService.Infrastructure
- ContactService.Tests

## Report Service
- ReportService.Api
- ReportService.ApiContract
- ReportService.ApplicationService
- ReportService.Container
- ReportService.Consumer (New separate project for asynchronous report processing)
- ReportService.Domain
- ReportService.Infrastructure
- ReportService.Tests

## Solution Structure

The solution follows a clean architecture pattern with separation of concerns:

1. **API Layer**: Entry point for HTTP requests, contains controllers
2. **ApiContract**: DTOs and contracts for API communication
3. **ApplicationService**: Business logic and handlers
4. **Container**: Dependency injection configuration
5. **Domain**: Core entities and business rules
6. **Grpc**: Service-to-service communication
7. **Infrastructure**: Data access and external services
8. **Consumer**: Background processing of asynchronous tasks

## Consumer Project

The ReportService.Consumer project is organized as a separate microservice that:

1. Runs independently from the API
2. Consumes messages from Kafka topics
3. Processes report generation requests asynchronously
4. Updates report status when processing is complete
5. Publishes completion events back to Kafka

By separating the consumer into its own project, we achieve:
- Better separation of concerns
- Independent scaling of API and processing resources
- More robust error handling for long-running tasks
- Easier deployment and monitoring of background processes
