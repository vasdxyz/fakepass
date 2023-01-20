using System;
using UnityEngine.Rendering.HighDefinition;

namespace Xyz.Vasd.FakePass
{
    public class FakePassAttribute : Attribute
    {
        public FakePassStage Stage { get; private set; }
        public CustomPassInjectionPoint Point { get; private set; }

        public FakePassAttribute(
            CustomPassInjectionPoint point = CustomPassInjectionPoint.AfterOpaqueDepthAndNormal,
            FakePassStage stage = FakePassStage.Execute
        )
        {
            Point = point;
            Stage = stage;
        }
    }
}