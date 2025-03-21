using Rocket.API;
using Rocket.API.Serialisation;
using Rocket.Core;
using Rocket.Core.Assets;
using Rocket.Core.Permissions;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

#nullable enable
namespace MrKan.RocketPermissionsOptimizer
{
    public class CachedPermissionsHelper : IDisposable
    {
        private readonly object m_PermissionsHelper;
        private readonly Asset<RocketPermissions> m_PermissionsAsset;

        private class PlayerPermissions
        {
            internal string PlayerId { get; }
            internal List<RocketPermissionsGroup> DirectGroups { get; set; }
            internal List<RocketPermissionsGroup> AllGroups { get; set; }
            internal Dictionary<string, Permission> Permissions { get; set; }
            internal PlayerPermissions(string playerId)
            {
                PlayerId = playerId;
                DirectGroups = new();
                AllGroups = new();
                Permissions = new();
            }
        }

        /// <summary>
        /// Groups sorted by priority (ascending)
        /// </summary>
        private RocketPermissionsGroup? m_DefaultGroup;
        private readonly List<RocketPermissionsGroup> m_SortedGroups;
        private readonly Dictionary<string, RocketPermissionsGroup> m_IdUniqueGroups;
        private readonly Dictionary<string, PlayerPermissions> m_Players;
        internal CachedPermissionsHelper()
        {
            m_PermissionsHelper = GetPermissionsHelper();
            m_PermissionsAsset = GetPermissionsAsset(m_PermissionsHelper);

            m_SortedGroups = new();
            m_IdUniqueGroups = new();
            m_Players = new();

            BuildGroupsMappings();

            Provider.onEnemyConnected += OnPlayerConnected;
            Provider.onEnemyDisconnected += OnPlayerDisconnected;
        }

        private static object GetPermissionsHelper()
        {
            var permissionHelperField = typeof(RocketPermissionsManager).GetField("helper", BindingFlags.Instance | BindingFlags.NonPublic);
            return permissionHelperField.GetValue(R.Permissions);
        }

        private static Asset<RocketPermissions> GetPermissionsAsset(object permissionsHelper)
        {
            return (Asset<RocketPermissions>)permissionsHelper.GetType().GetField("permissions", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(permissionsHelper);
        }

        private PlayerPermissions GetPlayerPermissionsObject(string id)
        {
            if (m_Players.TryGetValue(id, out PlayerPermissions playerPermissions))
            {
                return playerPermissions;
            }

            playerPermissions = new(id);
            BuildPlayerGroups(playerPermissions);
            BuildPlayerPermissions(playerPermissions);
            return playerPermissions;
        }

        private void OnPlayerConnected(SteamPlayer splayer)
        {
            var idString = splayer.playerID.steamID.m_SteamID.ToString();
            if (m_Players.ContainsKey(idString))
            {
                m_Players.Remove(idString);
            }

            GetPlayerPermissionsObject(idString);
        }

        private void OnPlayerDisconnected(SteamPlayer splayer)
        {

        }

        private void BuildGroupsMappings()
        {
            m_SortedGroups.Clear();
            m_SortedGroups.AddRange(m_PermissionsAsset.Instance.Groups.OrderBy(g => g.Priority));

            m_IdUniqueGroups.Clear();
            foreach (var group in Enumerable.Reverse(m_SortedGroups))
            {
                m_IdUniqueGroups[group.Id.ToLower()] = group;
            }

            if (!string.IsNullOrEmpty(m_PermissionsAsset.Instance.DefaultGroup))
            {
                m_DefaultGroup = GetGroup(m_PermissionsAsset.Instance.DefaultGroup);
            }
        }

        private void BuildPlayerGroups(PlayerPermissions perms)
        {
            perms.DirectGroups.Clear();
            perms.AllGroups.Clear();

            if (m_DefaultGroup != null)
            {
                perms.DirectGroups.Add(m_DefaultGroup);
                perms.AllGroups.Add(m_DefaultGroup);
            }

            foreach (var directGroup in m_SortedGroups)
            {
                if (!directGroup.Members.Contains(perms.PlayerId))
                {
                    continue;
                }

                perms.DirectGroups.Add(directGroup);
                perms.AllGroups.Add(directGroup);

                var parentGroupId = directGroup.ParentGroup?.ToLower() ?? string.Empty;
                var checkedGroups = new HashSet<string> { directGroup.Id.ToLower() };    // infinite loop protection

                while (!string.IsNullOrEmpty(parentGroupId) && !checkedGroups.Contains(parentGroupId))
                {
                    var parentGroup = GetGroup(parentGroupId);
                    if (parentGroup == null)
                    {
                        continue;
                    }

                    perms.AllGroups.Add(parentGroup);
                    checkedGroups.Add(parentGroupId.ToLower());
                    parentGroupId = parentGroup.ParentGroup?.ToLower() ?? string.Empty;
                }
            }

            perms.DirectGroups = perms.DirectGroups.Distinct().OrderBy(g => g.Priority).ToList();
            perms.AllGroups = perms.AllGroups.Distinct().OrderBy(g => g.Priority).ToList();
        }

        private void BuildPlayerPermissions(PlayerPermissions perms)
        {
            perms.Permissions.Clear();
            foreach (var group in Enumerable.Reverse(perms.AllGroups))
            {
                foreach (var p in group.Permissions)
                {
                    if (p.Name.StartsWith("-"))
                    {
                        if (perms.Permissions.ContainsKey(p.Name.ToLower()))
                        {
                            perms.Permissions.Remove(p.Name.ToLower());
                        }
                    }
                    else
                    {
                        perms.Permissions[p.Name.ToLower()] = p;
                    }
                }
            }
        }

        public RocketPermissionsGroup? GetGroup(string groupId)
        {
            if (m_IdUniqueGroups.TryGetValue(groupId.ToLower(), out var group)) return group;
            return null;
        }

        public List<RocketPermissionsGroup> GetGroups(List<string> _ids)
        {
            var ids = _ids.Select(g => g.ToLower()).ToHashSet();
            return m_SortedGroups.Where(g => ids.Contains(g.Id.ToLower())).ToList();
        }

        public List<RocketPermissionsGroup> GetPlayerGroups(IRocketPlayer player, bool includeParentGroups)
        {
            var perms = GetPlayerPermissionsObject(player.Id);
            if (includeParentGroups)
            {
                return perms.AllGroups.ToList();
            }
            return perms.DirectGroups.ToList();
        }

        public List<Permission> GetPlayerPermissions(IRocketPlayer player)
        {
            var perms = GetPlayerPermissionsObject(player.Id);
            return perms.Permissions.Values.ToList();
        }

        private bool HasPermissionExact(PlayerPermissions perms, string name, out Permission? p)
        {
            return perms.Permissions.TryGetValue(name.ToLower(), out p);
        }

        public List<Permission> GetPlayerPermissions(IRocketPlayer player, List<string> requestedPermissions)
        {
            var perms = GetPlayerPermissionsObject(player.Id);

            var result = new List<Permission>();

            if (HasPermissionExact(perms, "*", out var p))
            {
                result.Add(p!);
                return result;
            }
            
            foreach (var req in requestedPermissions)
            {
                if (HasPermissionExact(perms, req, out p))
                {
                    result.Add(p!);
                    continue;
                }

                for (int i = req.Length - 1; i > 0; i--)
                {
                    if (req[i] != '.')
                    {
                        continue;
                    }
                    if (HasPermissionExact(perms, req.Substring(0, i+1) + "*", out p))
                    {
                        result.Add(p!);
                        break;
                    }
                }
            }

            return result;
        }

        public void Dispose()
        {
            Provider.onEnemyConnected -= OnPlayerConnected;
            Provider.onEnemyDisconnected -= OnPlayerDisconnected;
        }
    }
}
