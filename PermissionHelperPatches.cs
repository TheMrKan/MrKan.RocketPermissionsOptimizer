using HarmonyLib;
using Rocket.API;
using Rocket.API.Serialisation;
using Rocket.Core.Permissions;
using System;
using System.Collections.Generic;
using System.Linq;
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

            foreach (var methodName in PATCHED_METHODS)
            {
                var targetMethod = helperType.GetMethod(methodName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                var prefix = new HarmonyMethod(typeof(PermissionHelperPatches).GetMethod(methodName + "Prefix", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic));
                harmony.Patch(targetMethod, prefix);
            }
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
    }
}
