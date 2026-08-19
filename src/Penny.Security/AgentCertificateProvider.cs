using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Penny.Security;

/// <summary>
/// Provides the self-signed TLS certificate the Agent uses to terminate
/// SslStream connections. Penny does not rely on a public CA — the Controller
/// does not validate the cert against a trust store, because Penny sessions
/// are authenticated by DeviceId + PIN + explicit approval, not by PKI identity.
///
/// TLS here exists purely to give every byte on the wire confidentiality and
/// integrity (protection from passive LAN sniffing / tampering) on top of that
/// application-level authorization — it is transport hardening, not identity.
///
/// The certificate is regenerated per install and cached under the Agent's
/// portable config folder (see PortableConfigPaths) so the same identity is
/// reused across runs from the same USB drive rather than regenerated per launch.
/// </summary>
public static class AgentCertificateProvider
{
    public static X509Certificate2 GetOrCreate(string certFilePath)
    {
        if (File.Exists(certFilePath))
        {
            try
            {
                return new X509Certificate2(certFilePath, (string?)null,
                    X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
            }
            catch
            {
                // Corrupt or unreadable cert file — fall through and regenerate.
            }
        }

        using var rsa = RSA.Create(2048);
        var req = new CertificateRequest(
            "CN=Penny Agent",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        var cert = req.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddYears(5));

        // Export as PFX (with private key) for reuse on the next launch.
        var pfxBytes = cert.Export(X509ContentType.Pfx);
        Directory.CreateDirectory(Path.GetDirectoryName(certFilePath)!);
        File.WriteAllBytes(certFilePath, pfxBytes);

        return new X509Certificate2(pfxBytes, (string?)null,
            X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
    }
}
