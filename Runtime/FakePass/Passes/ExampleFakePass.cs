using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Xyz.Vasd.FakePass.Passes
{
    [ExecuteInEditMode]
    public class ExampleFakePass : FakePassBase
    {
        protected override object Source => this;

        // Inject in Setup
        [FakePass(CustomPassInjectionPoint.AfterOpaqueDepthAndNormal, FakePassStage.Setup)]
        private void Setup_AfterDepth(ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
        }

        // Inject in Execute
        [FakePass(CustomPassInjectionPoint.AfterOpaqueDepthAndNormal, FakePassStage.Execute)]
        private void Execute_AfterDepth(CustomPassContext ctx)
        {
        }

        // Inject in Cleanup
        [FakePass(CustomPassInjectionPoint.AfterOpaqueDepthAndNormal, FakePassStage.Cleanup)]
        private void Cleanup_AfterDepth(CustomPassContext ctx)
        {
        }
    }
}