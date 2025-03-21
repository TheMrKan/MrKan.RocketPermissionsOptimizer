using HarmonyLib;
using Rocket.API;
using Rocket.API.Serialisation;
using Rocket.Core.Permissions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MrKan.RocketPermissionsOptimizer
{
    internal static class PermissionHelperPatches
    {
        private static readonly string[] PATCHED_METHODS = { "GetGroup", "GetGroupsByIds", "GetGroups" };
        private static CachedPermissionsHelper Helper => RPOptimizerPlugin.Helper;
        internal static void ApplyManualPatches(Harmony harmony)
        {
            var helperType = typeof(RocketPermissionsManager).Assembly.GetType("Rocket.Core.Permissions.RocketPermissionsHelper");

            MethodInfo targetMethod;
            HarmonyMethod prefix;
            foreach (var methodName in PATCHED_METHODS)
            {
                targetMethod = helperType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                prefix = new HarmonyMethod(typeof(PermissionHelperPatches).GetMethod(methodName + "Prefix", BindingFlags.Static | BindingFlags.NonPublic));
                harmony.Patch(targetMethod, prefix);
            }

            targetMethod = helperType.GetMethod("GetPermissions", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { typeof(IRocketPlayer) }, new ParameterModifier[] { });
            prefix = new HarmonyMethod(typeof(PermissionHelperPatches).GetMethod("GetPermissionsAllPrefix", BindingFlags.Static | BindingFlags.NonPublic));
            harmony.Patch(targetMethod, prefix);

            targetMethod = helperType.GetMethod("GetPermissions", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { typeof(IRocketPlayer), typeof(List<string>) }, new ParameterModifier[] { });
            prefix = new HarmonyMethod(typeof(PermissionHelperPatches).GetMethod("GetPermissionsRequestedPrefix", BindingFlags.Static | BindingFlags.NonPublic));
            harmony.Patch(targetMethod, prefix);
        }

        private static bool GetGroupPrefix(string groupId, ref RocketPermissionsGroup __result)
        {
            __result = Helper.GetGroup(groupId);
            return false;
        }

        private static bool GetGroupsByIdsPrefix(List<string> ids, ref List<RocketPermissionsGroup> __result)
        {
            __result = Helper.GetGroups(ids);
            return false;
        }

        private static bool GetGroupsPrefix(IRocketPlayer player, bool includeParentGroups, ref List<RocketPermissionsGroup> __result)
        {
            __result = Helper.GetPlayerGroups(player, includeParentGroups);
            return false;
        }

        private static bool GetPermissionsAllPrefix(IRocketPlayer player, ref List<Permission> __result)
        {
            __result = Helper.GetPlayerPermissions(player);
            return false;
        }

        private static bool GetPermissionsRequestedPrefix(IRocketPlayer player, List<string> requestedPermissions, ref List<Permission> __result)
        {
            __result = Helper.GetPlayerPermissions(player, requestedPermissions);
            return false;
        }
    }
}
