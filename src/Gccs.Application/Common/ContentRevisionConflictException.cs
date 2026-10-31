namespace Gccs.Application.Common;
public sealed class ContentRevisionConflictException() : InvalidOperationException("The content changed. Reload and retry with the current revision.");
