using ChaosCardGenerator;
using MegaCrit.Sts2.Core.Models;

namespace ChevalGrandSlay.Compatibility.AutoAnthony;

internal static class ChevalChaosSlots
{
    internal static IReadOnlyList<(GeneratedRarity Rarity, GeneratedCardType Type, int StartingStamina)> Layout { get; } =
    [
        .. CreateSlots(GeneratedRarity.Common, GeneratedCardType.Attack, 5, staminaCount: 3),
        .. CreateSlots(GeneratedRarity.Common, GeneratedCardType.Skill, 14, staminaCount: 7),
        .. CreateSlots(GeneratedRarity.Uncommon, GeneratedCardType.Attack, 11, staminaCount: 10),
        .. CreateSlots(GeneratedRarity.Uncommon, GeneratedCardType.Skill, 21, staminaCount: 13),
        .. CreateSlots(GeneratedRarity.Uncommon, GeneratedCardType.Power, 4, staminaCount: 0),
        .. CreateSlots(GeneratedRarity.Rare, GeneratedCardType.Attack, 5, staminaCount: 4),
        .. CreateSlots(GeneratedRarity.Rare, GeneratedCardType.Skill, 8, staminaCount: 7),
        .. CreateSlots(GeneratedRarity.Rare, GeneratedCardType.Power, 14, staminaCount: 0)
    ];

    private static IEnumerable<(GeneratedRarity Rarity, GeneratedCardType Type, int StartingStamina)> CreateSlots(
        GeneratedRarity rarity, GeneratedCardType type, int totalCount, int staminaCount)
    {
        int startingStamina = rarity switch
        {
            GeneratedRarity.Rare => 2,
            GeneratedRarity.Uncommon => 4,
            GeneratedRarity.Common => 6,
            _ => throw new ArgumentOutOfRangeException(nameof(rarity))
        };

        return Enumerable.Repeat((rarity, type, startingStamina), staminaCount)
            .Concat(Enumerable.Repeat((rarity, type, 0), totalCount - staminaCount));
    }

    private static readonly Type[] Types = typeof(ChevalChaosCard).Assembly.GetTypes()
        .Where(type => type.IsSubclassOf(typeof(ChevalChaosCard)) && !type.IsAbstract)
        .OrderBy(type => type.Name, StringComparer.Ordinal)
        .ToArray();

    internal static Type TypeAt(int slot) => Types[slot];

    internal static CardModel[] GetCards()
    {
        if (Types.Length != Layout.Count)
            throw new InvalidOperationException("Cheval generated card slot count does not match its rarity layout.");

        return Types.Select(type => ModelDb.GetById<CardModel>(ModelDb.GetId(type))).ToArray();
    }

    internal static bool IsSlot(ModelId id) =>
        Types.Any(type => ModelDb.GetId(type) == id);
}

// These type names and indices are identities used by existing saves.
public sealed class ChevalChaosCard000 : ChevalChaosCard
{
    protected override int Slot => 0;
}

public sealed class ChevalChaosCard001 : ChevalChaosCard
{
    protected override int Slot => 1;
}

public sealed class ChevalChaosCard002 : ChevalChaosCard
{
    protected override int Slot => 2;
}

public sealed class ChevalChaosCard003 : ChevalChaosCard
{
    protected override int Slot => 3;
}

public sealed class ChevalChaosCard004 : ChevalChaosCard
{
    protected override int Slot => 4;
}

public sealed class ChevalChaosCard005 : ChevalChaosCard
{
    protected override int Slot => 5;
}

public sealed class ChevalChaosCard006 : ChevalChaosCard
{
    protected override int Slot => 6;
}

public sealed class ChevalChaosCard007 : ChevalChaosCard
{
    protected override int Slot => 7;
}

public sealed class ChevalChaosCard008 : ChevalChaosCard
{
    protected override int Slot => 8;
}

public sealed class ChevalChaosCard009 : ChevalChaosCard
{
    protected override int Slot => 9;
}

public sealed class ChevalChaosCard010 : ChevalChaosCard
{
    protected override int Slot => 10;
}

public sealed class ChevalChaosCard011 : ChevalChaosCard
{
    protected override int Slot => 11;
}

public sealed class ChevalChaosCard012 : ChevalChaosCard
{
    protected override int Slot => 12;
}

public sealed class ChevalChaosCard013 : ChevalChaosCard
{
    protected override int Slot => 13;
}

public sealed class ChevalChaosCard014 : ChevalChaosCard
{
    protected override int Slot => 14;
}

public sealed class ChevalChaosCard015 : ChevalChaosCard
{
    protected override int Slot => 15;
}

public sealed class ChevalChaosCard016 : ChevalChaosCard
{
    protected override int Slot => 16;
}

public sealed class ChevalChaosCard017 : ChevalChaosCard
{
    protected override int Slot => 17;
}

public sealed class ChevalChaosCard018 : ChevalChaosCard
{
    protected override int Slot => 18;
}

public sealed class ChevalChaosCard019 : ChevalChaosCard
{
    protected override int Slot => 19;
}

public sealed class ChevalChaosCard020 : ChevalChaosCard
{
    protected override int Slot => 20;
}

public sealed class ChevalChaosCard021 : ChevalChaosCard
{
    protected override int Slot => 21;
}

public sealed class ChevalChaosCard022 : ChevalChaosCard
{
    protected override int Slot => 22;
}

public sealed class ChevalChaosCard023 : ChevalChaosCard
{
    protected override int Slot => 23;
}

public sealed class ChevalChaosCard024 : ChevalChaosCard
{
    protected override int Slot => 24;
}

public sealed class ChevalChaosCard025 : ChevalChaosCard
{
    protected override int Slot => 25;
}

public sealed class ChevalChaosCard026 : ChevalChaosCard
{
    protected override int Slot => 26;
}

public sealed class ChevalChaosCard027 : ChevalChaosCard
{
    protected override int Slot => 27;
}

public sealed class ChevalChaosCard028 : ChevalChaosCard
{
    protected override int Slot => 28;
}

public sealed class ChevalChaosCard029 : ChevalChaosCard
{
    protected override int Slot => 29;
}

public sealed class ChevalChaosCard030 : ChevalChaosCard
{
    protected override int Slot => 30;
}

public sealed class ChevalChaosCard031 : ChevalChaosCard
{
    protected override int Slot => 31;
}

public sealed class ChevalChaosCard032 : ChevalChaosCard
{
    protected override int Slot => 32;
}

public sealed class ChevalChaosCard033 : ChevalChaosCard
{
    protected override int Slot => 33;
}

public sealed class ChevalChaosCard034 : ChevalChaosCard
{
    protected override int Slot => 34;
}

public sealed class ChevalChaosCard035 : ChevalChaosCard
{
    protected override int Slot => 35;
}

public sealed class ChevalChaosCard036 : ChevalChaosCard
{
    protected override int Slot => 36;
}

public sealed class ChevalChaosCard037 : ChevalChaosCard
{
    protected override int Slot => 37;
}

public sealed class ChevalChaosCard038 : ChevalChaosCard
{
    protected override int Slot => 38;
}

public sealed class ChevalChaosCard039 : ChevalChaosCard
{
    protected override int Slot => 39;
}

public sealed class ChevalChaosCard040 : ChevalChaosCard
{
    protected override int Slot => 40;
}

public sealed class ChevalChaosCard041 : ChevalChaosCard
{
    protected override int Slot => 41;
}

public sealed class ChevalChaosCard042 : ChevalChaosCard
{
    protected override int Slot => 42;
}

public sealed class ChevalChaosCard043 : ChevalChaosCard
{
    protected override int Slot => 43;
}

public sealed class ChevalChaosCard044 : ChevalChaosCard
{
    protected override int Slot => 44;
}

public sealed class ChevalChaosCard045 : ChevalChaosCard
{
    protected override int Slot => 45;
}

public sealed class ChevalChaosCard046 : ChevalChaosCard
{
    protected override int Slot => 46;
}

public sealed class ChevalChaosCard047 : ChevalChaosCard
{
    protected override int Slot => 47;
}

public sealed class ChevalChaosCard048 : ChevalChaosCard
{
    protected override int Slot => 48;
}

public sealed class ChevalChaosCard049 : ChevalChaosCard
{
    protected override int Slot => 49;
}

public sealed class ChevalChaosCard050 : ChevalChaosCard
{
    protected override int Slot => 50;
}

public sealed class ChevalChaosCard051 : ChevalChaosCard
{
    protected override int Slot => 51;
}

public sealed class ChevalChaosCard052 : ChevalChaosCard
{
    protected override int Slot => 52;
}

public sealed class ChevalChaosCard053 : ChevalChaosCard
{
    protected override int Slot => 53;
}

public sealed class ChevalChaosCard054 : ChevalChaosCard
{
    protected override int Slot => 54;
}

public sealed class ChevalChaosCard055 : ChevalChaosCard
{
    protected override int Slot => 55;
}

public sealed class ChevalChaosCard056 : ChevalChaosCard
{
    protected override int Slot => 56;
}

public sealed class ChevalChaosCard057 : ChevalChaosCard
{
    protected override int Slot => 57;
}

public sealed class ChevalChaosCard058 : ChevalChaosCard
{
    protected override int Slot => 58;
}

public sealed class ChevalChaosCard059 : ChevalChaosCard
{
    protected override int Slot => 59;
}

public sealed class ChevalChaosCard060 : ChevalChaosCard
{
    protected override int Slot => 60;
}

public sealed class ChevalChaosCard061 : ChevalChaosCard
{
    protected override int Slot => 61;
}

public sealed class ChevalChaosCard062 : ChevalChaosCard
{
    protected override int Slot => 62;
}

public sealed class ChevalChaosCard063 : ChevalChaosCard
{
    protected override int Slot => 63;
}

public sealed class ChevalChaosCard064 : ChevalChaosCard
{
    protected override int Slot => 64;
}

public sealed class ChevalChaosCard065 : ChevalChaosCard
{
    protected override int Slot => 65;
}

public sealed class ChevalChaosCard066 : ChevalChaosCard
{
    protected override int Slot => 66;
}

public sealed class ChevalChaosCard067 : ChevalChaosCard
{
    protected override int Slot => 67;
}

public sealed class ChevalChaosCard068 : ChevalChaosCard
{
    protected override int Slot => 68;
}

public sealed class ChevalChaosCard069 : ChevalChaosCard
{
    protected override int Slot => 69;
}

public sealed class ChevalChaosCard070 : ChevalChaosCard
{
    protected override int Slot => 70;
}

public sealed class ChevalChaosCard071 : ChevalChaosCard
{
    protected override int Slot => 71;
}

public sealed class ChevalChaosCard072 : ChevalChaosCard
{
    protected override int Slot => 72;
}

public sealed class ChevalChaosCard073 : ChevalChaosCard
{
    protected override int Slot => 73;
}

public sealed class ChevalChaosCard074 : ChevalChaosCard
{
    protected override int Slot => 74;
}

public sealed class ChevalChaosCard075 : ChevalChaosCard
{
    protected override int Slot => 75;
}

public sealed class ChevalChaosCard076 : ChevalChaosCard
{
    protected override int Slot => 76;
}

public sealed class ChevalChaosCard077 : ChevalChaosCard
{
    protected override int Slot => 77;
}

public sealed class ChevalChaosCard078 : ChevalChaosCard
{
    protected override int Slot => 78;
}

public sealed class ChevalChaosCard079 : ChevalChaosCard
{
    protected override int Slot => 79;
}

public sealed class ChevalChaosCard080 : ChevalChaosCard
{
    protected override int Slot => 80;
}

public sealed class ChevalChaosCard081 : ChevalChaosCard
{
    protected override int Slot => 81;
}
