namespace VetClinic.Application.Interfaces;

public interface IRecoveryLockout
{
    int MaxAttempts { get; }
    void SetMaxAttempts(int maxAttempts);
    bool IsLocked(string username, out TimeSpan remaining);
    void RegisterFailure(string username);
    void Reset(string username);
}
