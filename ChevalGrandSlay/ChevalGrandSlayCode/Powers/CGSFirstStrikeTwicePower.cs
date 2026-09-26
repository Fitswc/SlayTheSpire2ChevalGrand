using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Powers;

[RegisterPower]
public sealed class CGSFirstStrikeTwicePower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override PowerAssetProfile AssetProfile => new();
    protected override bool IsVisibleInternal => false;
    private CardModel? _attack;
    private bool _used;
    public void SetAttack(CardModel attack) => _attack = attack;
    public override int ModifyAttackHitCount(AttackCommand command, int count)
    {
        if (_used || command.ModelSource != _attack) return count;
        _used = true;
        return count + 1;
    }
}
