using System.Security.Cryptography;
using System.Text;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ChevalGrandSlay.Compatibility.AutoAnthony;

// Keep the original model and property IDs readable for saves made before the RitsuLib migration.
public sealed class ChevalAutoAnthonyRunSnapshot : ModifierModel
{
    public override bool ShouldReceiveCombatHooks => false;

    [SavedProperty]
    public string Payload { get; set; } = string.Empty;

    [SavedProperty]
    public string Fingerprint { get; set; } = string.Empty;

    internal string VerifiedPayload
    {
        get
        {
            if (Payload.Length is 0 or > 128 * 1024 ||
                !StringComparer.Ordinal.Equals(Fingerprint, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Payload)))))
                throw new InvalidDataException("Cheval AutoAnthony pool fingerprint does not match.");
            return Payload;
        }
    }
}
