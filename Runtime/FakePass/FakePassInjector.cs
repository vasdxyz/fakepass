using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Xyz.Vasd.FakePass
{
    [AddComponentMenu("FakePass/" + nameof(FakePassInjector))]
    public class FakePassInjector : MonoBehaviour
    {
        private struct Injection<T>
        {
            public object Source;
            public T Action;
        }

        private struct TypeList
        {
            public bool IsValid;

            public Type Void;

            public Type Attribute;

            public Type SetupAction;
            public Type ExecuteAction;
            public Type CleanupAction;

            public Type CommandBuffer;
            public Type CustomPassContext;
            public Type ScriptableRenderContext;
        }

        private delegate void OnSetupAction(ScriptableRenderContext renderContext, CommandBuffer cmd);
        private delegate void OnExecuteAction(CustomPassContext ctx);
        private delegate void OnCleanupAction();

        private HashSet<object> _injectedObjects = new HashSet<object>();
        private Dictionary<CustomPassInjectionPoint, List<Injection<OnSetupAction>>> _setupInjections = new();
        private Dictionary<CustomPassInjectionPoint, List<Injection<OnExecuteAction>>> _executeInjections = new();
        private Dictionary<CustomPassInjectionPoint, List<Injection<OnCleanupAction>>> _cleanupInjections = new();

        private TypeList _types;

        public bool Contains(object source)
        {
            return _injectedObjects.Contains(source);
        }

        // add object's methods to injection
        public void Add(object source)
        {
            CacheTypes();

            var logName = nameof(Add);
            var type = source.GetType();

            if (_injectedObjects.Contains(source))
            {
                LogError(logName, $"Attemp't to inject twice for '{type}'");
                return;
            };

            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var method in methods)
            {
                var attrs = method.GetCustomAttributes(_types.Attribute, false);

                if (attrs.Length < 1) continue;

                var attr = attrs[0] as FakePassAttribute;
                if (attr == null) continue;

                var resultType = method.ReturnType;
                if (resultType != _types.Void)
                {
                    LogError(logName, $"Can't inject {type.Name}.{method.Name}: Pass methods should be void");
                    continue;
                };

                var args = method.GetParameters();

                var signature = $"()";

                switch (attr.Stage)
                {
                    case FakePassStage.Setup:
                        {
                            signature = $"({nameof(ScriptableRenderContext)}, {nameof(CommandBuffer)})";

                            if (args.Length != 2)
                            {
                                LogWrongParamsError(logName, signature, method.Name, type);
                                continue;
                            }

                            var type1 = args[0].ParameterType;
                            var type2 = args[1].ParameterType;

                            if (type1 != _types.ScriptableRenderContext && type2 != _types.CommandBuffer)
                            {
                                LogWrongParamsError(logName, signature, method.Name, type);
                                continue;
                            };

                            var injections = GetInjections(attr.Point, _setupInjections);
                            var injection = new Injection<OnSetupAction>
                            {
                                Source = source,
                                Action = method.CreateDelegate(_types.SetupAction, source) as OnSetupAction
                            };
                            injections.Add(injection);
                        }
                        break;
                    case FakePassStage.Execute:
                        {
                            signature = $"({nameof(CustomPassContext)})";

                            if (args.Length != 1)
                            {
                                LogWrongParamsError(logName, signature, method.Name, type);
                                continue;
                            };

                            var type1 = args[0].ParameterType;

                            if (type1 != _types.CustomPassContext)
                            {
                                LogWrongParamsError(logName, signature, method.Name, type);
                                continue;
                            };

                            var injections = GetInjections(attr.Point, _executeInjections);
                            var injection = new Injection<OnExecuteAction>
                            {
                                Source = source,
                                Action = method.CreateDelegate(_types.ExecuteAction, source) as OnExecuteAction
                            };
                            injections.Add(injection);
                        }
                        break;
                    case FakePassStage.Cleanup:
                        {
                            signature = $"()";

                            if (args.Length != 0)
                            {
                                LogWrongParamsError(logName, signature, method.Name, type);
                                continue;
                            };

                            var injections = GetInjections(attr.Point, _cleanupInjections);
                            var injection = new Injection<OnCleanupAction>
                            {
                                Source = source,
                                Action = method.CreateDelegate(_types.ExecuteAction, source) as OnCleanupAction
                            };
                            injections.Add(injection);
                        }
                        break;
                    default:
                        break;
                }
            }

            _injectedObjects.Add(source);
        }

        // remove object's methods from injection
        public void Remove(object source)
        {
            CacheTypes();

            // Remove from Setup stage
            {
                var injections = _setupInjections;
                var keys = injections.Keys.ToArray();
                foreach (var key in keys)
                {
                    var list = injections[key];
                    if (list == null) continue;

                    list.Select(injection => injection.Source != source);
                    injections[key] = list;
                }
            }

            // Remove from Execute stage
            {
                var injections = _executeInjections;
                var keys = injections.Keys.ToArray();
                foreach (var key in keys)
                {
                    var list = injections[key];
                    if (list == null) continue;

                    list.Select(injection => injection.Source != source);
                    injections[key] = list;
                }
            }

            // Remove from CLeanup stage
            {
                var injections = _cleanupInjections;
                var keys = injections.Keys.ToArray();
                foreach (var key in keys)
                {
                    var list = injections[key];
                    if (list == null) continue;

                    list.Select(injection => injection.Source != source);
                    injections[key] = list;
                }
            }

            _injectedObjects.Remove(source);
        }

        public void OnSetup(CustomPassInjectionPoint point, ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            var injections = GetInjections(point, _setupInjections);
            foreach (var injection in injections)
            {
                injection.Action(renderContext, cmd);
            }
        }

        public void OnExecute(CustomPassInjectionPoint point, CustomPassContext ctx)
        {
            var injections = GetInjections(point, _executeInjections);
            foreach (var injection in injections)
            {
                injection.Action(ctx);
            }
        }

        public void OnCleanup(CustomPassInjectionPoint point)
        {
            var injections = GetInjections(point, _cleanupInjections);
            foreach (var injection in injections)
            {
                injection.Action();
            }
        }

        private List<T> GetInjections<T>(CustomPassInjectionPoint point, Dictionary<CustomPassInjectionPoint, List<T>> dict)
        {
            if (dict.ContainsKey(point))
            {
                return dict[point];
            }

            var list = new List<T>(1);
            dict.Add(point, list);

            return list;
        }

        private void CacheTypes()
        {
            if (_types.IsValid) return;

            _types = new TypeList
            {
                IsValid = true,

                Void = typeof(void),
                Attribute = typeof(FakePassAttribute),
                SetupAction = typeof(OnSetupAction),
                ExecuteAction = typeof(OnExecuteAction),
                CleanupAction = typeof(OnCleanupAction),

                CommandBuffer = typeof(CommandBuffer),
                ScriptableRenderContext = typeof(ScriptableRenderContext),
                CustomPassContext = typeof(CustomPassContext),
            };
        }

        private void LogError(string name, string message)
        {
            Debug.LogError($"{nameof(FakePassInjector)}: {name}: {message}");
        }

        private void LogWrongParamsError(string logName, string signature, string methodName, Type sourceType)
        {
            var method = $"{sourceType.Name}.{methodName}";
            Debug.LogError($"{nameof(FakePassInjector)}: {logName}: Cant inject {method}. Wrong signature, should be: {signature}");
        }
    }
}