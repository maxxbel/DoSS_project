namespace DocReview.Infrastructure;

public class InfrastructureException: Exception
{
    protected InfrastructureException(string message) : base(message) {}
}

public class InfrastructureConflictException(string message) : InfrastructureException(message);

public class InfrastructureNotFoundException(string name, object key)
    : InfrastructureException($"Entity {name} ({key}) was not found.");

public class InfrastructureForbiddenException(string message) : InfrastructureException(message);