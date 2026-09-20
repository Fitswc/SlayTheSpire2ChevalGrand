using ChevalGrandSlay;
using Godot;
using STS2RitsuLib;
using STS2RitsuLib.Combat.SecondaryResources;

public static class CGSDetermination
{
    public static SecondaryResourceDefinition CGSDeterminationResource { get; private set; } = null!;
    public static string CGSDeterminationId { get; private set; } = string.Empty;

    public static void Register()
    {
        var registry = RitsuLibFramework.GetSecondaryResourceRegistry(Entry.ModId);

        // 无上限，每场开始清零，战斗内存储。
        CGSDeterminationResource = registry.Register("CGSDetermination", new SecondaryResourceDefinition(
            defaultAmount: 0,
            baseMaxAmount: null,
            turnStartPolicy: SecondaryResourceTurnStartPolicy.None,
            persistencePolicy: SecondaryResourcePersistencePolicy.Combat,
            smallIconPath: $"{Entry.ResPath}/images/resources/rage_small.png",
            largeIconPath: $"{Entry.ResPath}/images/resources/rage_large.png"
        ));
        CGSDeterminationId = CGSDeterminationResource.Id;
        
        registry.RegisterCombatUi(
            "CGSDetermination_combat_counter",
            parent =>
            {
                var row = NSecondaryResourceCounter.Create(CGSDeterminationResource, new SecondaryResourceCounterStyle
                {
                    FontSize = 32,
                    PositiveColor = Colors.Cyan,
                    FormatAmount = (amount, max) => amount.ToString(),
                    IconStyle = SecondaryResourceIconStyle.Default with
                    {
                        Size = new Vector2(80, 80),
                        HoverTip = SecondaryResourceHoverTipStyle.Default,
                    },
                });
                // 自由指定位置。例如这里我们找到能量计数器的位置，放在它旁边
                var energyCounter = parent.GetNode<Control>("%EnergyCounterContainer");
                row.Position = energyCounter.Position + new Vector2(120, -120);
                return row;
            },
            ctx => ctx.Node.Bind(ctx.Player)
        );

// 卡牌面上的次级资源费用显示。使用的图标就是你注册时提供的图标
        registry.RegisterCardUi(
            "CGSDetermination_card_ui",
            parent =>
            {
                var ui = NSecondaryResourceCardCostUi.Create(CGSDeterminationId, new SecondaryResourceCardCostUiStyle
                {
                    IconSize = new Vector2(48, 48),
                    FontSize = 24,
                });
                // 自由指定位置。例如这里我们找到能量图标的位置，放在它旁边
                var energyIcon = parent.GetNode<TextureRect>("%EnergyIcon");
                ui.Position = energyIcon.Position + new Vector2(0, 80);
                return ui;
            },
            ctx => ctx.Node.Refresh(ctx)
        );
    }
}