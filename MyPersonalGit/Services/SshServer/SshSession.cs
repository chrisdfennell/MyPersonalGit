using System.Diagnostics;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using LibGit2Sharp;
using MyPersonalGit.Data;

namespace MyPersonalGit.Services.SshServer;

/// <summary>
/// Handles a single SSH connection through all protocol phases:
/// version exchange, key exchange, authentication, channel management, and git command execution.
///
/// Supported algorithms:
///   KEX:      ecdh-sha2-nistp256
///   Host key: ecdsa-sha2-nistp256
///   Cipher:   aes128-ctr, aes256-ctr
///   MAC:      hmac-sha2-256
///   Auth:     publickey (RSA, ECDSA, Ed25519)
/// </summary>
public sealed class SshSession : IDisposable
{
    private const string ServerVersion = "SSH-2.0-MyPersonalGit_1.0";

    // SSH message types
    private const byte SSH_MSG_DISCONNECT = 1;
    private const byte SSH_MSG_IGNORE = 2;
    private const byte SSH_MSG_UNIMPLEMENTED = 3;
    private const byte SSH_MSG_SERVICE_REQUEST = 5;
    private const byte SSH_MSG_SERVICE_ACCEPT = 6;
    private const byte SSH_MSG_KEXINIT = 20;
    private const byte SSH_MSG_NEWKEYS = 21;
    private const byte SSH_MSG_KEX_ECDH_INIT = 30;
    private const byte SSH_MSG_KEX_ECDH_REPLY = 31;
    private const byte SSH_MSG_USERAUTH_REQUEST = 50;
    private const byte SSH_MSG_USERAUTH_FAILURE = 51;
    private const byte SSH_MSG_USERAUTH_SUCCESS = 52;
    private const byte SSH_MSG_USERAUTH_PK_OK = 60;
    private const byte SSH_MSG_CHANNEL_OPEN = 90;
    private const byte SSH_MSG_CHANNEL_OPEN_CONFIRMATION = 91;
    private const byte SSH_MSG_CHANNEL_WINDOW_ADJUST = 93;
    private const byte SSH_MSG_CHANNEL_DATA = 94;
    private const byte SSH_MSG_CHANNEL_EXTENDED_DATA = 95;
    private const byte SSH_MSG_CHANNEL_EOF = 96;
    private const byte SSH_MSG_CHANNEL_CLOSE = 97;
    private const byte SSH_MSG_CHANNEL_REQUEST = 98;
    private const byte SSH_MSG_CHANNEL_SUCCESS = 99;
    private const byte SSH_MSG_CHANNEL_FAILURE = 100;

    private readonly TcpClient _client;
    private readonly NetworkStream _stream;
    private readonly ECDsa _hostKey;
    private readonly byte[] _hostKeyBlob;
    private readonly string _projectRoot;
    private readonly ISshAuthService _sshAuth;
    private readonly IRepositoryService _repoService;
    private readonly ICollaboratorService _collaboratorService;
    private readonly IDeployKeyService _deployKeyService;
    private readonly IIssueAutoCloseService _issueAutoCloseService;
    private readonly IWorkflowService _workflowService;
    private readonly IAGitFlowService _agitFlowService;
    private readonly ILogger _logger;

    // Protocol state
    private string _clientVersion = "";
    private byte[]? _clientKexInit;
    private byte[]? _serverKexInit;
    private byte[]? _sessionId;
    private string? _authenticatedUser;
    private string? _authKeyFingerprint;

    // Encryption state
    private AesCtrCipher? _encryptCipher;
    private AesCtrCipher? _decryptCipher;
    private HMACSHA256? _encryptMac;
    private HMACSHA256? _decryptMac;
    private uint _sendSeq;
    private uint _recvSeq;
    private bool _encrypted;
    private int _cipherBlockSize = 8; // before encryption
    private int _macLength; // 0 before encryption

    // Chosen algorithms
    private string _chosenCipher = "aes128-ctr";

    public SshSession(
        TcpClient client, ECDsa hostKey, byte[] hostKeyBlob, string projectRoot,
        ISshAuthService sshAuth, IRepositoryService repoService,
        ICollaboratorService collaboratorService, IDeployKeyService deployKeyService,
        IIssueAutoCloseService issueAutoCloseService, IWorkflowService workflowService, IAGitFlowService agitFlowService, ILogger logger)
    {
        _client = client;
        _stream = client.GetStream();
        _hostKey = hostKey;
        _hostKeyBlob = hostKeyBlob;
        _projectRoot = projectRoot;
        _sshAuth = sshAuth;
        _repoService = repoService;
        _collaboratorService = collaboratorService;
        _deployKeyService = deployKeyService;
        _issueAutoCloseService = issueAutoCloseService;
        _workflowService = workflowService;
        _agitFlowService = agitFlowService;
        _logger = logger;

        _client.ReceiveTimeout = 30000;
        _client.SendTimeout = 30000;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        try
        {
            await ExchangeVersions(ct);
            await PerformKeyExchange(ct);
            await HandleServiceRequest(ct);
            await HandleAuthentication(ct);
            await HandleChannels(ct);
        }
        catch (SshDisconnectException) { /* clean disconnect */ }
        catch (OperationCanceledException) { }
        catch (IOException) { /* connection reset */ }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "SSH session error");
            try { await SendDisconnect(11, "Protocol error"); } catch { }
        }
    }

    // ─── Version Exchange ────────────────────────────────────────────────

    private async Task ExchangeVersions(CancellationToken ct)
    {
        // Send our version
        var serverBytes = Encoding.ASCII.GetBytes(ServerVersion + "\r\n");
        await _stream.WriteAsync(serverBytes, ct);

        // Read client version (line ending with \r\n or \n)
        var buf = new byte[256];
        int pos = 0;
        while (pos < buf.Length)
        {
            int b = _stream.ReadByte();
            if (b < 0) throw new IOException("Connection closed during version exchange");
            buf[pos++] = (byte)b;
            if (pos >= 2 && buf[pos - 1] == '\n')
                break;
        }

        _clientVersion = Encoding.ASCII.GetString(buf, 0, pos).TrimEnd('\r', '\n');
        if (!_clientVersion.StartsWith("SSH-2.0-"))
            throw new InvalidOperationException($"Unsupported SSH version: {_clientVersion}");

        _logger.LogDebug("SSH client version: {Version}", _clientVersion);
    }

    // ─── Key Exchange ────────────────────────────────────────────────────

    private async Task PerformKeyExchange(CancellationToken ct)
    {
        // Send our KEXINIT
        _serverKexInit = BuildKexInit();
        await SendPacket(_serverKexInit, ct);

        // Read client KEXINIT
        var clientPacket = await ReadPacket(ct);
        if (clientPacket[0] != SSH_MSG_KEXINIT)
            throw new InvalidOperationException($"Expected KEXINIT, got {clientPacket[0]}");
        _clientKexInit = clientPacket;

        // Parse client's algorithm proposals to select cipher
        NegotiateAlgorithms(clientPacket);

        // Wait for KEX_ECDH_INIT
        var ecdhInit = await ReadPacket(ct);
        if (ecdhInit[0] != SSH_MSG_KEX_ECDH_INIT)
            throw new InvalidOperationException($"Expected KEX_ECDH_INIT, got {ecdhInit[0]}");

        // Parse client's ephemeral public key
        var reader = new SshDataReader(ecdhInit, 1);
        var clientEphemeralPub = reader.ReadBinary();

        // Generate server ephemeral ECDH key pair
        using var serverEcdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        var serverPub = serverEcdh.PublicKey.ExportSubjectPublicKeyInfo();

        // Extract the raw point from the client's key
        // Client sends the uncompressed point Q_C directly
        using var clientEcdhKey = ECDiffieHellman.Create();
        var ecParams = new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            Q = ParseEcPoint(clientEphemeralPub)
        };
        clientEcdhKey.ImportParameters(ecParams);

        // Derive shared secret
        var sharedSecret = serverEcdh.DeriveRawSecretAgreement(clientEcdhKey.PublicKey);

        // Build server's ephemeral public key point (uncompressed)
        var serverParams = serverEcdh.ExportParameters(false);
        var serverQ = new byte[1 + serverParams.Q.X!.Length + serverParams.Q.Y!.Length];
        serverQ[0] = 0x04;
        Buffer.BlockCopy(serverParams.Q.X, 0, serverQ, 1, serverParams.Q.X.Length);
        Buffer.BlockCopy(serverParams.Q.Y, 0, serverQ, 1 + serverParams.Q.X.Length, serverParams.Q.Y.Length);

        // Compute exchange hash H
        var exchangeHash = ComputeExchangeHash(
            _clientVersion, ServerVersion,
            _clientKexInit, _serverKexInit,
            _hostKeyBlob, clientEphemeralPub, serverQ, sharedSecret);

        // Session ID is the first exchange hash
        _sessionId ??= (byte[])exchangeHash.Clone();

        // Sign the exchange hash with our host key
        var signature = _hostKey.SignData(exchangeHash, HashAlgorithmName.SHA256);

        // Build signature blob: string "ecdsa-sha2-nistp256" + string (DER signature)
        var sigBlob = BuildEcdsaSignatureBlob(signature);

        // Send KEX_ECDH_REPLY
        using var reply = new MemoryStream();
        reply.WriteByte(SSH_MSG_KEX_ECDH_REPLY);
        SshDataHelper.WriteBytes(reply, _hostKeyBlob);  // K_S (host key)
        SshDataHelper.WriteBytes(reply, serverQ);         // Q_S (server ephemeral pub)
        SshDataHelper.WriteBytes(reply, sigBlob);          // signature of H
        await SendPacket(reply.ToArray(), ct);

        // Send NEWKEYS
        await SendPacket(new[] { SSH_MSG_NEWKEYS }, ct);

        // Read client NEWKEYS
        var newKeys = await ReadPacket(ct);
        if (newKeys[0] != SSH_MSG_NEWKEYS)
            throw new InvalidOperationException($"Expected NEWKEYS, got {newKeys[0]}");

        // Derive encryption keys
        DeriveKeys(sharedSecret, exchangeHash);
        _encrypted = true;
    }

    private void NegotiateAlgorithms(byte[] clientKexInit)
    {
        var reader = new SshDataReader(clientKexInit, 17); // skip type(1) + cookie(16)

        var clientKex = reader.ReadString();
        var clientHostKey = reader.ReadString();
        var clientCipherC2S = reader.ReadString();
        var clientCipherS2C = reader.ReadString();

        // Negotiate cipher (client-to-server and server-to-client use the same choice)
        var supportedCiphers = new[] { "aes256-ctr", "aes128-ctr" };
        var clientCiphers = clientCipherC2S.Split(',');
        _chosenCipher = "aes128-ctr"; // default
        foreach (var c in clientCiphers)
        {
            if (supportedCiphers.Contains(c))
            {
                _chosenCipher = c;
                break;
            }
        }
    }

    private byte[] BuildKexInit()
    {
        using var ms = new MemoryStream();
        ms.WriteByte(SSH_MSG_KEXINIT);

        // 16-byte random cookie
        var cookie = RandomNumberGenerator.GetBytes(16);
        ms.Write(cookie);

        // Algorithm lists
        SshDataHelper.WriteString(ms, "ecdh-sha2-nistp256");                    // kex_algorithms
        SshDataHelper.WriteString(ms, "ecdsa-sha2-nistp256");                   // server_host_key_algorithms
        SshDataHelper.WriteString(ms, "aes256-ctr,aes128-ctr");                 // encryption_algorithms_client_to_server
        SshDataHelper.WriteString(ms, "aes256-ctr,aes128-ctr");                 // encryption_algorithms_server_to_client
        SshDataHelper.WriteString(ms, "hmac-sha2-256");                         // mac_algorithms_client_to_server
        SshDataHelper.WriteString(ms, "hmac-sha2-256");                         // mac_algorithms_server_to_client
        SshDataHelper.WriteString(ms, "none");                                  // compression_algorithms_client_to_server
        SshDataHelper.WriteString(ms, "none");                                  // compression_algorithms_server_to_client
        SshDataHelper.WriteString(ms, "");                                      // languages_client_to_server
        SshDataHelper.WriteString(ms, "");                                      // languages_server_to_client

        ms.WriteByte(0); // first_kex_packet_follows = false
        ms.Write(new byte[4]); // reserved (uint32 0)

        return ms.ToArray();
    }

    private byte[] ComputeExchangeHash(
        string clientVersion, string serverVersion,
        byte[] clientKexInit, byte[] serverKexInit,
        byte[] hostKeyBlob, byte[] clientQ, byte[] serverQ, byte[] sharedSecret)
    {
        // H = SHA-256(V_C || V_S || I_C || I_S || K_S || Q_C || Q_S || K)
        using var ms = new MemoryStream();
        SshDataHelper.WriteString(ms, clientVersion);
        SshDataHelper.WriteString(ms, serverVersion);
        SshDataHelper.WriteBytes(ms, clientKexInit);
        SshDataHelper.WriteBytes(ms, serverKexInit);
        SshDataHelper.WriteBytes(ms, hostKeyBlob);
        SshDataHelper.WriteBytes(ms, clientQ);
        SshDataHelper.WriteBytes(ms, serverQ);
        SshDataHelper.WriteMpint(ms, sharedSecret);

        return SHA256.HashData(ms.ToArray());
    }

    private byte[] BuildEcdsaSignatureBlob(byte[] derSignature)
    {
        // Parse DER to get r, s integers, then encode as SSH signature
        var (r, s) = ParseDerSignature(derSignature);

        using var inner = new MemoryStream();
        SshDataHelper.WriteMpint(inner, r);
        SshDataHelper.WriteMpint(inner, s);

        using var outer = new MemoryStream();
        SshDataHelper.WriteString(outer, "ecdsa-sha2-nistp256");
        SshDataHelper.WriteBytes(outer, inner.ToArray());
        return outer.ToArray();
    }

    private static (byte[] r, byte[] s) ParseDerSignature(byte[] der)
    {
        // DER: 0x30 <len> 0x02 <rlen> <r> 0x02 <slen> <s>
        int offset = 2; // skip 0x30 + length
        if (der[1] > 0x80) offset += der[1] - 0x80; // long form length

        // r
        if (der[offset++] != 0x02) throw new InvalidDataException("Bad DER");
        int rLen = der[offset++];
        var r = new byte[rLen];
        Buffer.BlockCopy(der, offset, r, 0, rLen);
        offset += rLen;

        // s
        if (der[offset++] != 0x02) throw new InvalidDataException("Bad DER");
        int sLen = der[offset++];
        var s = new byte[sLen];
        Buffer.BlockCopy(der, offset, s, 0, sLen);

        return (r, s);
    }

    private void DeriveKeys(byte[] sharedSecret, byte[] exchangeHash)
    {
        int keyLen = _chosenCipher == "aes256-ctr" ? 32 : 16;
        int ivLen = 16;
        int macKeyLen = 32;

        var ivC2S = DeriveKey(sharedSecret, exchangeHash, (byte)'A', ivLen);
        var ivS2C = DeriveKey(sharedSecret, exchangeHash, (byte)'B', ivLen);
        var keyC2S = DeriveKey(sharedSecret, exchangeHash, (byte)'C', keyLen);
        var keyS2C = DeriveKey(sharedSecret, exchangeHash, (byte)'D', keyLen);
        var macKeyC2S = DeriveKey(sharedSecret, exchangeHash, (byte)'E', macKeyLen);
        var macKeyS2C = DeriveKey(sharedSecret, exchangeHash, (byte)'F', macKeyLen);

        _decryptCipher = new AesCtrCipher(keyC2S, ivC2S);
        _encryptCipher = new AesCtrCipher(keyS2C, ivS2C);
        _decryptMac = new HMACSHA256(macKeyC2S);
        _encryptMac = new HMACSHA256(macKeyS2C);
        _cipherBlockSize = 16;
        _macLength = 32;
    }

    private byte[] DeriveKey(byte[] K, byte[] H, byte letter, int needed)
    {
        // K1 = HASH(K || H || X || session_id)
        using var ms = new MemoryStream();
        SshDataHelper.WriteMpint(ms, K);
        ms.Write(H);
        ms.WriteByte(letter);
        ms.Write(_sessionId!);

        var result = SHA256.HashData(ms.ToArray());

        // Extend if needed (for AES-256 key, SHA-256 output is exactly 32 bytes, so no extension needed)
        if (result.Length >= needed)
            return result[..needed];

        // Extension: K2 = HASH(K || H || K1)
        using var ms2 = new MemoryStream();
        SshDataHelper.WriteMpint(ms2, K);
        ms2.Write(H);
        ms2.Write(result);
        var k2 = SHA256.HashData(ms2.ToArray());

        var extended = new byte[result.Length + k2.Length];
        Buffer.BlockCopy(result, 0, extended, 0, result.Length);
        Buffer.BlockCopy(k2, 0, extended, result.Length, k2.Length);
        return extended[..needed];
    }

    // ─── Service Request ─────────────────────────────────────────────────

    private async Task HandleServiceRequest(CancellationToken ct)
    {
        var packet = await ReadPacket(ct);
        if (packet[0] != SSH_MSG_SERVICE_REQUEST)
            throw new InvalidOperationException($"Expected SERVICE_REQUEST, got {packet[0]}");

        var reader = new SshDataReader(packet, 1);
        var serviceName = reader.ReadString();

        if (serviceName != "ssh-userauth")
            throw new InvalidOperationException($"Unsupported service: {serviceName}");

        using var reply = new MemoryStream();
        reply.WriteByte(SSH_MSG_SERVICE_ACCEPT);
        SshDataHelper.WriteString(reply, "ssh-userauth");
        await SendPacket(reply.ToArray(), ct);
    }

    // ─── Authentication ──────────────────────────────────────────────────

    private async Task HandleAuthentication(CancellationToken ct)
    {
        while (true)
        {
            var packet = await ReadPacket(ct);
            if (packet[0] != SSH_MSG_USERAUTH_REQUEST)
            {
                if (packet[0] == SSH_MSG_IGNORE || packet[0] == SSH_MSG_UNIMPLEMENTED) continue;
                throw new InvalidOperationException($"Expected USERAUTH_REQUEST, got {packet[0]}");
            }

            var reader = new SshDataReader(packet, 1);
            var username = reader.ReadString();
            var serviceName = reader.ReadString();
            var methodName = reader.ReadString();

            if (methodName == "none")
            {
                await SendAuthFailure(ct);
                continue;
            }

            if (methodName != "publickey")
            {
                await SendAuthFailure(ct);
                continue;
            }

            var hasSig = reader.ReadBool();
            var algName = reader.ReadString();
            var pubKeyBlob = reader.ReadBinary();

            // Compute fingerprint and look up the key
            var fingerprint = ComputeKeyFingerprint(pubKeyBlob);
            var keyOwner = await _sshAuth.AuthenticateByFingerprintAsync(fingerprint);

            if (keyOwner == null)
            {
                await SendAuthFailure(ct);
                continue;
            }

            if (!hasSig)
            {
                // Client is asking if this key is acceptable — reply PK_OK
                using var pkOk = new MemoryStream();
                pkOk.WriteByte(SSH_MSG_USERAUTH_PK_OK);
                SshDataHelper.WriteString(pkOk, algName);
                SshDataHelper.WriteBytes(pkOk, pubKeyBlob);
                await SendPacket(pkOk.ToArray(), ct);
                continue;
            }

            // Verify the signature
            var sigBlob = reader.ReadBinary();
            var dataToVerify = BuildAuthSignedData(
                _sessionId!, username, serviceName, algName, pubKeyBlob);

            if (!VerifyPublicKeySignature(algName, pubKeyBlob, sigBlob, dataToVerify))
            {
                _logger.LogWarning("SSH auth signature verification failed for user {User} with {Alg}", username, algName);
                await SendAuthFailure(ct);
                continue;
            }

            // Authentication successful
            _authenticatedUser = keyOwner;
            _authKeyFingerprint = fingerprint;
            _logger.LogInformation("SSH user {User} authenticated via {Alg} key", keyOwner, algName);

            await SendPacket(new[] { SSH_MSG_USERAUTH_SUCCESS }, ct);
            return;
        }
    }

    private async Task SendAuthFailure(CancellationToken ct)
    {
        using var ms = new MemoryStream();
        ms.WriteByte(SSH_MSG_USERAUTH_FAILURE);
        SshDataHelper.WriteString(ms, "publickey");
        ms.WriteByte(0); // partial success = false
        await SendPacket(ms.ToArray(), ct);
    }

    private byte[] BuildAuthSignedData(byte[] sessionId, string username, string service, string algName, byte[] pubKeyBlob)
    {
        using var ms = new MemoryStream();
        SshDataHelper.WriteBytes(ms, sessionId);
        ms.WriteByte(SSH_MSG_USERAUTH_REQUEST);
        SshDataHelper.WriteString(ms, username);
        SshDataHelper.WriteString(ms, service);
        SshDataHelper.WriteString(ms, "publickey");
        ms.WriteByte(1); // TRUE
        SshDataHelper.WriteString(ms, algName);
        SshDataHelper.WriteBytes(ms, pubKeyBlob);
        return ms.ToArray();
    }

    private static string ComputeKeyFingerprint(byte[] keyBlob)
    {
        var hash = SHA256.HashData(keyBlob);
        return "SHA256:" + Convert.ToBase64String(hash).TrimEnd('=');
    }

    private bool VerifyPublicKeySignature(string algName, byte[] pubKeyBlob, byte[] sigBlob, byte[] data)
    {
        try
        {
            var sigReader = new SshDataReader(sigBlob, 0);
            var sigAlg = sigReader.ReadString();
            var sigData = sigReader.ReadBinary();

            if (algName.StartsWith("ecdsa-sha2-"))
            {
                return VerifyEcdsaSignature(pubKeyBlob, sigData, data);
            }
            else if (algName == "ssh-ed25519")
            {
                return VerifyEd25519Signature(pubKeyBlob, sigData, data);
            }
            else if (algName == "ssh-rsa" || algName == "rsa-sha2-256" || algName == "rsa-sha2-512")
            {
                var hashAlg = sigAlg switch
                {
                    "rsa-sha2-512" => HashAlgorithmName.SHA512,
                    "rsa-sha2-256" => HashAlgorithmName.SHA256,
                    _ => HashAlgorithmName.SHA1
                };
                return VerifyRsaSignature(pubKeyBlob, sigData, data, hashAlg);
            }

            _logger.LogWarning("Unsupported key algorithm for verification: {Alg}", algName);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Signature verification error");
            return false;
        }
    }

    private static bool VerifyRsaSignature(byte[] pubKeyBlob, byte[] signature, byte[] data, HashAlgorithmName hashAlg)
    {
        var reader = new SshDataReader(pubKeyBlob, 0);
        var keyType = reader.ReadString(); // "ssh-rsa"
        var e = reader.ReadBinary();
        var n = reader.ReadBinary();

        using var rsa = RSA.Create();
        rsa.ImportParameters(new RSAParameters
        {
            Exponent = e,
            Modulus = n
        });

        return rsa.VerifyData(data, signature, hashAlg, RSASignaturePadding.Pkcs1);
    }

    private static bool VerifyEcdsaSignature(byte[] pubKeyBlob, byte[] sigData, byte[] data)
    {
        var reader = new SshDataReader(pubKeyBlob, 0);
        var keyType = reader.ReadString(); // "ecdsa-sha2-nistp256"
        var curveName = reader.ReadString();
        var qBytes = reader.ReadBinary();

        var curve = curveName switch
        {
            "nistp256" => ECCurve.NamedCurves.nistP256,
            "nistp384" => ECCurve.NamedCurves.nistP384,
            "nistp521" => ECCurve.NamedCurves.nistP521,
            _ => throw new NotSupportedException($"Unsupported curve: {curveName}")
        };

        var hashAlg = curveName switch
        {
            "nistp256" => HashAlgorithmName.SHA256,
            "nistp384" => HashAlgorithmName.SHA384,
            "nistp521" => HashAlgorithmName.SHA512,
            _ => HashAlgorithmName.SHA256
        };

        var point = ParseEcPoint(qBytes);

        using var ecdsa = ECDsa.Create(new ECParameters { Curve = curve, Q = point });

        // sigData is SSH-encoded: mpint r + mpint s — convert to DER
        var sigReader = new SshDataReader(sigData, 0);
        var r = sigReader.ReadBinary();
        var s = sigReader.ReadBinary();
        var derSig = EncodeDerSignature(r, s);

        return ecdsa.VerifyData(data, derSig, hashAlg, DSASignatureFormat.Rfc3279DerSequence);
    }

    private static bool VerifyEd25519Signature(byte[] pubKeyBlob, byte[] signature, byte[] data)
    {
        // SSH Ed25519 public key blob: string "ssh-ed25519" + string <32-byte public key>
        var reader = new SshDataReader(pubKeyBlob, 0);
        var keyType = reader.ReadString(); // "ssh-ed25519"
        var rawPubKey = reader.ReadBinary(); // 32 bytes

        // Ed25519 signature is 64 bytes
        var pubKeyParams = new Org.BouncyCastle.Crypto.Parameters.Ed25519PublicKeyParameters(rawPubKey, 0);
        var verifier = new Org.BouncyCastle.Crypto.Signers.Ed25519Signer();
        verifier.Init(false, pubKeyParams);
        verifier.BlockUpdate(data, 0, data.Length);
        return verifier.VerifySignature(signature);
    }

    private static byte[] EncodeDerSignature(byte[] r, byte[] s)
    {
        static byte[] DerInteger(byte[] value)
        {
            // Trim leading zeros but ensure positive (add 0x00 if high bit set)
            int start = 0;
            while (start < value.Length - 1 && value[start] == 0) start++;
            var trimmed = value[start..];
            bool needsPad = (trimmed[0] & 0x80) != 0;
            var result = new byte[(needsPad ? 1 : 0) + trimmed.Length + 2];
            result[0] = 0x02;
            result[1] = (byte)(trimmed.Length + (needsPad ? 1 : 0));
            if (needsPad) result[2] = 0;
            Buffer.BlockCopy(trimmed, 0, result, needsPad ? 3 : 2, trimmed.Length);
            return result;
        }

        var rDer = DerInteger(r);
        var sDer = DerInteger(s);
        var seq = new byte[2 + rDer.Length + sDer.Length];
        seq[0] = 0x30;
        seq[1] = (byte)(rDer.Length + sDer.Length);
        Buffer.BlockCopy(rDer, 0, seq, 2, rDer.Length);
        Buffer.BlockCopy(sDer, 0, seq, 2 + rDer.Length, sDer.Length);
        return seq;
    }

    // ─── Channel Management ──────────────────────────────────────────────

    private async Task HandleChannels(CancellationToken ct)
    {
        // Increase timeouts for data transfer phase
        _client.ReceiveTimeout = 0;
        _client.SendTimeout = 0;

        while (true)
        {
            var packet = await ReadPacket(ct);
            var msgType = packet[0];

            switch (msgType)
            {
                case SSH_MSG_CHANNEL_OPEN:
                    await HandleChannelOpen(packet, ct);
                    break;

                case SSH_MSG_CHANNEL_REQUEST:
                    await HandleChannelRequest(packet, ct);
                    break;

                case SSH_MSG_CHANNEL_DATA:
                case SSH_MSG_CHANNEL_EXTENDED_DATA:
                case SSH_MSG_CHANNEL_WINDOW_ADJUST:
                case SSH_MSG_CHANNEL_EOF:
                    // These are handled during git command execution
                    break;

                case SSH_MSG_CHANNEL_CLOSE:
                    return; // Session complete

                case SSH_MSG_DISCONNECT:
                    throw new SshDisconnectException();

                case SSH_MSG_IGNORE:
                case SSH_MSG_UNIMPLEMENTED:
                    break;

                default:
                    _logger.LogDebug("Unhandled SSH message type: {Type}", msgType);
                    break;
            }
        }
    }

    // Channel state
    private uint _clientChannelId;
    private uint _serverChannelId;
    private uint _clientWindowSize;
    private uint _clientMaxPacket = 32768;

    private async Task HandleChannelOpen(byte[] packet, CancellationToken ct)
    {
        var reader = new SshDataReader(packet, 1);
        var channelType = reader.ReadString();
        _clientChannelId = reader.ReadUint32();
        _clientWindowSize = reader.ReadUint32();
        _clientMaxPacket = reader.ReadUint32();

        if (channelType != "session")
        {
            _logger.LogWarning("Unsupported channel type: {Type}", channelType);
            return;
        }

        _serverChannelId = 0;

        using var reply = new MemoryStream();
        reply.WriteByte(SSH_MSG_CHANNEL_OPEN_CONFIRMATION);
        SshDataHelper.WriteUint32(reply, _clientChannelId);   // recipient channel
        SshDataHelper.WriteUint32(reply, _serverChannelId);   // sender channel
        SshDataHelper.WriteUint32(reply, 2097152);             // initial window size (2MB)
        SshDataHelper.WriteUint32(reply, 32768);               // maximum packet size
        await SendPacket(reply.ToArray(), ct);
    }

    private async Task HandleChannelRequest(byte[] packet, CancellationToken ct)
    {
        var reader = new SshDataReader(packet, 1);
        var channelId = reader.ReadUint32();
        var requestType = reader.ReadString();
        var wantReply = reader.ReadBool();

        if (requestType == "exec")
        {
            var command = reader.ReadString();

            if (wantReply)
                await SendPacket(BuildChannelSuccess(channelId), ct);

            await ExecuteGitCommand(command, channelId, ct);
        }
        else if (requestType == "env")
        {
            // Ignore environment variable requests
            if (wantReply)
                await SendPacket(BuildChannelFailure(channelId), ct);
        }
        else
        {
            if (wantReply)
                await SendPacket(BuildChannelFailure(channelId), ct);
        }
    }

    private static byte[] BuildChannelSuccess(uint channelId)
    {
        using var ms = new MemoryStream();
        ms.WriteByte(SSH_MSG_CHANNEL_SUCCESS);
        SshDataHelper.WriteUint32(ms, channelId);
        return ms.ToArray();
    }

    private static byte[] BuildChannelFailure(uint channelId)
    {
        using var ms = new MemoryStream();
        ms.WriteByte(SSH_MSG_CHANNEL_FAILURE);
        SshDataHelper.WriteUint32(ms, channelId);
        return ms.ToArray();
    }

    // ─── Git Command Execution ───────────────────────────────────────────

    private async Task ExecuteGitCommand(string command, uint channelId, CancellationToken ct)
    {
        _logger.LogDebug("SSH exec: {Command} by {User}", command, _authenticatedUser);

        // Parse command: git-upload-pack 'repo.git' or git-receive-pack 'repo.git'
        var (operation, repoPath) = ParseGitCommand(command);
        if (operation == null || repoPath == null)
        {
            await SendChannelData(channelId,
                Encoding.UTF8.GetBytes("Error: Only git operations are allowed over SSH.\n"), ct);
            await SendExitStatus(channelId, 1, ct);
            await SendChannelEofAndClose(channelId, ct);
            return;
        }

        // Check access permissions
        var repoName = repoPath.TrimEnd('/');
        if (repoName.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
            repoName = repoName[..^4];

        // Security: reject path traversal — resolve and verify the path stays within project root
        var fullCheck = Path.GetFullPath(Path.Combine(_projectRoot, repoName));
        var rootCheck = Path.GetFullPath(_projectRoot);
        if (!fullCheck.StartsWith(rootCheck, StringComparison.OrdinalIgnoreCase)
            || repoName.Contains("..") || repoName.Contains('/') || repoName.Contains('\\'))
        {
            await SendChannelData(channelId,
                Encoding.UTF8.GetBytes("Error: Invalid repository path.\n"), ct);
            await SendExitStatus(channelId, 1, ct);
            await SendChannelEofAndClose(channelId, ct);
            return;
        }

        var allowed = await CheckAccess(_authenticatedUser!, repoName, operation);
        if (!allowed)
        {
            await SendChannelData(channelId,
                Encoding.UTF8.GetBytes($"Error: Access denied to repository '{repoName}'.\n"), ct);
            await SendExitStatus(channelId, 1, ct);
            await SendChannelEofAndClose(channelId, ct);
            return;
        }

        // Ensure repo directory exists (for push to new repo)
        var fullRepoPath = Path.Combine(_projectRoot, repoPath);
        if (!fullRepoPath.EndsWith(".git"))
            fullRepoPath += ".git";

        if (operation == "git-receive-pack" && !Directory.Exists(fullRepoPath))
        {
            // Auto-init bare repo for push
            LibGit2Sharp.Repository.Init(fullRepoPath, true);
            _logger.LogInformation("Auto-created bare repo at {Path} via SSH push by {User}", fullRepoPath, _authenticatedUser);
        }

        if (!Directory.Exists(fullRepoPath))
        {
            await SendChannelData(channelId,
                Encoding.UTF8.GetBytes($"Error: Repository '{repoName}' not found.\n"), ct);
            await SendExitStatus(channelId, 1, ct);
            await SendChannelEofAndClose(channelId, ct);
            return;
        }

        // Track git operation
        if (operation == "git-upload-pack")
            GitOperationCounters.Increment("fetch");
        else
            GitOperationCounters.Increment("push");

        // Spawn git process
        var psi = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = $"{operation} \"{fullRepoPath}\"",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        if (process == null)
        {
            await SendChannelData(channelId,
                Encoding.UTF8.GetBytes("Error: Failed to start git process.\n"), ct);
            await SendExitStatus(channelId, 1, ct);
            await SendChannelEofAndClose(channelId, ct);
            return;
        }

        // Pipe data between SSH channel and git process
        var readFromClient = PipeClientToProcess(process, ct);
        var readFromProcess = PipeProcessToClient(process, channelId, ct);
        var readStderr = PipeStderrToClient(process, channelId, ct);

        await Task.WhenAll(readFromProcess, readStderr);

        try { await process.WaitForExitAsync(ct); } catch { }
        try { await readFromClient; } catch { }

        var exitCode = process.ExitCode;

        // After successful push, process issue auto-close
        if (exitCode == 0 && operation == "git-receive-pack")
        {
            try
            {
                await ProcessPostPush(fullRepoPath, repoName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to process post-push hooks");
            }
        }

        await SendExitStatus(channelId, exitCode, ct);
        await SendChannelEofAndClose(channelId, ct);
    }

    private async Task PipeClientToProcess(Process process, CancellationToken ct)
    {
        try
        {
            var buf = new byte[32768];
            while (!ct.IsCancellationRequested && !process.HasExited)
            {
                var packet = await ReadPacket(ct);
                if (packet[0] == SSH_MSG_CHANNEL_DATA)
                {
                    var reader = new SshDataReader(packet, 1);
                    reader.ReadUint32(); // channel id
                    var data = reader.ReadBinary();
                    await process.StandardInput.BaseStream.WriteAsync(data, ct);
                    await process.StandardInput.BaseStream.FlushAsync(ct);
                }
                else if (packet[0] == SSH_MSG_CHANNEL_EOF || packet[0] == SSH_MSG_CHANNEL_CLOSE)
                {
                    break;
                }
                else if (packet[0] == SSH_MSG_CHANNEL_WINDOW_ADJUST)
                {
                    var reader = new SshDataReader(packet, 1);
                    reader.ReadUint32(); // channel id
                    var bytesToAdd = reader.ReadUint32();
                    _clientWindowSize += bytesToAdd;
                }
                else if (packet[0] == SSH_MSG_DISCONNECT)
                {
                    break;
                }
            }
        }
        catch (Exception) { /* connection closed */ }
        finally
        {
            try { process.StandardInput.Close(); } catch { }
        }
    }

    private async Task PipeProcessToClient(Process process, uint channelId, CancellationToken ct)
    {
        try
        {
            var buf = new byte[32768];
            int read;
            while ((read = await process.StandardOutput.BaseStream.ReadAsync(buf, ct)) > 0)
            {
                var data = buf[..read];
                await SendChannelData(channelId, data, ct);
            }
        }
        catch (Exception) { }
    }

    private async Task PipeStderrToClient(Process process, uint channelId, CancellationToken ct)
    {
        try
        {
            var buf = new byte[32768];
            int read;
            while ((read = await process.StandardError.BaseStream.ReadAsync(buf, ct)) > 0)
            {
                var data = buf[..read];
                await SendChannelExtendedData(channelId, 1, data, ct); // type 1 = stderr
            }
        }
        catch (Exception) { }
    }

    private async Task ProcessPostPush(string repoDir, string repoName)
    {
        if (!LibGit2Sharp.Repository.IsValid(repoDir)) return;

        using var repo = new LibGit2Sharp.Repository(repoDir);
        var defaultBranch = repo.Branches["main"] ?? repo.Branches["master"] ?? repo.Head;
        if (defaultBranch?.Tip == null) return;

        var filter = new CommitFilter
        {
            IncludeReachableFrom = defaultBranch.Tip,
            SortBy = CommitSortStrategies.Time
        };

        var branch = defaultBranch.FriendlyName;
        var sha = defaultBranch.Tip.Sha;
        var message = defaultBranch.Tip.MessageShort;
        var user = _authenticatedUser ?? "system";

        foreach (var commit in repo.Commits.QueryBy(filter).Take(20))
        {
            await _issueAutoCloseService.ProcessCommitMessage(repoName, commit.Message, commit.Sha, user);
        }

        // Trigger workflows that listen for push events
        await _workflowService.TriggerPushWorkflowsAsync(repoName, repoDir, branch, sha, message, user);

        // AGit Flow: detect pushes to refs/for/* and create PRs
        try
        {
            foreach (var reference in repo.Refs.Where(r => r.CanonicalName.StartsWith("refs/for/")))
            {
                var prNumber = await _agitFlowService.ProcessAGitPushAsync(repoDir, repoName, reference.CanonicalName, user);
                if (prNumber.HasValue)
                    _logger.LogInformation("AGit: created/updated PR #{Number} for {Ref}", prNumber.Value, reference.CanonicalName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to process AGit flow via SSH");
        }
    }

    private async Task<bool> CheckAccess(string username, string repoName, string operation)
    {
        // Check deploy key first
        if (!string.IsNullOrEmpty(_authKeyFingerprint))
        {
            var deployResult = await _sshAuth.AuthenticateDeployKeyByFingerprintAsync(_authKeyFingerprint, repoName);
            if (deployResult != null)
            {
                if (deployResult.ReadOnly && operation == "git-receive-pack")
                    return false;
                return true;
            }
        }

        var repo = await _repoService.GetRepositoryAsync(repoName);

        // New repo — allow if creating via push
        if (repo == null)
            return operation == "git-receive-pack";

        // Archived repos block pushes
        if (repo.IsArchived && operation == "git-receive-pack")
            return false;

        // Owner always has access
        if (repo.Owner.Equals(username, StringComparison.OrdinalIgnoreCase))
            return true;

        // For read operations on public repos, always allow
        if (operation == "git-upload-pack" && !repo.IsPrivate)
            return true;

        // Check collaborator permissions
        var requiredPerm = operation == "git-receive-pack"
            ? Models.CollaboratorPermission.Write
            : Models.CollaboratorPermission.Read;

        return await _collaboratorService.HasPermissionAsync(repoName, username, requiredPerm);
    }

    private static (string? operation, string? repoPath) ParseGitCommand(string command)
    {
        // Formats:
        //   git-upload-pack 'repo.git'
        //   git-upload-pack '/repo.git'
        //   git-receive-pack 'repo.git'
        command = command.Trim();

        string? op = null;
        int argStart;
        if (command.StartsWith("git-upload-pack "))
        { op = "git-upload-pack"; argStart = "git-upload-pack ".Length; }
        else if (command.StartsWith("git-receive-pack "))
        { op = "git-receive-pack"; argStart = "git-receive-pack ".Length; }
        else if (command.StartsWith("git upload-pack "))
        { op = "git-upload-pack"; argStart = "git upload-pack ".Length; }
        else if (command.StartsWith("git receive-pack "))
        { op = "git-receive-pack"; argStart = "git receive-pack ".Length; }
        else
            return (null, null);

        var rest = command[argStart..].Trim();
        // Remove quotes and leading slash
        rest = rest.Trim('\'', '"').TrimStart('/');

        if (string.IsNullOrEmpty(rest))
            return (null, null);

        return (op, rest);
    }

    // ─── Channel Data Helpers ────────────────────────────────────────────

    private async Task SendChannelData(uint channelId, byte[] data, CancellationToken ct)
    {
        int offset = 0;
        while (offset < data.Length)
        {
            var chunkSize = Math.Min(data.Length - offset, (int)Math.Min(_clientMaxPacket - 100, 32000));
            using var ms = new MemoryStream();
            ms.WriteByte(SSH_MSG_CHANNEL_DATA);
            SshDataHelper.WriteUint32(ms, _clientChannelId);
            var chunk = new byte[chunkSize];
            Buffer.BlockCopy(data, offset, chunk, 0, chunkSize);
            SshDataHelper.WriteBytes(ms, chunk);
            await SendPacket(ms.ToArray(), ct);
            offset += chunkSize;
        }
    }

    private async Task SendChannelExtendedData(uint channelId, uint dataType, byte[] data, CancellationToken ct)
    {
        using var ms = new MemoryStream();
        ms.WriteByte(SSH_MSG_CHANNEL_EXTENDED_DATA);
        SshDataHelper.WriteUint32(ms, _clientChannelId);
        SshDataHelper.WriteUint32(ms, dataType);
        SshDataHelper.WriteBytes(ms, data);
        await SendPacket(ms.ToArray(), ct);
    }

    private async Task SendExitStatus(uint channelId, int exitCode, CancellationToken ct)
    {
        using var ms = new MemoryStream();
        ms.WriteByte(SSH_MSG_CHANNEL_REQUEST);
        SshDataHelper.WriteUint32(ms, _clientChannelId);
        SshDataHelper.WriteString(ms, "exit-status");
        ms.WriteByte(0); // want_reply = false
        SshDataHelper.WriteUint32(ms, (uint)exitCode);
        await SendPacket(ms.ToArray(), ct);
    }

    private async Task SendChannelEofAndClose(uint channelId, CancellationToken ct)
    {
        // EOF
        using var eof = new MemoryStream();
        eof.WriteByte(SSH_MSG_CHANNEL_EOF);
        SshDataHelper.WriteUint32(eof, _clientChannelId);
        await SendPacket(eof.ToArray(), ct);

        // Close
        using var close = new MemoryStream();
        close.WriteByte(SSH_MSG_CHANNEL_CLOSE);
        SshDataHelper.WriteUint32(close, _clientChannelId);
        await SendPacket(close.ToArray(), ct);
    }

    private async Task SendDisconnect(uint reasonCode, string description)
    {
        using var ms = new MemoryStream();
        ms.WriteByte(SSH_MSG_DISCONNECT);
        SshDataHelper.WriteUint32(ms, reasonCode);
        SshDataHelper.WriteString(ms, description);
        SshDataHelper.WriteString(ms, ""); // language tag
        try { await SendPacket(ms.ToArray(), CancellationToken.None); } catch { }
    }

    // ─── Packet I/O ──────────────────────────────────────────────────────

    private async Task<byte[]> ReadPacket(CancellationToken ct)
    {
        if (!_encrypted)
        {
            // Read 4-byte length
            var lenBuf = await ReadExact(4, ct);
            var packetLen = (int)SshDataHelper.ReadUint32(lenBuf, 0);

            if (packetLen < 1 || packetLen > 256 * 1024)
                throw new InvalidOperationException($"Invalid packet length: {packetLen}");

            var rest = await ReadExact(packetLen, ct);
            var paddingLen = rest[0];
            var payloadLen = packetLen - paddingLen - 1;

            _recvSeq++;
            return rest[1..(1 + payloadLen)];
        }
        else
        {
            // Read first block to get packet length
            var firstBlock = await ReadExact(_cipherBlockSize, ct);
            _decryptCipher!.Process(firstBlock, 0, firstBlock.Length);

            var packetLen = (int)SshDataHelper.ReadUint32(firstBlock, 0);
            if (packetLen < 1 || packetLen > 256 * 1024)
                throw new InvalidOperationException($"Invalid encrypted packet length: {packetLen}");

            // Read remaining encrypted data
            var remaining = packetLen + 4 - _cipherBlockSize;
            byte[] restEnc = Array.Empty<byte>();
            if (remaining > 0)
            {
                restEnc = await ReadExact(remaining, ct);
                _decryptCipher.Process(restEnc, 0, restEnc.Length);
            }

            // Read MAC
            var mac = await ReadExact(_macLength, ct);

            // Verify MAC
            var fullPacket = new byte[4 + packetLen];
            Buffer.BlockCopy(firstBlock, 0, fullPacket, 0, firstBlock.Length);
            if (restEnc.Length > 0)
                Buffer.BlockCopy(restEnc, 0, fullPacket, firstBlock.Length, restEnc.Length);

            var seqBuf = new byte[4];
            SshDataHelper.WriteUint32Be(seqBuf, 0, _recvSeq);
            var macInput = new byte[4 + fullPacket.Length];
            Buffer.BlockCopy(seqBuf, 0, macInput, 0, 4);
            Buffer.BlockCopy(fullPacket, 0, macInput, 4, fullPacket.Length);
            var expectedMac = _decryptMac!.ComputeHash(macInput);

            if (!CryptographicOperations.FixedTimeEquals(mac, expectedMac))
                throw new InvalidOperationException("MAC verification failed");

            var paddingLen = fullPacket[4];
            var payloadLen = packetLen - paddingLen - 1;

            _recvSeq++;
            return fullPacket[5..(5 + payloadLen)];
        }
    }

    private async Task SendPacket(byte[] payload, CancellationToken ct)
    {
        var blockSize = _encrypted ? _cipherBlockSize : 8;

        // padding: packet must be multiple of block size (minimum 4 bytes)
        var packetLen = 1 + payload.Length; // padding_length(1) + payload
        var paddingLen = blockSize - ((4 + packetLen) % blockSize);
        if (paddingLen < 4) paddingLen += blockSize;
        packetLen += paddingLen;

        var packet = new byte[4 + packetLen];
        SshDataHelper.WriteUint32Be(packet, 0, (uint)packetLen);
        packet[4] = (byte)paddingLen;
        Buffer.BlockCopy(payload, 0, packet, 5, payload.Length);
        var padding = RandomNumberGenerator.GetBytes(paddingLen);
        Buffer.BlockCopy(padding, 0, packet, 5 + payload.Length, paddingLen);

        if (!_encrypted)
        {
            await _stream.WriteAsync(packet, ct);
            _sendSeq++;
        }
        else
        {
            // Compute MAC before encryption (encrypt-then-mac is not used; standard SSH uses encrypt-and-mac)
            // Actually SSH uses MAC over unencrypted packet
            var seqBuf = new byte[4];
            SshDataHelper.WriteUint32Be(seqBuf, 0, _sendSeq);
            var macInput = new byte[4 + packet.Length];
            Buffer.BlockCopy(seqBuf, 0, macInput, 0, 4);
            Buffer.BlockCopy(packet, 0, macInput, 4, packet.Length);
            var mac = _encryptMac!.ComputeHash(macInput);

            // Encrypt
            _encryptCipher!.Process(packet, 0, packet.Length);

            // Send encrypted packet + MAC
            await _stream.WriteAsync(packet, ct);
            await _stream.WriteAsync(mac, ct);
            _sendSeq++;
        }

        await _stream.FlushAsync(ct);
    }

    private async Task<byte[]> ReadExact(int count, CancellationToken ct)
    {
        var buf = new byte[count];
        int offset = 0;
        while (offset < count)
        {
            int read = await _stream.ReadAsync(buf.AsMemory(offset, count - offset), ct);
            if (read <= 0)
                throw new IOException("Connection closed");
            offset += read;
        }
        return buf;
    }

    // ─── EC Point Parsing ────────────────────────────────────────────────

    private static ECPoint ParseEcPoint(byte[] data)
    {
        if (data[0] != 0x04)
            throw new InvalidOperationException("Only uncompressed EC points are supported");

        int coordLen = (data.Length - 1) / 2;
        var x = new byte[coordLen];
        var y = new byte[coordLen];
        Buffer.BlockCopy(data, 1, x, 0, coordLen);
        Buffer.BlockCopy(data, 1 + coordLen, y, 0, coordLen);
        return new ECPoint { X = x, Y = y };
    }

    public void Dispose()
    {
        _encryptCipher?.Dispose();
        _decryptCipher?.Dispose();
        _encryptMac?.Dispose();
        _decryptMac?.Dispose();
    }
}

/// <summary>
/// AES in Counter (CTR) mode — not provided by .NET directly.
/// Encrypts a counter block with AES-ECB, then XORs with plaintext.
/// </summary>
internal sealed class AesCtrCipher : IDisposable
{
    private readonly Aes _aes;
    private readonly ICryptoTransform _encryptor;
    private readonly byte[] _counter;
    private readonly byte[] _keystreamBlock = new byte[16];
    private int _keystreamOffset = 16;

    public AesCtrCipher(byte[] key, byte[] iv)
    {
        _aes = Aes.Create();
        _aes.Mode = CipherMode.ECB;
        _aes.Padding = PaddingMode.None;
        _aes.Key = key;
        _encryptor = _aes.CreateEncryptor();
        _counter = (byte[])iv.Clone();
    }

    public void Process(byte[] data, int offset, int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (_keystreamOffset >= 16)
            {
                _encryptor.TransformBlock(_counter, 0, 16, _keystreamBlock, 0);
                IncrementCounter();
                _keystreamOffset = 0;
            }
            data[offset + i] ^= _keystreamBlock[_keystreamOffset++];
        }
    }

    private void IncrementCounter()
    {
        for (int i = 15; i >= 0; i--)
        {
            if (++_counter[i] != 0) break;
        }
    }

    public void Dispose()
    {
        _encryptor.Dispose();
        _aes.Dispose();
    }
}

/// <summary>
/// SSH data encoding/decoding helpers per RFC 4251.
/// </summary>
internal static class SshDataHelper
{
    public static void WriteString(Stream s, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        WriteBytes(s, bytes);
    }

    public static void WriteBytes(Stream s, byte[] data)
    {
        WriteUint32(s, (uint)data.Length);
        s.Write(data);
    }

    public static void WriteUint32(Stream s, uint value)
    {
        s.WriteByte((byte)(value >> 24));
        s.WriteByte((byte)(value >> 16));
        s.WriteByte((byte)(value >> 8));
        s.WriteByte((byte)value);
    }

    public static void WriteUint32Be(byte[] buf, int offset, uint value)
    {
        buf[offset] = (byte)(value >> 24);
        buf[offset + 1] = (byte)(value >> 16);
        buf[offset + 2] = (byte)(value >> 8);
        buf[offset + 3] = (byte)value;
    }

    public static uint ReadUint32(byte[] buf, int offset)
    {
        return (uint)(buf[offset] << 24 | buf[offset + 1] << 16 | buf[offset + 2] << 8 | buf[offset + 3]);
    }

    public static void WriteMpint(Stream s, byte[] value)
    {
        // SSH mpint: strip leading zeros, add 0x00 prefix if high bit is set
        int start = 0;
        while (start < value.Length - 1 && value[start] == 0) start++;
        var trimmed = value[start..];

        if ((trimmed[0] & 0x80) != 0)
        {
            WriteUint32(s, (uint)(trimmed.Length + 1));
            s.WriteByte(0);
        }
        else
        {
            WriteUint32(s, (uint)trimmed.Length);
        }
        s.Write(trimmed);
    }
}

/// <summary>
/// Sequential reader for SSH wire-format data.
/// </summary>
internal sealed class SshDataReader
{
    private readonly byte[] _data;
    private int _pos;

    public SshDataReader(byte[] data, int offset)
    {
        _data = data;
        _pos = offset;
    }

    public uint ReadUint32()
    {
        var val = SshDataHelper.ReadUint32(_data, _pos);
        _pos += 4;
        return val;
    }

    public string ReadString()
    {
        var len = (int)ReadUint32();
        var val = Encoding.UTF8.GetString(_data, _pos, len);
        _pos += len;
        return val;
    }

    public byte[] ReadBinary()
    {
        var len = (int)ReadUint32();
        var val = new byte[len];
        Buffer.BlockCopy(_data, _pos, val, 0, len);
        _pos += len;
        return val;
    }

    public bool ReadBool()
    {
        return _data[_pos++] != 0;
    }
}

internal class SshDisconnectException : Exception
{
    public SshDisconnectException() : base("SSH disconnect") { }
}
