using Rocket.API;
using Rocket.API.Serialisation;
using Rocket.Core;
using Rocket.Core.Assets;
using Rocket.Core.Permissions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

#nullable enable
namespace MrKan.RocketPermissionsOptimizer
{
    public class CachedPermissionsHelper
    {
        private readonly object m_PermissionsHelper;
        private readonly Asset<RocketPermissions> m_PermissionsAsset;
        internal CachedPermissionsHelper()
        {
            m_PermissionsHelper = GetPermissionsHelper();
            m_PermissionsAsset = GetPermissionsAsset(m_PermissionsHelper);
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
    }
}
