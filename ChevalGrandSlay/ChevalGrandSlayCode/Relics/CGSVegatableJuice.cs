using ChevalGrandSlay.Characters;
using MegaCrit.Sts2.Core.Entities.Relics;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ChevalGrandSlay.Relics;

[RegisterRelic(typeof(CGSRelicPool))]
public sealed class CGSVegatableJuice : ModRelicTemplate
{
    private const string JuiceTexture = $"{Entry.ResPath}/images/relics/CGSVegatableJuice.png";
    // 图片资源统一放在 AssetProfile 里配置。
    // 三个路径可以先指向同一张图。后续有高清图或轮廓图时再拆开。
    public override RelicAssetProfile AssetProfile => new(
        // 小图标（原版 85x85）。
        IconPath: $"{Entry.ResPath}/images/relics/CGSVegatableJuice_small.png",
        // 轮廓图标（原版 85x85）。
        IconOutlinePath: $"{Entry.ResPath}/images/relics/CGSVegatableJuice_outline.png",
        // 大图标（原版 256x256）。
        BigIconPath: $"{Entry.ResPath}/images/relics/CGSVegatableJuice.png");

    public override RelicRarity Rarity => RelicRarity.Rare;
    
}
