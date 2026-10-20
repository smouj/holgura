namespace Cleaner.Contracts;

/// <summary>How risky it is to remove an item.</summary>
public enum SafetyLevel
{
    /// <summary>Regenerable data with no value to the user.</summary>
    Safe = 0,

    /// <summary>Probably disposable, but the user should look at it first.</summary>
    Review = 1,

    /// <summary>Never selected automatically.</summary>
    Advanced = 2,
}
