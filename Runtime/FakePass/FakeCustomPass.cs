using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Xyz.Vasd.FakePass
{
    public class FakeCustomPass : CustomPass
    {
        public bool AutoFindInjector;
        public FakePassInjector Injector;

        protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            FindInjector();
            if (Injector == null) return;

            Injector.OnSetup(injectionPoint, renderContext, cmd);
        }

        protected override void Execute(CustomPassContext ctx)
        {
            FindInjector();
            if (Injector == null) return;

            Injector.OnExecute(injectionPoint, ctx);
        }

        protected override void Cleanup()
        {
            FindInjector();
            if (Injector == null) return;

            Injector.OnCleanup(injectionPoint);
        }

        private void FindInjector()
        {
            if (Injector != null) return;

            if (!AutoFindInjector) return;
            Injector = Object.FindObjectOfType<FakePassInjector>();
        }
    }
}