using Dalamud.Hooking;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using RotationSolver.Commands;

namespace RotationSolver.Updaters
{
    public static class ActionQueueManager
    {
        // Action Manager Hook for intercepting user input
        private static Hook<UseActionDelegate>? _useActionHook;

        // Configuration flags
        public static bool InterceptUserInput { get; set; } = Service.Config.InterceptAction;
        public static double DefaultQueueTime { get; set; } = Service.Config.InterceptActionTime;

        // Delegates for ActionManager functions
        private unsafe delegate bool UseActionDelegate(ActionManager* actionManager, uint actionType, uint actionID, ulong targetObjectID, uint param, uint useType, int pvp, bool* isGroundTarget);

        public static void Enable()
        {
            // Initialize hooks
            InitializeActionHooks();
        }

        public static void Disable()
        {
            // Dispose hooks
            DisposeActionHooks();
        }

        private static unsafe void InitializeActionHooks()
        {
            try
            {
                var useActionAddress = (nint)ActionManager.Addresses.UseAction.Value;
                var useActionLocationAddress = (nint)ActionManager.Addresses.UseActionLocation.Value;

                _useActionHook = Svc.Hook.HookFromAddress<UseActionDelegate>(useActionAddress, UseActionDetour);

                _useActionHook?.Enable();

                PluginLog.Debug("[Watcher] Action interception hooks initialized");
            }
            catch (Exception ex)
            {
                PluginLog.Error($"[Watcher] Failed to initialize action hooks: {ex}");
            }
        }

        private static void DisposeActionHooks()
        {
            try
            {
                _useActionHook?.Disable();
                _useActionHook?.Dispose();
                _useActionHook = null;

                PluginLog.Debug("[Watcher] Action interception hooks disposed");
            }
            catch (Exception ex)
            {
                PluginLog.Error($"[Watcher] Failed to dispose action hooks: {ex}");
            }
        }

        private static unsafe bool UseActionDetour(ActionManager* actionManager, uint actionType, uint actionID, ulong targetObjectID, uint param, uint useType, int pvp, bool* isGroundTarget)
        {
            if (Player.Available && InterceptUserInput && DataCenter.State && DataCenter.InCombat)
            {
                try
                {
                    // Cast actionType to ActionType enum before passing to ShouldInterceptAction
                    if (ShouldInterceptAction((ActionType)actionType, actionID))
                    {
                        HandleInterceptedAction(actionID);
                        return false; // Block the original action
                    }
                }
                catch (Exception ex)
                {
                    PluginLog.Error($"[Watcher] Error in UseActionDetour: {ex}");
                }
            }

            // Call original function if not intercepted
            return _useActionHook!.Original(actionManager, actionType, actionID, targetObjectID, param, useType, pvp, isGroundTarget);
        }

        private static bool ShouldInterceptAction(ActionType actionType, uint actionId)
        {
            // Only intercept player actions, not system actions
            if (actionType != ActionType.Action)
                return false;

            // Never intercept items - they should always go through the original game logic
            // to ensure status effects are properly applied
            if (actionType == ActionType.Item)
                return false;

            if (ActionUpdater.NextAction != null && actionId == ActionUpdater.NextAction.AdjustedID)
                return false;

            // Don't intercept auto-attacks
            var actionSheet = Svc.Data.GetExcelSheet<Lumina.Excel.Sheets.Action>();
            var action = actionSheet?.GetRow(actionId);
            if (action?.ActionCategory.Value.RowId == (uint)ActionCate.Autoattack)
                return false;

            return true;
        }

        private static void HandleInterceptedAction(uint actionId)
        {
            try
            {
                // Find the action in current rotation
                var rotationActions = RotationUpdater.CurrentRotationActions ?? [];
                var dutyActions = DataCenter.CurrentDutyRotation?.AllActions ?? [];

                // Combine actions from both sources
                var allActions = new List<IAction>();
                allActions.AddRange(rotationActions);
                allActions.AddRange(dutyActions);

                // Find matching action by ID
                IAction? matchingAction = null;
                foreach (var action in allActions)
                {
                    if (action.AdjustedID == actionId)
                    {
                        matchingAction = action;
                        break;
                    }
                }

                if (matchingAction != null)
                {
                    // Use the RSCommand system to queue the action - this is the correct approach
                    // The action will be queued using DataCenter.AddCommandAction and executed via RSCommands.DoAction()
                    string actionName = matchingAction.Name;
                    string commandString = $"{actionName}-{DefaultQueueTime}";

                    // Use the DoActionCommand which properly integrates with the RSCommand system
                    RSCommands.DoActionCommand(commandString);

                    PluginLog.Debug($"[Watcher] Intercepted and queued action via RSCommand: {matchingAction.Name} (ID: {actionId})");
                }
                else
                {
                    PluginLog.Warning($"[Watcher] Could not find matching action for intercepted ID: {actionId}");
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error($"[Watcher] Error handling intercepted action {actionId}: {ex}");
            }
        }
    }
}