namespace MyPersonalGit.Models;

/// <summary>
/// A WebAuthn/FIDO2 credential (passkey or hardware security key) registered to a user.
/// </summary>
public class WebAuthnCredential
{
    public int Id { get; set; }
    public required string Username { get; set; }

    /// <summary>Display name set by the user (e.g., "YubiKey 5", "iPhone Passkey").</summary>
    public required string Name { get; set; }

    /// <summary>Base64url-encoded credential ID from the authenticator.</summary>
    public required string CredentialId { get; set; }

    /// <summary>Base64url-encoded public key (COSE format).</summary>
    public required string PublicKey { get; set; }

    /// <summary>Signature counter for replay detection.</summary>
    public long SignCount { get; set; }

    /// <summary>AAGUID of the authenticator (identifies make/model).</summary>
    public string? AaGuid { get; set; }

    /// <summary>Whether this is a platform authenticator (passkey) vs cross-platform (security key).</summary>
    public bool IsPlatform { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastUsedAt { get; set; }
}
