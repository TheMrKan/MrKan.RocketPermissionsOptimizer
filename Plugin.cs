using HarmonyLib;
using Rocket.Core;
using Rocket.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MrKan.RocketPermissionsOptimizer
{
    public class RPOptimizerPlugin : RocketPlugin<Config>
    {
        public static RPOptimizerPlugin? Instance { get; private set; }
        public static CachedPermissionsHelper Helper { get; private set; }

        private Harmony m_Harmony;
        protected override void Load()
        {
            Instance = this;

            m_Harmony = new Harmony("MrKan.RocketPermissionsOptimizer");
            m_Harmony.PatchAll();

            Helper = new();
        }

        protected override void Unload()
        {
            m_Harmony.UnpatchAll("MrKan.RocketPermissionsOptimizer");

            Instance = null;
        }
    }
}
