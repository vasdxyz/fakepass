A small package for easy custom pass injection.

For now, in HDRP, you should create custom pass volume for each injection point.
It's not very handfull for multi-pass features.

With this package you can mark you methods like

```C#
class MyClass
{
  // mark it to run on Execute stage at AfterOpaqueDepthAndNormal injection point
  [FakePass(CustomPassInjectionPoint.AfterOpaqueDepthAndNormal, FakePassStage.Execute)]
  private void Execute_AfterDepth(CustomPassContext ctx)
  {
  }
}
```

Then you should add you object in injector
```C#
class MyClass
{
  void Init () {
    FakePassInjector injector = FindObjectOfType<FakePassInjector>();
    injector.Add(this);
  }
}
```

Or extend `FakePassBase` monobehaviour.

In this case, use it like

```c#
[ExecuteInEditMode]
public class ExampleFakePass : FakePassBase
{
  // Tell the source of pass methods
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
```