using Godot;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Utils;

namespace ChevalGrandSlay.Characters;

public sealed class CGSCardPool : TypeListCardPoolModel
{
    private static readonly Material? PoolFrameTintMaterial =
        MaterialUtils.CreateReplaceHueShaderMaterial(0.42f, 0.65f, 0.72f);
    //MaterialUtils.CreateRgbShaderMaterial(0.42f, 0.65f, 0.72f);

    // Title 和 EnergyColorName 是池子的稳定标识，不是玩家看到的角色名。
    // 自定义角色卡、遗物、药水池保持同一个 EnergyColorName，方便实验室和文本统一读取能量图标。
    public override string Title => "ChevalGrandSlay";
    public override string EnergyColorName => "ChevalGrandSlay";

    // 这里指定卡牌文本和大图使用的能量图标路径。
    // res://ChevalGrandSlay/... 里的 ChevalGrandSlay 是 PCK 资源目录，不是 C# namespace。
    // 描述中使用的能量图标。大小为24x24。
    public override string? BigEnergyIconPath => $"{Entry.ResPath}/images/characters/energy_big.png";
    // tooltip和卡牌左上角的能量图标。大小为74x74。
    public override string? TextEnergyIconPath => $"{Entry.ResPath}/images/characters/energy_text.png";
    // 卡池的主题色。
    public override Color DeckEntryCardColor => CGSCharacter.ThemeColor;
    // 能量表盘文字轮廓颜色
    public override Color EnergyOutlineColor => new(0.5f, 0.5f, 1f);
    
//    Color is rgb(100, 195, 193)

    // 根据你使用的卡框决定使用哪个Material
    private static readonly Material? _poolFrameMaterial = MaterialUtils.CreateReplaceHueShaderMaterial(0.39f, 0.76f, 0.76f); // 如果你使用原版卡框，使用这个直接替换色调。
    // private static readonly Material? _poolFrameMaterial = MaterialUtils.CreateRgbShaderMaterial(0.5f, 0.5f, 1f); // 使用原版卡框替换色调。除非你的版本没有CreateReplaceHueShaderMaterial函数，否则应使用上面那种
    // private static readonly Material? _poolFrameMaterial = MaterialUtils.CreateUnmodulatedHsvShaderMaterial(); // 如果你是自定义卡框，使用这个
    public override Material? PoolFrameMaterial => _poolFrameMaterial;
    
//    public override Material? PoolFrameMaterial => PoolFrameTintMaterial;

    // false 表示这是角色专属卡池，不是事件/状态那类无色卡池。
    public override bool IsColorless => false;
}