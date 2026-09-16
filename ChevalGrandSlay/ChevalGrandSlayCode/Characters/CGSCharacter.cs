using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Characters;
using STS2RitsuLib.Scaffolding.Godot;
using STS2RitsuLib.Scaffolding.Visuals.StateMachine;

namespace ChevalGrandSlay.Characters;

[RegisterCharacter]
public sealed class CGSCharacter : ModCharacterTemplate<CGSCardPool, CGSRelicPool,
    CGSPotionPool>
{
    public static readonly Color ThemeColor = new(0.4f, 0.76f, 0.76f);

    private const string SceneRoot = $"{Entry.ResPath}/scenes/characters";
    private const string SceneRootMerchant = $"{Entry.ResPath}/scenes/merchant/characters";
    private const string SceneRootRest = $"{Entry.ResPath}/scenes/rest_site";
    private const string ImageRoot = $"{Entry.ResPath}/images/characters";
    private const string CharacterScenePath = $"{SceneRoot}/ChevalGrandSlay_character.tscn";
    private const string EnergyCounterScenePath = $"{SceneRoot}/ChevalGrandSlay_energy_counter.tscn";
    private const string MerchantScenePath = $"{SceneRootMerchant}/ChevalGrandSlay_merchant.tscn";
    private const string RestSiteScenePath = $"{SceneRootRest}/ChevalGrandSlay_rest_site.tscn";
    private const string CharacterSelectBgScenePath = $"{SceneRoot}/ChevalGrandSlay_character_select_bg.tscn";

    // 角色名称颜色。
    public override Color NameColor => ThemeColor;

    // 能量图标轮廓颜色。
    public override Color EnergyLabelOutlineColor => new(0.08f, 0.18f, 0.24f);

    // 地图绘制颜色。
    public override Color MapDrawingColor => ThemeColor;

    // 人物性别（男女中立）。
    public override CharacterGender Gender => CharacterGender.Feminine;

    // 初始血量和金币。
    public override int StartingHp => 75;
    public override int StartingGold => 99;

    // CharacterAssetProfile 按类别拆分。你只写需要替换的部分，其他字段会保留回退。
    // AssetProfile 只指定模板自带的静态占位资源；没有复制的音频、拖尾、转场等资源继续从占位角色回退。
    public override CharacterAssetProfile AssetProfile => new(
        Scenes: new CharacterSceneAssetSet(
            // 人物模型 tscn 路径。
            VisualsPath: CharacterScenePath,
            // 能量表盘 tscn 路径。
            EnergyCounterPath: EnergyCounterScenePath,
            // 商店人物场景。
            MerchantAnimPath: MerchantScenePath,
            // 篝火休息场景。
            RestSiteAnimPath: RestSiteScenePath),
        Ui: new CharacterUiAssetSet(
            // 游戏左上角头像、角色统计页头像、每日挑战角色头像。
            IconPath: $"{Entry.ResPath}/scenes/icons/ChevalGrandSlay_character_icon.tscn",
            // 人物头像路径。
            IconTexturePath: $"{ImageRoot}/ChevalGrandSlay_character_icon.png",
            // 人物头像轮廓。
            IconOutlineTexturePath: $"{ImageRoot}/ChevalGrandSlay_character_icon_outline.png",
            // 人物选择背景。
            CharacterSelectBgPath: CharacterSelectBgScenePath,
            // 人物过渡动画
            CharacterSelectTransitionPath: $"{Entry.ResPath}/scenes/transition/ChevalGrandSlay_transition.tres",
            // 人物选择图标。
            CharacterSelectIconPath: $"{ImageRoot}/ChevalGrandSlay_character_select.png",
            // 人物选择图标-锁定状态。
            CharacterSelectLockedIconPath: $"{ImageRoot}/ChevalGrandSlay_character_select_locked.png",
            // 地图上的角色标记图标、表情轮盘上的角色头像。
            MapMarkerPath: $"{ImageRoot}/ChevalGrandSlay_map_marker.png"
        ),
        Vfx: new(
            // 卡牌拖尾场景。
            // TrailPath: "res://scenes/vfx/card_trail_ironclad.tscn"
        ),
        Audio: new(
            // 攻击音效
            // AttackSfx: null,
            // 施法音效
            // CastSfx: null,
            // 死亡音效
            DeathSfx: "event:/ChevalGrandSlaySFX/SFX/CGSSDeath",
            // 角色选择音效
            CharacterSelectSfx: "event:/ChevalGrandSlaySFX/SFX/CGSSIntroduce",
            // 过渡音效
            CharacterTransitionSfx: "event:/ChevalGrandSlaySFX/SFX/CGSSTransition"
        ),
        Multiplayer: new(
            // 多人模式-手指。
            // ArmPointingTexturePath: null,
            // 多人模式剪刀石头布-石头。
            // ArmRockTexturePath: null,
            // 多人模式剪刀石头布-布。
            // ArmPaperTexturePath: null,
            // 多人模式剪刀石头布-剪刀。
            // ArmScissorsTexturePath: null
        )
        // 其余如果有需要自行取消注释使用
        // Spine: null,
        // WorldProceduralVisuals: null,
        // 以下为让遗物根据你的人物展现不同的图像资源，在列表里添加即可
        // VanillaCardVisualOverrides: [],
        // VanillaRelicVisualOverrides: [
        //     new (CharacterOwnedVanillaRelicModelId.YummyCookie, new("res://icon.svg")) // 美味饼干覆盖
        // ],
        // VanillaPotionVisualOverrides: []
    );

    // 某个字段没写时，RitsuLib 会从占位角色配置里补齐。
    public override string? PlaceholderCharacterId => "ironclad";

    // 如果你的人物不需要时间线小故事，加上这句。
    public override bool RequiresEpochAndTimeline => false;

    // 攻击和施法动画延迟，以对齐动画。静态占位资源不需要延迟。
    public override float AttackAnimDelay => 0f;
    public override float CastAnimDelay => 0f;

    // 让 RitsuLib 把普通 Godot 场景转换成游戏需要的 NCreatureVisuals。
    // 自动转换人物场景，让你不需要手动挂脚本。复制即可。
    protected override NCreatureVisuals? TryCreateCreatureVisuals()
    {
        return RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(
            CharacterScenePath);
    }
    
    // 进入商店后，由状态机启动并循环播放动画。
    protected override ModAnimStateMachine? SetupCustomMerchantAnimationStateMachine(
        Node merchantRoot,
        CharacterModel character)
    {
        // RitsuLib 会在商店场景外面增加一层 Node2D，因此 SpineSprite 不一定是直接子节点，使用递归寻找节点。
        Node? spineNode = merchantRoot.FindChild("SpineSprite", recursive: true, owned: false);

        if (spineNode == null)
        {
            throw new InvalidOperationException(
                "The SpineSprite node cannot be found in the store scene. Please check ChevalGrandSlay merchant.tscn.");
        }

        return ModAnimStateMachineBuilder.Create()
            .AddState("relaxed_loop", true)
            .AsInitial()
            .Done()
            .BuildSpine(new MegaSprite(spineNode));
    }
    

    // 攻击建筑师的攻击特效列表。
    public override List<string> GetArchitectAttackVfx()
    {
        return
        [
            "vfx/vfx_attack_blunt",
            "vfx/vfx_heavy_blunt",
            "vfx/vfx_attack_slash",
            "vfx/vfx_bloody_impact",
            "vfx/vfx_rock_shatter"
        ];
    }
}
