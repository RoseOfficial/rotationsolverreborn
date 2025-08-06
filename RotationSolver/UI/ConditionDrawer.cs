using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using Lumina.Excel.Sheets;
using RotationSolver.Basic.Configuration.Conditions;
using RotationSolver.Data;
using RotationSolver.Updaters;
using Action = System.Action;
using TargetType = RotationSolver.Basic.Configuration.Conditions.TargetType;

namespace RotationSolver.UI;

internal static class ConditionDrawer
{
    internal static void DrawMain(this ConditionSet? conditionSet, ICustomRotation? rotation)
    {
        if (conditionSet == null)
        {
            return;
        }

        if (rotation == null)
        {
            return;
        }

        DrawCondition(conditionSet.IsTrue(rotation), conditionSet.GetHashCode().ToString(), ref conditionSet.Not);
        ImGui.SameLine();
        conditionSet.Draw(rotation);
    }

    internal static void DrawCondition(bool? tag, string id, ref bool isNot)
    {
        float size = IconSize * (1 + (8 / 82));
        if (!tag.HasValue)
        {
            if ((IconSet.GetTexture("ui/uld/image2.tex", out Dalamud.Interface.Textures.TextureWraps.IDalamudTextureWrap? texture, true) || IconSet.GetTexture(0u, out texture)) && texture?.Handle != null)
            {
                if (ImGuiHelper.SilenceImageButton(texture, Vector2.One * size, false, id))
                {
                    isNot = !isNot;
                }

                ImguiTooltips.HoveredTooltip(string.Format(UiString.ActionSequencer_NotDescription.GetDescription(), isNot));
            }
        }
        else
        {
            if (IconSet.GetTexture("ui/uld/readycheck_hr1.tex", out Dalamud.Interface.Textures.TextureWraps.IDalamudTextureWrap? texture, true) && texture?.Handle != null)
            {
                if (ImGuiHelper.SilenceImageButton(texture, Vector2.One * size,
                    new Vector2(tag.Value ? 0 : 0.5f, 0),
                    new Vector2(tag.Value ? 0.5f : 1, 1), isNot ? ImGui.ColorConvertFloat4ToU32(new Vector4(1, 0.8f, 0.5f, 0.2f)) : 0, id))
                {
                    isNot = !isNot;
                }
                ImguiTooltips.HoveredTooltip(string.Format(UiString.ActionSequencer_NotDescription.GetDescription(), isNot));
            }
        }
    }

    internal static void DrawCondition(bool? tag)
    {
        float size = IconSize * (1 + (8 / 82));

        if (!tag.HasValue)
        {
            if ((IconSet.GetTexture("ui/uld/image2.tex", out Dalamud.Interface.Textures.TextureWraps.IDalamudTextureWrap? texture, true) || IconSet.GetTexture(0u, out texture)) && texture?.Handle != null)
            {
                ImGui.Image(texture.Handle, Vector2.One * size);
            }
        }
        else
        {
            if (IconSet.GetTexture("ui/uld/readycheck_hr1.tex", out Dalamud.Interface.Textures.TextureWraps.IDalamudTextureWrap? texture, true) && texture?.Handle != null)
            {
                ImGui.Image(texture.Handle, Vector2.One * size,
                    new Vector2(tag.Value ? 0 : 0.5f, 0),
                    new Vector2(tag.Value ? 0.5f : 1, 1));
            }
        }
    }

    private static HashSet<MemberInfo> GetAllMethods(this Type? type, Func<Type, HashSet<MemberInfo>> getFunc)
    {
        if (type == null || getFunc == null)
        {
            return [];
        }

        IEnumerable<MemberInfo> methods = getFunc(type);
        IEnumerable<MemberInfo> baseMethods = type.BaseType.GetAllMethods(getFunc);

        // Union without LINQ
        HashSet<MemberInfo> set = [.. methods];
        foreach (MemberInfo m in baseMethods)
        {
            _ = set.Add(m);
        }

        return set;
    }

    public static bool DrawByteEnum<T>(string name, ref T value) where T : struct, Enum
    {
        // Use static cache for each enum type
        var cache = EnumCache<T>.Instance;
        int index = Array.IndexOf(cache.Values, value);

        if (ImGuiHelper.SelectableCombo(name, cache.Names, ref index))
        {
            value = cache.Values[index];
            return true;
        }
        return false;
    }

    // Static cache for enum values and descriptions
    private static class EnumCache<T> where T : struct, Enum
    {
        public static readonly EnumCacheData Instance = new();

        public class EnumCacheData
        {
            public readonly T[] Values;
            public readonly string[] Names;

            public EnumCacheData()
            {
                var allValues = Enum.GetValues<T>();
                var tempList = new List<T>(allValues.Length);
                var nameList = new List<string>(allValues.Length);

                foreach (var i in allValues)
                {
                    if (i.GetAttribute<ObsoleteAttribute>() == null)
                    {
                        tempList.Add(i);
                        nameList.Add(i.GetDescription());
                    }
                }

                Values = [.. tempList];
                Names = [.. nameList];
            }
        }
    }

    public static bool DrawDragFloat2(ConfigUnitType type, string name, ref Vector2 value, string id, string name1, string name2)
    {
        ImGui.Text(name);
        id = "##" + id;
        bool result = DrawDragFloat(type, name1 + id, ref value.X);
        result |= DrawDragFloat(type, name2 + id, ref value.Y);
        return result;
    }

    public static bool DrawDragFloat3(ConfigUnitType type, string name, ref Vector3 value, string id, string name1, string name2, string name3, Func<Vector3>? func = null)
    {
        bool result = false;
        if (func == null)
        {
            ImGui.Text(name);
        }
        else
        {
            if (ImGui.Button(name + "##" + id))
            {
                value = func();
                result = true;
            }
        }

        id = "##" + id;
        result |= DrawDragFloat(type, name1 + id, ref value.X);
        result |= DrawDragFloat(type, name2 + id, ref value.Y);
        result |= DrawDragFloat(type, name3 + id, ref value.Z);
        return result;
    }

    public static bool DrawDragFloat(ConfigUnitType type, string name, ref float value, string tooltip = "")
    {
        // Display the value with appropriate formatting based on the unit type
        ImGui.SameLine();
        string show = type == ConfigUnitType.Percent ? $"{value * 100:F1}{type.ToSymbol()}" : $"{value:F2}{type.ToSymbol()}";

        // Set the width for the next item
        ImGui.SetNextItemWidth(Math.Max(50 * ImGuiHelpers.GlobalScale, ImGui.CalcTextSize(show).X));

        // Draw the appropriate control based on the unit type
        bool result = type == ConfigUnitType.Percent ? ImGui.SliderFloat(name, ref value, 0, 1, show)
            : ImGui.DragFloat(name, ref value, 0.1f, 0, 0, show);

        // Append the type description to the tooltip if it is not null or empty
        if (!string.IsNullOrEmpty(tooltip))
        {
            tooltip += "\n";
        }
        ImguiTooltips.HoveredTooltip(tooltip + type.GetDescription());

        return result;
    }

    public static bool DrawDragInt(string name, ref int value)
    {
        ImGui.SameLine();
        ImGui.SetNextItemWidth(Math.Max(50 * ImGuiHelpers.GlobalScale, ImGui.CalcTextSize(value.ToString()).X));
        return ImGui.DragInt(name, ref value);
    }

    public static bool DrawCondition(ICondition condition, ref int index)
    {
        ImGui.SameLine();

        return ImGuiHelper.SelectableCombo($"##Comparation{condition.GetHashCode()}", [">", "<", "="], ref index);
    }

    internal static void SearchItemsReflection(string popId, string name, ref string searchTxt, MemberInfo[] actions, Action<MemberInfo> selectAction)
    {
        // Provide the missing 'searchingHint' parameter
        ImGuiHelper.SearchCombo(
            popId,
            name,
            ref searchTxt,
            actions,
            action => action.Name, // Assuming the function to get the name of the action
            selectAction,
            UiString.ConfigWindow_Actions_MemberName.GetDescription(),
            null, // Optional parameter, can be null if not needed
            null  // Optional parameter, can be null if not needed
        );
    }

    public static float IconSizeRaw => ImGuiHelpers.GetButtonSize("H").Y;
    public static float IconSize => IconSizeRaw * ImGuiHelpers.GlobalScale;
    private const int count = 8;
    public static void ActionSelectorPopUp(string popUpId, CollapsingHeaderGroup group, ICustomRotation rotation, Action<IAction> action, Action? others = null)
    {
        if (group == null)
        {
            return;
        }

        using ImRaii.IEndObject popUp = ImRaii.Popup(popUpId);

        if (!popUp.Success)
        {
            return;
        }

        others?.Invoke();

        group.ClearCollapsingHeader();

        List<IBaseAction> filtered = new();
        foreach (IBaseAction i in rotation.AllBaseActions)
        {
            if (i.Action.IsInJob())
            {
                filtered.Add(i);
            }
        }
        IEnumerable<IGrouping<string, IAction>>? grouped = RotationUpdater.GroupActions(filtered.ToArray());

        foreach (IGrouping<string, IAction> pair in grouped!)
        {
            group.AddCollapsingHeader(() => pair.Key, () =>
            {
                int index = 0;
                List<IAction> items = [.. pair];
                items.Sort((a, b) => a.ID.CompareTo(b.ID));
                foreach (IAction? item in items)
                {
                    if (!item.GetTexture(out Dalamud.Interface.Textures.TextureWraps.IDalamudTextureWrap? icon) || icon?.Handle == null)
                    {
                        continue;
                    }

                    if (index++ % count != 0)
                    {
                        ImGui.SameLine();
                    }

                    using (ImRaii.IEndObject group = ImRaii.Group())
                    {
                        Vector2 cursor = ImGui.GetCursorPos();
                        if (ImGuiHelper.NoPaddingNoColorImageButton(icon, Vector2.One * IconSize, group.GetHashCode().ToString()))
                        {
                            action?.Invoke(item);
                            ImGui.CloseCurrentPopup();
                        }
                        ImGuiHelper.DrawActionOverlay(cursor, IconSize, 1);
                    }

                    string name = item.Name;
                    if (!string.IsNullOrEmpty(name))
                    {
                        ImguiTooltips.HoveredTooltip(name);
                    }
                }
            });
        }
        group.Draw();
    }

    #region Draw
    public static void Draw(this ICondition condition, ICustomRotation rotation)
    {
        if (rotation == null)
        {
            ImGui.TextColored(ImGuiColors.DalamudRed, UiString.ConfigWindow_Condition_RotationNullWarning.GetDescription());
            return;
        }

        _ = condition.CheckBefore(rotation);

        condition.DrawBefore();

        if (condition is DelayCondition delay)
        {
            delay.DrawDelay();
        }

        ImGui.SameLine();

        condition.DrawAfter(rotation);
    }

    private static void DrawDelay(this DelayCondition condition)
    {
        const float MIN = 0, MAX = 600;

        ImGui.SetNextItemWidth(80 * ImGuiHelpers.GlobalScale);
        if (ImGui.DragFloatRange2($"##Random Delay {condition.GetHashCode()}", ref condition.DelayMin, ref condition.DelayMax, 0.1f, MIN, MAX,
            $"{condition.DelayMin:F1}{ConfigUnitType.Seconds.ToSymbol()}", $"{condition.DelayMax:F1}{ConfigUnitType.Seconds.ToSymbol()}"))
        {
            condition.DelayMin = Math.Max(Math.Min(condition.DelayMin, condition.DelayMax), MIN);
            condition.DelayMax = Math.Min(Math.Max(condition.DelayMin, condition.DelayMax), MAX);
        }
        ImguiTooltips.HoveredTooltip(UiString.ActionSequencer_Delay_Description.GetDescription() +
            "\n" + ConfigUnitType.Seconds.GetDescription());

        ImGui.SameLine();

        ImGui.SetNextItemWidth(40 * ImGuiHelpers.GlobalScale);
        _ = ImGui.DragFloat($"##Offset Delay {condition.GetHashCode()}", ref condition.DelayOffset, 0.1f, MIN, MAX,
            $"{condition.DelayOffset:F1}{ConfigUnitType.Seconds.ToSymbol()}");

        ImguiTooltips.HoveredTooltip(UiString.ActionSequencer_Offset_Description.GetDescription() +
    "\n" + ConfigUnitType.Seconds.GetDescription());
    }

    private static void DrawBefore(this ICondition condition)
    {
        if (condition is ConditionSet)
        {
            ImGui.BeginGroup();
        }
    }

    private static void DrawAfter(this ICondition condition, ICustomRotation rotation)
    {
        switch (condition)
        {
            case TraitCondition traitCondition:
                traitCondition.DrawAfter(rotation);
                break;

            case ActionCondition actionCondition:
                actionCondition.DrawAfter(rotation);
                break;

            case ConditionSet conditionSet:
                conditionSet.DrawAfter(rotation);
                break;

            case RotationCondition rotationCondition:
                rotationCondition.DrawAfter(rotation);
                break;

            case TargetCondition targetCondition:
                targetCondition.DrawAfter(rotation);
                break;

            case NamedCondition namedCondition:
                namedCondition.DrawAfter(rotation);
                break;

            case TerritoryCondition territoryCondition:
                territoryCondition.DrawAfter(rotation);
                break;
        }
    }

    private static void DrawAfter(this NamedCondition namedCondition, ICustomRotation _)
    {
        (string Name, ConditionSet Condition)[] namedConditions = DataCenter.CurrentConditionValue.NamedConditions;
        List<string> namesList = new();
        foreach ((string Name, ConditionSet Condition) p in namedConditions)
        {
            namesList.Add(p.Name);
        }

        ImGuiHelper.SearchCombo($"##Comparation{namedCondition.GetHashCode()}", namedCondition.ConditionName, ref searchTxt,
            namesList.ToArray(), i => i.ToString(), i =>
            {
                namedCondition.ConditionName = i;
            }, UiString.ConfigWindow_Condition_ConditionName.GetDescription());

        ImGui.SameLine();
    }

    private static void DrawAfter(this TraitCondition traitCondition, ICustomRotation rotation)
    {
        string name = traitCondition._trait?.Name ?? string.Empty;
        string popUpKey = "Trait Condition Pop Up" + traitCondition.GetHashCode().ToString();

        using (ImRaii.IEndObject popUp = ImRaii.Popup(popUpKey))
        {
            if (popUp.Success)
            {
                int index = 0;
                foreach (Basic.Traits.IBaseTrait trait in rotation.AllTraits)
                {
                    if (!trait.GetTexture(out Dalamud.Interface.Textures.TextureWraps.IDalamudTextureWrap? traitIcon) || traitIcon?.Handle == null)
                    {
                        continue;
                    }

                    if (index++ % count != 0)
                    {
                        ImGui.SameLine();
                    }

                    using (ImRaii.IEndObject group = ImRaii.Group())
                    {
                        if (group.Success)
                        {
                            Vector2 cursor = ImGui.GetCursorPos();
                            if (ImGuiHelper.NoPaddingNoColorImageButton(traitIcon, Vector2.One * IconSize, trait.GetHashCode().ToString()))
                            {
                                traitCondition.TraitID = trait.ID;
                                ImGui.CloseCurrentPopup();
                            }
                            ImGuiHelper.DrawActionOverlay(cursor, IconSize, -1);
                        }
                    }

                    string tooltip = trait.Name;
                    if (!string.IsNullOrEmpty(tooltip))
                    {
                        ImguiTooltips.HoveredTooltip(tooltip);
                    }
                }
            }
        }

        if ((traitCondition._trait?.GetTexture(out Dalamud.Interface.Textures.TextureWraps.IDalamudTextureWrap? icon) ?? false || IconSet.GetTexture(4, out icon)) && icon?.Handle != null)
        {
            Vector2 cursor = ImGui.GetCursorPos();
            if (ImGuiHelper.NoPaddingNoColorImageButton(icon, Vector2.One * IconSize, traitCondition.GetHashCode().ToString()))
            {
                if (!ImGui.IsPopupOpen(popUpKey))
                {
                    ImGui.OpenPopup(popUpKey);
                }
            }
            ImGuiHelper.DrawActionOverlay(cursor, IconSize, -1);
            ImguiTooltips.HoveredTooltip(name);
        }

        ImGui.SameLine();
        int i = 0;
        _ = ImGuiHelper.SelectableCombo($"##Category{traitCondition.GetHashCode()}",
        [
            UiString.ActionConditionType_EnoughLevel.GetDescription()
        ], ref i);
        ImGui.SameLine();
    }

    private static readonly CollapsingHeaderGroup _actionsList = new([])
    {
        HeaderSize = 12,
    };

    private static string searchTxt = string.Empty;

    private static void DrawAfter(this ActionCondition actionCondition, ICustomRotation rotation)
    {
        string name = actionCondition._action?.Name ?? string.Empty;
        string popUpKey = "Action Condition Pop Up" + actionCondition.GetHashCode().ToString();

        ActionSelectorPopUp(popUpKey, _actionsList, rotation, item => actionCondition.ID = (ActionID)item.ID);

        if (((actionCondition._action?.GetTexture(out Dalamud.Interface.Textures.TextureWraps.IDalamudTextureWrap? icon) ?? false) || IconSet.GetTexture(4, out icon)) && icon?.Handle != null)
        {
            Vector2 cursor = ImGui.GetCursorPos();
            if (ImGuiHelper.NoPaddingNoColorImageButton(icon, Vector2.One * IconSize, actionCondition.GetHashCode().ToString()))
            {
                if (!ImGui.IsPopupOpen(popUpKey))
                {
                    ImGui.OpenPopup(popUpKey);
                }
            }
            ImGuiHelper.DrawActionOverlay(cursor, IconSize, 1);
            ImguiTooltips.HoveredTooltip(name);
        }

        ImGui.SameLine();

        _ = DrawByteEnum($"##Category{actionCondition.GetHashCode()}", ref actionCondition.ActionConditionType);

        switch (actionCondition.ActionConditionType)
        {
            case ActionConditionType.Elapsed:
            case ActionConditionType.Remain:
                _ = DrawDragFloat(ConfigUnitType.Seconds, $"##Seconds{actionCondition.GetHashCode()}", ref actionCondition.Time);
                break;

            case ActionConditionType.ElapsedGCD:
            case ActionConditionType.RemainGCD:
                if (DrawDragInt($"GCD##GCD{actionCondition.GetHashCode()}", ref actionCondition.Param1))
                {
                    actionCondition.Param1 = Math.Max(0, actionCondition.Param1);
                }
                if (DrawDragInt($"{UiString.ActionSequencer_TimeOffset.GetDescription()}##Ability{actionCondition.GetHashCode()}", ref actionCondition.Param2))
                {
                    actionCondition.Param2 = Math.Max(0, actionCondition.Param2);
                }
                break;

            case ActionConditionType.CanUse:
                string popUpId = "Can Use Id" + actionCondition.GetHashCode().ToString();
                CanUseOption option = (CanUseOption)actionCondition.Param1;

                if (ImGui.Selectable($"{option}##CanUse{actionCondition.GetHashCode()}"))
                {
                    if (!ImGui.IsPopupOpen(popUpId))
                    {
                        ImGui.OpenPopup(popUpId);
                    }
                }

                using (ImRaii.IEndObject popUp = ImRaii.Popup(popUpId))
                {
                    if (popUp.Success)
                    {
                        List<CanUseOption> showedValuesList = [];
                        foreach (CanUseOption i in Enum.GetValues<CanUseOption>())
                        {
                            if (i.GetAttribute<JsonIgnoreAttribute>() == null)
                            {
                                showedValuesList.Add(i);
                            }
                        }

                        foreach (CanUseOption value in showedValuesList)
                        {
                            bool b = option.HasFlag(value);
                            if (ImGui.Checkbox(value.GetDescription(), ref b))
                            {
                                option ^= value;
                                actionCondition.Param1 = (int)option;
                            }
                        }
                    }
                }
                break;

            case ActionConditionType.CurrentCharges:
            case ActionConditionType.MaxCharges:
                _ = DrawCondition(actionCondition, ref actionCondition.Param2);

                if (DrawDragInt($"{UiString.ActionSequencer_Charges.GetDescription()}##Charges{actionCondition.GetHashCode()}", ref actionCondition.Param1))
                {
                    actionCondition.Param1 = Math.Max(0, actionCondition.Param1);
                }
                break;
        }
    }

    private static void DrawAfter(this ConditionSet conditionSet, ICustomRotation rotation)
    {
        AddButton();

        ImGui.SameLine();

        _ = DrawByteEnum($"##Rule{conditionSet.GetHashCode()}", ref conditionSet.Type);

        ImGui.Spacing();

        for (int i = 0; i < conditionSet.Conditions.Count; i++)
        {
            ICondition condition = conditionSet.Conditions[i];

            void Delete()
            {
                conditionSet.Conditions.RemoveAt(i);
            }
            ;

            void Up()
            {
                conditionSet.Conditions.RemoveAt(i);
                conditionSet.Conditions.Insert(Math.Max(0, i - 1), condition);
            }
            ;

            void Down()
            {
                conditionSet.Conditions.RemoveAt(i);
                conditionSet.Conditions.Insert(Math.Min(conditionSet.Conditions.Count, i + 1), condition);
            }

            void Copy()
            {
                string str = JsonConvert.SerializeObject(conditionSet.Conditions[i], Formatting.Indented);
                ImGui.SetClipboardText(str);
            }

            string key = $"Condition Pop Up: {condition.GetHashCode()}";

            ImGuiHelper.DrawHotKeysPopup(key, string.Empty,
                (UiString.ConfigWindow_List_Remove.GetDescription(), Delete, ["Delete"]),
                (UiString.ConfigWindow_Actions_MoveUp.GetDescription(), Up, ["↑"]),
                (UiString.ConfigWindow_Actions_MoveDown.GetDescription(), Down, ["↓"]),
                (UiString.ConfigWindow_Actions_Copy.GetDescription(), Copy, ["Ctrl"]));

            if (condition is DelayCondition delay)
            {
                DrawCondition(delay.IsTrue(rotation), delay.GetHashCode().ToString(), ref delay.Not);
            }
            else
            {
                DrawCondition(condition.IsTrue(rotation));
            }

            ImGuiHelper.ExecuteHotKeysPopup(key, string.Empty, string.Empty, true,
                (Delete, [VirtualKey.DELETE]),
                (Up, [VirtualKey.UP]),
                (Down, [VirtualKey.DOWN]),
                (Copy, [VirtualKey.CONTROL]));

            ImGui.SameLine();

            condition.Draw(rotation);
        }

        ImGui.EndGroup();

        void AddButton()
        {
            if (ImGuiEx.IconButton(FontAwesomeIcon.Plus, "AddButton" + conditionSet.GetHashCode().ToString()))
            {
                ImGui.OpenPopup("Popup" + conditionSet.GetHashCode().ToString());
            }

            using ImRaii.IEndObject popUp = ImRaii.Popup("Popup" + conditionSet.GetHashCode().ToString());
            if (popUp)
            {
                AddOneCondition<ConditionSet>(UiString.ConfigWindow_ConditionSet.GetDescription());
                AddOneCondition<ActionCondition>(UiString.ConfigWindow_ActionSet.GetDescription());
                AddOneCondition<TraitCondition>(UiString.ConfigWindow_TraitSet.GetDescription());
                AddOneCondition<TargetCondition>(UiString.ConfigWindow_TargetSet.GetDescription());
                AddOneCondition<RotationCondition>(UiString.ConfigWindow_RotationSet.GetDescription());
                AddOneCondition<NamedCondition>(UiString.ConfigWindow_NamedSet.GetDescription());
                AddOneCondition<TerritoryCondition>(UiString.ConfigWindow_Territoryset.GetDescription());
                if (ImGui.Selectable(UiString.ActionSequencer_FromClipboard.GetDescription()))
                {
                    string str = ImGui.GetClipboardText();
                    try
                    {
                        ICondition set = JsonConvert.DeserializeObject<ICondition>(str, new IConditionConverter())!;
                        conditionSet.Conditions.Add(set);
                    }
                    catch (Exception ex)
                    {
                        PluginLog.Warning($"Failed to load the condition: {ex.Message}");
                    }
                    ImGui.CloseCurrentPopup();
                }
            }

            void AddOneCondition<T>(string description) where T : ICondition
            {
                if (ImGui.Selectable(description))
                {
                    conditionSet.Conditions.Add(Activator.CreateInstance<T>());
                    ImGui.CloseCurrentPopup();
                }
            }
        }
    }

    private static void DrawAfter(this RotationCondition rotationCondition, ICustomRotation rotation)
    {
        _ = DrawByteEnum($"##Category{rotationCondition.GetHashCode()}", ref rotationCondition.ComboConditionType);

        switch (rotationCondition.ComboConditionType)
        {
            case ComboConditionType.Bool:
                ImGui.SameLine();
                SearchItemsReflection($"##Comparation{rotationCondition.GetHashCode()}", rotationCondition._prop?.ToString() ?? "No Property", ref searchTxt, rotation.AllBools, i =>
                {
                    rotationCondition._prop = (PropertyInfo)i;
                    rotationCondition.PropertyName = i.Name;
                });

                break;

            case ComboConditionType.Integer:
                ImGui.SameLine();
                SearchItemsReflection($"##ByteChoice{rotationCondition.GetHashCode()}", rotationCondition._prop?.ToString() ?? "No Property", ref searchTxt, rotation.AllBytesOrInt, i =>
                {
                    rotationCondition._prop = (PropertyInfo)i;
                    rotationCondition.PropertyName = i.Name;
                });

                _ = DrawCondition(rotationCondition, ref rotationCondition.Condition);

                _ = DrawDragInt($"##Value{rotationCondition.GetHashCode()}", ref rotationCondition.Param1);

                break;

            case ComboConditionType.Float:
                ImGui.SameLine();
                SearchItemsReflection($"##FloatChoice{rotationCondition.GetHashCode()}", rotationCondition._prop?.ToString() ?? "No Property", ref searchTxt, rotation.AllFloats, i =>
                {
                    rotationCondition._prop = (PropertyInfo)i;
                    rotationCondition.PropertyName = i.Name;
                });

                _ = DrawCondition(rotationCondition, ref rotationCondition.Condition);

                _ = DrawDragFloat(ConfigUnitType.None, $"##Value{rotationCondition.GetHashCode()}", ref rotationCondition.Param2);
                break;

            case ComboConditionType.Last:
                ImGui.SameLine();

                string[] names = new string[]
                    {
                        nameof(CustomRotation.IsLastGCD),
                        nameof(CustomRotation.IsLastAction),
                        nameof(CustomRotation.IsLastAbility),
                    };
                int index = Math.Max(0, Array.IndexOf(names, rotationCondition.MethodName));
                if (ImGuiHelper.SelectableCombo($"##Last{rotationCondition.GetHashCode()}", names, ref index))
                {
                    rotationCondition.MethodName = names[index];
                }

                ImGui.SameLine();

                string name = rotationCondition._action?.Name ?? string.Empty;

                string popUpKey = "Rotation Condition Pop Up" + rotationCondition.GetHashCode().ToString();

                ActionSelectorPopUp(popUpKey, _actionsList, rotation, item => rotationCondition.ID = (ActionID)item.ID);

                if ((rotationCondition._action?.GetTexture(out Dalamud.Interface.Textures.TextureWraps.IDalamudTextureWrap? icon) ?? false || IconSet.GetTexture(4, out icon)) && icon?.Handle != null)
                {
                    Vector2 cursor = ImGui.GetCursorPos();
                    if (ImGuiHelper.NoPaddingNoColorImageButton(icon, Vector2.One * IconSize, rotationCondition.GetHashCode().ToString()))
                    {
                        if (!ImGui.IsPopupOpen(popUpKey))
                        {
                            ImGui.OpenPopup(popUpKey);
                        }
                    }
                    ImGuiHelper.DrawActionOverlay(cursor, IconSize, 1);
                }

                ImGui.SameLine();
                _ = ImGuiHelper.SelectableCombo($"##Adjust{rotationCondition.GetHashCode()}",
                [
                    UiString.ActionSequencer_Original.GetDescription(),
                    UiString.ActionSequencer_Adjusted.GetDescription(),
                ], ref rotationCondition.Param1);
                break;
        }
    }

    private static Status[]? _allStatus = null;
    private static Status[] AllStatus
    {
        get
        {
            if (_allStatus == null)
            {
                StatusID[] ids = Enum.GetValues<StatusID>();
                List<Status> tempList = new();
                foreach (StatusID id in ids)
                {
                    Status status = Service.GetSheet<Status>().GetRow((uint)id);
                    if (status.RowId != 0)
                    {
                        tempList.Add(status);
                    }
                }
                _allStatus = tempList.ToArray();
            }
            return _allStatus!;
        }
    }

    private static void DrawAfter(this TargetCondition targetCondition, ICustomRotation rotation)
    {
        _ = DelayCondition.CheckBaseAction(rotation, targetCondition.ID, ref targetCondition._action);

        if (targetCondition.StatusId != StatusID.None &&
            (targetCondition.Status == null || targetCondition.Status.Value.RowId != (uint)targetCondition.StatusId))
        {
            Status? found = null;
            foreach (Status a in AllStatus)
            {
                if (a.RowId == (uint)targetCondition.StatusId)
                {
                    found = a;
                    break;
                }
            }
            targetCondition.Status = found;
        }

        string popUpKey = "Target Condition Pop Up" + targetCondition.GetHashCode().ToString();

        ActionSelectorPopUp(popUpKey, _actionsList, rotation, item => targetCondition.ID = (ActionID)item.ID, () =>
        {
            if (ImGui.Selectable(TargetType.HostileTarget.GetDescription()))
            {
                targetCondition._action = null;
                targetCondition.ID = ActionID.None;
                targetCondition.TargetType = TargetType.HostileTarget;
            }

            if (ImGui.Selectable(TargetType.Target.GetDescription()))
            {
                targetCondition._action = null;
                targetCondition.ID = ActionID.None;
                targetCondition.TargetType = TargetType.Target;
            }

            if (ImGui.Selectable(TargetType.Player.GetDescription()))
            {
                targetCondition._action = null;
                targetCondition.ID = ActionID.None;
                targetCondition.TargetType = TargetType.Player;
            }
        });

        if ((targetCondition._action != null ? targetCondition._action.GetTexture(out Dalamud.Interface.Textures.TextureWraps.IDalamudTextureWrap? icon) || IconSet.GetTexture(4, out icon)
            : IconSet.GetTexture(targetCondition.TargetType switch
            {
                TargetType.Target => 16u,
                TargetType.HostileTarget => 15u,
                TargetType.Player => 18u,
                _ => 0,
            }, out icon)) && icon?.Handle != null)
        {
            Vector2 cursor = ImGui.GetCursorPos();
            if (ImGuiHelper.NoPaddingNoColorImageButton(icon, Vector2.One * IconSize, targetCondition.GetHashCode().ToString()))
            {
                if (!ImGui.IsPopupOpen(popUpKey))
                {
                    ImGui.OpenPopup(popUpKey);
                }
            }
            ImGuiHelper.DrawActionOverlay(cursor, IconSize, 1);

            string description = targetCondition._action != null ? string.Format(UiString.ActionSequencer_ActionTarget.GetDescription(), targetCondition._action.Name)
                : targetCondition.TargetType.GetDescription();
            ImguiTooltips.HoveredTooltip(description);
        }

        ImGui.SameLine();
        _ = DrawByteEnum($"##Category{targetCondition.GetHashCode()}", ref targetCondition.TargetConditionType);

        string popupId = "Status Finding Popup" + targetCondition.GetHashCode().ToString();

        RotationConfigWindow.StatusPopUp(popupId, AllStatus, ref searchTxt, status =>
        {
            targetCondition.Status = status;
            targetCondition.StatusId = (StatusID)targetCondition.Status.Value.RowId;
        }, size: IconSizeRaw);

        void DrawStatusIcon()
        {
            if ((IconSet.GetTexture(targetCondition.Status?.Icon ?? 16220, out Dalamud.Interface.Textures.TextureWraps.IDalamudTextureWrap? icon)
                || IconSet.GetTexture(16220, out icon)) && icon?.Handle != null)
            {
                if (ImGuiHelper.NoPaddingNoColorImageButton(icon, new Vector2(IconSize * 3 / 4, IconSize) * ImGuiHelpers.GlobalScale, targetCondition.GetHashCode().ToString()))
                {
                    if (!ImGui.IsPopupOpen(popupId))
                    {
                        ImGui.OpenPopup(popupId);
                    }
                }
                ImguiTooltips.HoveredTooltip(targetCondition.Status?.Name.ExtractText() ?? string.Empty);
            }
        }

        switch (targetCondition.TargetConditionType)
        {
            case TargetConditionType.HasStatus:
                ImGui.SameLine();
                DrawStatusIcon();

                ImGui.SameLine();

                int check = targetCondition.FromSelf ? 1 : 0;
                if (ImGuiHelper.SelectableCombo($"From Self {targetCondition.GetHashCode()}",
                [
                    UiString.ActionSequencer_StatusAll.GetDescription(),
                    UiString.ActionSequencer_StatusSelf.GetDescription(),
                ], ref check))
                {
                    targetCondition.FromSelf = check != 0;
                }
                break;

            case TargetConditionType.StatusEnd:
                ImGui.SameLine();
                DrawStatusIcon();

                ImGui.SameLine();

                check = targetCondition.FromSelf ? 1 : 0;
                if (ImGuiHelper.SelectableCombo($"From Self {targetCondition.GetHashCode()}",
                [
                    UiString.ActionSequencer_StatusAll.GetDescription(),
                    UiString.ActionSequencer_StatusSelf.GetDescription(),
                ], ref check))
                {
                    targetCondition.FromSelf = check != 0;
                }

                _ = DrawCondition(targetCondition, ref targetCondition.Param2);

                _ = DrawDragFloat(ConfigUnitType.Seconds, $"s##Seconds{targetCondition.GetHashCode()}", ref targetCondition.DistanceOrTime);
                break;


            case TargetConditionType.StatusEndGCD:
                ImGui.SameLine();
                DrawStatusIcon();

                ImGui.SameLine();

                check = targetCondition.FromSelf ? 1 : 0;
                if (ImGuiHelper.SelectableCombo($"From Self {targetCondition.GetHashCode()}",
                [
                    UiString.ActionSequencer_StatusAll.GetDescription(),
                    UiString.ActionSequencer_StatusSelf.GetDescription(),
                ], ref check))
                {
                    targetCondition.FromSelf = check != 0;
                }

                _ = DrawDragInt($"GCD##GCD{targetCondition.GetHashCode()}", ref targetCondition.GCD);
                _ = DrawDragFloat(ConfigUnitType.Seconds, $"{UiString.ActionSequencer_TimeOffset.GetDescription()}##Ability{targetCondition.GetHashCode()}", ref targetCondition.DistanceOrTime);
                break;

            case TargetConditionType.Distance:
                _ = DrawCondition(targetCondition, ref targetCondition.Param2);

                if (DrawDragFloat(ConfigUnitType.Yalms, $"##yalm{targetCondition.GetHashCode()}", ref targetCondition.DistanceOrTime))
                {
                    targetCondition.DistanceOrTime = Math.Max(0, targetCondition.DistanceOrTime);
                }
                break;

            case TargetConditionType.CastingAction:
                ImGui.SameLine();
                ImGuiHelper.SetNextWidthWithName(targetCondition.CastingActionName);
                _ = ImGui.InputText($"Ability Name##CastingActionName{targetCondition.GetHashCode()}", ref targetCondition.CastingActionName, 128);
                break;

            case TargetConditionType.CastingActionTime:
                _ = DrawCondition(targetCondition, ref targetCondition.Param2);
                _ = DrawDragFloat(ConfigUnitType.Seconds, $"##CastingActionTimeUntil{targetCondition.GetHashCode()}", ref targetCondition.DistanceOrTime);
                break;

            case TargetConditionType.HPRatio:
                _ = DrawCondition(targetCondition, ref targetCondition.Param2);

                _ = DrawDragFloat(ConfigUnitType.Percent, $"##HPRatio{targetCondition.GetHashCode()}", ref targetCondition.DistanceOrTime);
                break;

            case TargetConditionType.MP:
            case TargetConditionType.HP:
                _ = DrawCondition(targetCondition, ref targetCondition.Param2);

                _ = DrawDragInt($"##HPorMP{targetCondition.GetHashCode()}", ref targetCondition.GCD);
                break;

            case TargetConditionType.TimeToKill:
                _ = DrawCondition(targetCondition, ref targetCondition.Param2);

                _ = DrawDragFloat(ConfigUnitType.Seconds, $"##TimeToKill{targetCondition.GetHashCode()}", ref targetCondition.DistanceOrTime);
                break;

            case TargetConditionType.TargetName:
                ImGui.SameLine();
                ImGuiHelper.SetNextWidthWithName(targetCondition.CastingActionName);
                _ = ImGui.InputText($"Name##TargetName{targetCondition.GetHashCode()}", ref targetCondition.CastingActionName, 128);
                break;
            case TargetConditionType.TargetRole:
                ImGui.SameLine();
                ImGuiHelper.SetNextWidthWithName(targetCondition.CombatRole.ToString());
                _ = ImGui.InputText($"Name##TargetRole{targetCondition.GetHashCode()}", ref targetCondition.CombatRole, 128);
                break;
        }

        if (targetCondition._action == null && targetCondition.TargetType == TargetType.Target)
        {
            using ImRaii.Color style = ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudRed);
            ImGui.TextWrapped(UiString.ConfigWindow_Condition_TargetWarning.GetDescription());
        }
    }

    private static string[]? _territoryNames = null;
    public static string[] TerritoryNames
    {
        get
        {
            if (_territoryNames == null)
            {
                Lumina.Excel.ExcelSheet<TerritoryType> sheet = Service.GetSheet<TerritoryType>();
                List<string> tempList = new();
                if (sheet != null)
                {
                    foreach (TerritoryType t in sheet)
                    {
                        string name = t.PlaceName.Value.Name.ExtractText();
                        if (string.IsNullOrEmpty(name))
                        {
                            continue;
                        }

                        tempList.Add(name);
                    }
                }
                _territoryNames = tempList.ToArray();
            }
            return _territoryNames!;
        }
    }

    private static string[]? _dutyNames = null;
    public static string[] DutyNames
    {
        get
        {
            if (_dutyNames == null)
            {
                Lumina.Excel.ExcelSheet<ContentFinderCondition> sheet = Service.GetSheet<ContentFinderCondition>();
                HashSet<string> tempSet = new();
                if (sheet != null)
                {
                    foreach (ContentFinderCondition t in sheet)
                    {
                        string name = t.Name.ExtractText();
                        if (!string.IsNullOrEmpty(name))
                        {
                            _ = tempSet.Add(name);
                        }
                    }
                }
                // Reverse
                string[] arr = new string[tempSet.Count];
                tempSet.CopyTo(arr);
                Array.Reverse(arr);
                _dutyNames = arr;
            }
            return _dutyNames!;
        }
    }

    private static void DrawAfter(this TerritoryCondition territoryCondition, ICustomRotation _)
    {
        DrawByteEnum($"##Category{territoryCondition.GetHashCode()}", ref territoryCondition.TerritoryConditionType);

        switch (territoryCondition.TerritoryConditionType)
        {
            case TerritoryConditionType.TerritoryContentType:
                ImGui.SameLine();

                var type = (TerritoryContentType)territoryCondition.TerritoryId;
                DrawByteEnum($"##TerritoryContentType{territoryCondition.GetHashCode()}", ref type);
                territoryCondition.TerritoryId = (int)type;
                break;

            case TerritoryConditionType.TerritoryName:
                ImGui.SameLine();

                ImGuiHelper.SearchCombo($"##TerritoryName{territoryCondition.GetHashCode()}", territoryCondition.Name, ref searchTxt,
                TerritoryNames, i => i.ToString(), i =>
                {
                    territoryCondition.Name = i;
                }, UiString.ConfigWindow_Condition_TerritoryName.GetDescription());
                break;

            case TerritoryConditionType.DutyName:
                ImGui.SameLine();

                ImGuiHelper.SearchCombo($"##DutyName{territoryCondition.GetHashCode()}", territoryCondition.Name, ref searchTxt,
                DutyNames, i => i.ToString(), i =>
                {
                    territoryCondition.Name = i;
                }, UiString.ConfigWindow_Condition_DutyName.GetDescription());
                break;
        }
    }
    #endregion
}
