namespace Gccs.Domain.Storage;

public enum ObjectWriteIntentState
{
    Prepared, Writing, Written, Committed, CleanupPending, Cleaned, Ambiguous, ConflictHold, ResolvedNoWrite
}
