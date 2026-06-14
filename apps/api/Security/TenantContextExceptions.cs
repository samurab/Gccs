namespace Gccs.Api.Security;

public abstract class ApiContextException(string message) : InvalidOperationException(message);

public sealed class MissingTenantContextException(string message) : ApiContextException(message);

public sealed class InvalidUserContextException(string message) : ApiContextException(message);
